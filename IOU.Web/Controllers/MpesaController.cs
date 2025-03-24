using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services;
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

        public MpesaController(IOUWebContext context, MpesaPaymentService mpesaService, ILogger<MpesaController> logger)
        {
            _context = context;
            _logger = logger;
            _mpesaService = mpesaService;
        }
        [HttpPost("initiate")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiateRequest request)
        {
            try
            {
                // Log the incoming request
                _logger.LogInformation($"Payment initiation request: {JsonConvert.SerializeObject(request)}");

                // Validate input
                if (string.IsNullOrEmpty(request.DebtId) || request.Amount <= 0 || string.IsNullOrEmpty(request.PhoneNumber))
                {
                    _logger.LogWarning($"Invalid payment details: DebtId={request.DebtId}, Amount={request.Amount}, Phone={request.PhoneNumber}");
                    return BadRequest(new { success = false, message = "Invalid payment details" });
                }

                // Check if debt exists and belongs to the current user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var debt = await _context.Debt.FirstOrDefaultAsync(d => d.Id == request.DebtId && d.StudentUserId == userId);

                if (debt == null)
                {
                    _logger.LogWarning($"Debt not found or not authorized: DebtId={request.DebtId}, UserId={userId}");
                    return NotFound(new { success = false, message = "Debt not found or not authorized" });
                }

                // Format phone number to E.164 format
                string formattedPhone = FormatPhoneNumber(request.PhoneNumber);
                _logger.LogInformation($"Formatted phone number: {formattedPhone}");

                // Initiate the MPESA payment
                var response = await _mpesaService.InitiatePaymentAsync(
                    formattedPhone,
                    request.Amount,
                    request.DebtId // Using debt ID as account reference
                );

                // Store pending payment information - use a unique ID
                var payment = new Payment
                {
                    Id = Guid.NewGuid().ToString(), // Explicitly set ID
                    DebtId = request.DebtId,
                    Amount = request.Amount,
                    PhoneNumber = formattedPhone, 
                    CheckoutRequestID = response.CheckoutRequestID,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,

                    // Explicitly set nullable fields
                    ResultCode = null,
                    ResultDescription = null,
                    MpesaReceiptNumber = null,
                    MpesaTransactionId = null,
                    CompletedAt = null,
                    PaymentDate = null
                };

                try
                {
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    // Log the specific database exception
                    _logger.LogError(dbEx, $"Database error when saving payment: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        _logger.LogError($"Inner DB exception: {dbEx.InnerException.Message}");

                        // Return more detailed error info for debugging
                        return StatusCode(500, new
                        {
                            success = false,
                            message = "Database error while saving payment",
                            error = dbEx.Message,
                            innerError = dbEx.InnerException.Message
                        });
                    }
                    throw; // Re-throw to be caught by outer catch
                }

                _logger.LogInformation($"Payment initiated successfully: ID={payment.Id}, CheckoutRequestID={response.CheckoutRequestID}");

                return Ok(new
                {
                    success = true,
                    message = "Payment initiated",
                    checkoutRequestID = response.CheckoutRequestID,
                    paymentId = payment.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initiating MPESA payment: {ex.Message}");

                // Log inner exception details if present
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }

                // Return a more detailed error message
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to initiate payment. Please try again.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("status/{paymentId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CheckPaymentStatus(string paymentId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var payment = await _context.Payments
                    .Include(p => p.Debt)
                    .FirstOrDefaultAsync(p => p.Id == paymentId && p.Debt.StudentUserId == userId);

                if (payment == null)
                {
                    return NotFound(new { success = false, message = "Payment not found" });
                }

                return Ok(new
                {
                    success = true,
                    status = payment.Status.ToString(),
                    amount = payment.Amount,
                    mpesaReceiptNumber = payment.MpesaReceiptNumber,
                    paymentDate = payment.PaymentDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status");
                return StatusCode(500, new { success = false, message = "Failed to check payment status" });
            }
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
                _logger.LogInformation($"Received MPESA callback: {JsonConvert.SerializeObject(callbackData)}");

                var stkCallback = callbackData.Body?.StkCallback;
                if (stkCallback == null)
                {
                    _logger.LogWarning("Invalid callback data received");
                    return BadRequest("Invalid callback data");
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.CheckoutRequestID == stkCallback.CheckoutRequestID);

                if (payment == null)
                {
                    _logger.LogWarning($"Payment not found for CheckoutRequestID: {stkCallback.CheckoutRequestID}");
                    return NotFound("Payment not found");
                }

                payment.ResultCode = stkCallback.ResultCode;
                payment.ResultDescription = stkCallback.ResultDesc;
                payment.CompletedAt = DateTime.UtcNow;

                if (stkCallback.ResultCode == "0")
                {
                    payment.Status = PaymentStatus.Paid;
                    var metadata = stkCallback.CallbackMetadata?.Item;

                    payment.MpesaReceiptNumber = metadata?
                        .FirstOrDefault(i => i.Name == "MpesaReceiptNumber")?.Value?.ToString();
                    payment.MpesaTransactionId = metadata?
                        .FirstOrDefault(i => i.Name == "TransactionID")?.Value?.ToString();

                    payment.PaymentDate = DateTime.UtcNow;

                    // Update associated debt
                    var debt = await _context.Debt.FindAsync(payment.DebtId);
                    if (debt != null)
                    {
                        debt.CurrentBalance -= payment.Amount;
                    }
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MPESA callback");
                return StatusCode(500, "Internal server error");
            }
        }

    }
    
}
