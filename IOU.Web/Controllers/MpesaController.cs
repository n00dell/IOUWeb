using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services;
using IOU.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;

namespace IOU.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MpesaController : Controller
    {
        private readonly IOUWebContext _context;
        private readonly MpesaPaymentService _mpesaService;
        private readonly ILogger<MpesaController> _logger;
        private readonly IDebtService _debtService;

        public MpesaController(IOUWebContext context, MpesaPaymentService mpesaService, ILogger<MpesaController> logger, IDebtService debtService)
        {
            _context = context;
            _logger = logger;
            _mpesaService = mpesaService;
            _debtService = debtService;
        }
        [HttpPost("initiate")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiateRequest request)
        {
            _logger.LogInformation("InitiatePayment request received: {@Request}", request);

            try
            {
                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("Null request received");
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                if (request.Amount <= 0)
                {
                    _logger.LogWarning("Invalid amount: {Amount}", request.Amount);
                    return BadRequest(new { success = false, message = "Amount must be greater than 0" });
                }

                // Get current user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not authenticated");
                    return Unauthorized();
                }

                // Verify debt exists and belongs to user
                var debt = await _context.Debt
                    .FirstOrDefaultAsync(d => d.Id == request.DebtId && d.StudentUserId == userId);

                if (debt == null)
                {
                    _logger.LogWarning("Debt not found or unauthorized access. DebtId: {DebtId}, UserId: {UserId}",
                        request.DebtId, userId);
                    return NotFound(new { success = false, message = "Debt not found" });
                }

                // Format phone number
                string formattedPhone;
                try
                {
                    formattedPhone = FormatPhoneNumber(request.PhoneNumber);
                    _logger.LogInformation("Formatted phone number: {Phone}", formattedPhone);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phone number formatting failed");
                    return BadRequest(new { success = false, message = "Invalid phone number format" });
                }

                // Initiate M-Pesa payment
                var mpesaResponse = await _mpesaService.InitiatePaymentAsync(
                    formattedPhone,
                    request.Amount,
                    request.DebtId
                );

                if (!mpesaResponse.Success)
                {
                    
                    return StatusCode(500, new
                    {
                        success = false,
                        message = mpesaResponse.ErrorMessage ?? "Payment initiation failed"
                    });
                }

                // Save payment record
                var payment = new Payment
                {
                    Id = Guid.NewGuid().ToString(),
                    DebtId = request.DebtId,
                    Amount = request.Amount,
                    PhoneNumber = formattedPhone,
                    CheckoutRequestID = mpesaResponse.CheckoutRequestID,
                    Status = PaymentTransactionStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment initiated successfully. PaymentId: {PaymentId}", payment.Id);

                return Ok(new
                {
                    success = true,
                    message = "Payment initiated successfully",
                    paymentId = payment.Id,
                    checkoutRequestID = mpesaResponse.CheckoutRequestID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred",
                    error = ex.Message
                });
            }
        }

        
        [Authorize(Roles = "Student")]
        [HttpGet("payment-status/{checkoutRequestId}")]
        public async Task<IActionResult> CheckPaymentStatus(string checkoutRequestId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.CheckoutRequestID == checkoutRequestId);

            if (payment == null) return NotFound();

            return Ok(new
            {
                status = payment.Status.ToString(),
                confirmed = payment.Status == PaymentTransactionStatus.Paid,
                receipt = payment.MpesaReceiptNumber,
                error = payment.FailureReason
            });
        }
        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove any non-digit characters
            string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // If starts with 0, replace it with 254 (Kenya's country code)
            if (digitsOnly.StartsWith("0"))
            {
                digitsOnly = "254" + digitsOnly.Substring(1);
            }
            // If the number doesn't have a country code and is 9 digits, add country code
            else if (digitsOnly.Length == 9)
            {
                digitsOnly = "254" + digitsOnly;
            }

            return digitsOnly;
        }

        [HttpPost("callback")]
        public async Task<IActionResult> MpesaCallback([FromBody] MpesaCallbackResponse callbackData)
        {
            try
            {
                _logger.LogInformation($"Received MPESA callback: {JsonConvert.SerializeObject(callbackData, Formatting.Indented)}");

                if (callbackData?.Body?.StkCallback == null)
                {
                    _logger.LogWarning("Invalid callback structure received: Null or missing StkCallback");
                    return BadRequest("Invalid callback structure");
                }

                var stkCallback = callbackData.Body.StkCallback;
                _logger.LogInformation($"Callback Details - ResultCode: {stkCallback.ResultCode}, Description: {stkCallback.ResultDesc}");

                // Find the payment record
                var payment = await _context.Payments
                    .Include(p => p.Debt) // Include Debt for updates
                    .FirstOrDefaultAsync(p => p.CheckoutRequestID == stkCallback.CheckoutRequestID);

                if (payment == null)
                {
                    _logger.LogWarning($"Payment not found for CheckoutRequestID: {stkCallback.CheckoutRequestID}");
                    return NotFound("Payment not found");
                }

                // Update payment details
                payment.ResultCode = stkCallback.ResultCode;
                payment.ResultDescription = stkCallback.ResultDesc;

                if (stkCallback.ResultCode == "0") // Success
                {
                    payment.Status = PaymentTransactionStatus.Paid;
                    payment.PaymentDate = DateTime.UtcNow;
                    payment.CompletedAt = DateTime.UtcNow;

                    // Extract metadata values
                    if (stkCallback.CallbackMetadata?.Item != null)
                    {
                        foreach (var item in stkCallback.CallbackMetadata.Item)
                        {
                            switch (item.Name)
                            {
                                case "MpesaReceiptNumber":
                                    payment.MpesaReceiptNumber = item.Value?.ToString();
                                    break;
                                case "TransactionID":
                                    payment.MpesaTransactionId = item.Value?.ToString();
                                    break;
                                case "Amount":
                                    if (decimal.TryParse(item.Value?.ToString(), out var amount))
                                        payment.Amount = amount;
                                    break;
                                case "PhoneNumber":
                                    payment.PhoneNumber = item.Value?.ToString();
                                    break;
                            }
                        }
                    }

                    // Update debt
                    if (payment.Debt != null)
                    {
                        payment.Debt.CurrentBalance -= payment.Amount;
                        await _debtService.UpdateDebtCalculations(payment.Debt.Id);
                    }

                    // Remove Reload() as it overwrites changes
                    await _context.SaveChangesAsync();

                    // Process against scheduled payments
                    await ProcessPaymentAgainstSchedule(payment);
                }
                else
                {
                    payment.Status = PaymentTransactionStatus.Failed;
                    payment.FailureReason = stkCallback.ResultDesc;
                    _logger.LogError($"MPESA payment failed. Code: {stkCallback.ResultCode}, Description: {stkCallback.ResultDesc}");
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MPESA callback");
                return StatusCode(500, "Internal server error");
            }
        }
        private bool ValidateCallback(MpesaCallbackResponse callback)
        {
            if (callback?.Body?.StkCallback == null) return false;

            // Required fields for successful transaction
            if (callback.Body.StkCallback.ResultCode == "0")
            {
                return callback.Body.StkCallback.CallbackMetadata?.Item != null &&
                       callback.Body.StkCallback.CallbackMetadata.Item.Any(i =>
                           i.Name == "MpesaReceiptNumber" &&
                           i.Name == "Amount");
            }

            return true;
        }

        [HttpPost("update-debt/{paymentId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateDebtAfterPayment(string paymentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var payment = await _context.Payments
                    .Include(p => p.Debt)
                    .FirstOrDefaultAsync(p => p.Id == paymentId && p.Debt.StudentUserId == userId);

                if (payment == null)
                {
                    return NotFound(new { success = false, message = "Payment not found" });
                }

                if (payment.Status != PaymentTransactionStatus.Paid)
                {
                    return BadRequest(new { success = false, message = "Payment not completed" });
                }

                // Update debt calculations
                var debt = payment.Debt;
                debt.CurrentBalance -= payment.Amount;

                // Process the payment against scheduled payments
                await ProcessPaymentAgainstSchedule(payment);

                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating debt after payment");
                return StatusCode(500, new { success = false, message = "Failed to update debt" });
            }
        }
        private async Task ProcessSuccessfulPayment(Payment payment, StkCallback stkCallback)
        {
            try
            {
                payment.Status = PaymentTransactionStatus.Paid;
                payment.PaymentDate = DateTime.UtcNow;

                foreach (var item in stkCallback.CallbackMetadata.Item)
                {
                    switch (item.Name)
                    {
                        case "MpesaReceiptNumber":
                            payment.MpesaReceiptNumber = item.Value?.ToString();
                            break;
                        case "TransactionID":
                            payment.MpesaTransactionId = item.Value?.ToString();
                            break;
                        case "Amount":
                            if (decimal.TryParse(item.Value?.ToString(), out var amount))
                            {
                                payment.Amount = amount;
                            }
                            break;
                        case "PhoneNumber":
                            payment.PhoneNumber = item.Value?.ToString();
                            break;
                    }
                }

                var debt = await _context.Debt.FindAsync(payment.DebtId);
                if (debt != null)
                {
                    debt.CurrentBalance -= payment.Amount;
                    await _debtService.UpdateDebtCalculations(debt.Id);
                }

                await ProcessPaymentAgainstSchedule(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing successful payment {payment.Id}, Receipt: {payment.MpesaReceiptNumber}");
                throw;
            }

        }


        private async Task ProcessPaymentAgainstSchedule(Payment payment)
        {
            var scheduledPayments = await _context.ScheduledPayment
                .Where(p => p.DebtId == payment.DebtId && p.Status != ScheduledPaymentStatus.Paid)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

            var remainingAmount = payment.Amount;

            foreach (var scheduledPayment in scheduledPayments)
            {
                if (remainingAmount <= 0) break;

                var amountToApply = Math.Min(remainingAmount, scheduledPayment.Amount);

                scheduledPayment.Amount -= amountToApply;
                remainingAmount -= amountToApply;

                if (scheduledPayment.Amount <= 0)
                {
                    scheduledPayment.Status = ScheduledPaymentStatus.Paid;
                    scheduledPayment.PaymentDate = DateTime.UtcNow;
                }
            }

            // If there's remaining amount after applying to all scheduled payments,
            // create a custom payment record
            if (remainingAmount > 0)
            {
                var customPayment = new ScheduledPayment
                {
                    DebtId = payment.DebtId,
                    Amount = remainingAmount,
                    Status = ScheduledPaymentStatus.Paid,
                    PaymentDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow,
                    IsCustomPayment = true
                };

                _context.ScheduledPayment.Add(customPayment);
            }
        }

    }
    
}
