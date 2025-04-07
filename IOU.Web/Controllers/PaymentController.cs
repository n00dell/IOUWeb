using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static IOU.Web.Models.MpesaModels;

namespace IOU.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IMpesaService _mpesa;
        private readonly ILogger<PaymentController> _logger;
        private readonly IOUWebContext _context;

        public PaymentController(
            IMpesaService mpesa,
            ILogger<PaymentController> logger,
            IOUWebContext context)
        {
            _mpesa = mpesa;
            _logger = logger;
            _context = context;
        }
        [HttpPost("initiate")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Initiating payment for {Phone}, Amount: {Amount}",
            request.PhoneNumber, request.Amount);

                if (request.Amount < 10)
                    return BadRequest(new { Message = "Minimum payment is KES 10" });
                // Validate duplicate pending payments
                var duplicatePayment = await _context.Payments
                    .Where(p => p.DebtId == request.DebtId
                        && p.PhoneNumber == request.PhoneNumber
                        && p.Status == PaymentTransactionStatus.Pending
                        && p.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
                    .FirstOrDefaultAsync();

                if (duplicatePayment != null)
                {
                    return Conflict(new
                    {
                        duplicatePayment.CheckoutRequestID,
                        Message = "A similar payment is already processing"
                    });
                }

                var stkResponse = await _mpesa.InitiateStkPushAsync(request);

                var payment = new Payment
                {
                    DebtId = request.DebtId,
                    Amount = request.Amount,
                    PhoneNumber = request.PhoneNumber,
                    CheckoutRequestID = stkResponse.CheckoutRequestID,
                    Status = PaymentTransactionStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Initiated payment for debt {DebtId}. CheckoutID: {CheckoutId}",
                    request.DebtId, stkResponse.CheckoutRequestID);

                return Ok(new
                {
                    Success = true,
                    CheckoutRequestID = payment.CheckoutRequestID,
                    Message = stkResponse.CustomerMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment initiation failed for debt {DebtId}", request?.DebtId);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Payment initiation failed. Please try again."
                });
            }
        }

        [HttpPost("callback")]
        public async Task<IActionResult> HandleMpesaCallback([FromBody] MpesaCallback request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("MPesa callback received: {@Request}", request);

                // Signature verification
                if (!await _mpesa.VerifyCallbackSignature(Request))
                {
                    _logger.LogWarning("Invalid callback signature");
                    return Unauthorized(new { Status = "Invalid signature" });
                }

                var callback = request.Body.StkCallback;
                var payment = await _context.Payments
                    .Include(p => p.Debt)
                    .FirstOrDefaultAsync(p => p.CheckoutRequestID == callback.CheckoutRequestID);

                if (payment == null)
                {
                    _logger.LogError("Payment not found for CheckoutID: {CheckoutId}", callback.CheckoutRequestID);
                    return NotFound(new { Status = "Payment not found" });
                }

                // Idempotency check
                if (payment.Status != PaymentTransactionStatus.Pending)
                {
                    _logger.LogWarning("Payment already processed as {Status}", payment.Status);
                    return Ok(new { Status = "Already processed" });
                }

                // Update payment status
                payment.Status = callback.ResultCode == 0
                    ? PaymentTransactionStatus.Paid
                    : PaymentTransactionStatus.Failed;
                payment.UpdatedAt = DateTime.UtcNow;
                payment.ResultDescription = callback.ResultDesc;

                if (payment.Status == PaymentTransactionStatus.Paid)
                {
                    payment.MpesaReceiptNumber = callback.CallbackMetadata?.ReceiptNumber;
                    payment.PaymentDate = DateTime.UtcNow;

                    // Update debt balance
                    payment.Debt.CurrentBalance -= payment.Amount;
                    if (payment.Debt.CurrentBalance <= 0)
                    {
                        payment.Debt.Status = DebtStatus.Paid;
                    }

                    await AllocatePaymentToInstallments(payment);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Processed callback for payment {PaymentId}", payment.Id);
                return Ok(new { Status = "Processed successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "MPesa callback processing failed");
                return StatusCode(500, new { Status = "Processing error" });
            }
        }
        [HttpGet("payment-status/{checkoutRequestId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetPaymentStatus(string checkoutRequestId)
        {
            var payment = await _context.Payments
                .Where(p => p.CheckoutRequestID == checkoutRequestId)
                .Select(p => new {
                    p.Status,
                    p.ResultDescription,
                    p.MpesaReceiptNumber,
                    p.Amount
                })
                .FirstOrDefaultAsync();

            if (payment == null)
                return NotFound(new { Message = "Payment not found" });

            return Ok(new
            {
                status = payment.Status.ToString(),
                confirmed = payment.Status == PaymentTransactionStatus.Paid,
                receipt = payment.MpesaReceiptNumber,
                amount = payment.Amount,
                error = payment.ResultDescription
            });
        }
        private async Task AllocatePaymentToInstallments(Payment payment)
        {
            var debt = await _context.Debt
                .Include(d => d.ScheduledPayments)
                .FirstOrDefaultAsync(d => d.Id == payment.DebtId);

            if (debt == null) return;

            var remainingAmount = payment.Amount;
            var unpaidInstallments = debt.ScheduledPayments
                .Where(sp => sp.Status != ScheduledPaymentStatus.Paid)
                .OrderBy(sp => sp.DueDate)
                .ToList();

            foreach (var installment in unpaidInstallments)
            {
                if (remainingAmount <= 0) break;

                var amountToApply = Math.Min(remainingAmount, installment.Amount);

                // Create payment record for this installment
                var installmentPayment = new Payment
                {
                    DebtId = payment.DebtId,
                    ScheduledPaymentId = installment.Id,
                    Amount = amountToApply,
                    PhoneNumber = payment.PhoneNumber,
                    Status = PaymentTransactionStatus.Paid,
                    MpesaReceiptNumber = payment.MpesaReceiptNumber,
                    PaymentDate = payment.PaymentDate
                };

                _context.Payments.Add(installmentPayment);

                // Update installment status
                installment.Amount -= amountToApply;
                if (installment.Amount <= 0)
                {
                    installment.Status = ScheduledPaymentStatus.Paid;
                    installment.PaymentDate = DateTime.UtcNow;
                }

                remainingAmount -= amountToApply;
            }

            // Handle any remaining amount (overpayment)
            if (remainingAmount > 0)
            {
                debt.CurrentBalance -= remainingAmount;

                // Optionally create a credit record
                var overpayment = new Payment
                {
                    DebtId = payment.DebtId,
                    Amount = remainingAmount,
                    PhoneNumber = payment.PhoneNumber,
                    Status = PaymentTransactionStatus.Paid,
                    MpesaReceiptNumber = payment.MpesaReceiptNumber,
                    PaymentDate = payment.PaymentDate,
                    ResultDescription = "Overpayment credit"
                };
                _context.Payments.Add(overpayment);
            }

            await _context.SaveChangesAsync();
        }
    }
}
