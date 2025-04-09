using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using IOU.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static IOU.Web.Models.MpesaModels;
using System.Net.Http.Headers;

namespace IOU.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IMpesaService _mpesa;
        private readonly ILogger<PaymentController> _logger;
        private readonly IOUWebContext _context;
        private readonly IConfiguration _configuration;
        public PaymentController(
            IMpesaService mpesa,
            ILogger<PaymentController> logger,
            IOUWebContext context,
            IConfiguration configuration)
        {
            _mpesa = mpesa;
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("initiate")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                // Validate request
                if (request.Amount < 1)
                    return BadRequest(new { Code = "MIN_AMOUNT", Message = "Minimum payment is KES 1" });

                // Check existing payments
                var existingPayment = await _context.Payments
                    .AnyAsync(p => p.DebtId == request.DebtId
                        && p.Status == PaymentTransactionStatus.Pending
                        && p.CreatedAt > DateTime.UtcNow.AddMinutes(-5));

                if (existingPayment)
                    return Conflict(new { Code = "PENDING_PAYMENT", Message = "A payment is already processing" });

                // Initiate STK Push
                var stkResponse = await _mpesa.InitiateStkPushAsync(request);

                // Save payment
                var payment = new Payment
                {
                    DebtId = request.DebtId,
                    Amount = request.Amount,
                    ScheduledPaymentId = request.ScheduledPaymentId,
                    PhoneNumber = request.PhoneNumber,
                    CheckoutRequestID = stkResponse.CheckoutRequestID,
                    Status = PaymentTransactionStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    checkoutRequestId = payment.CheckoutRequestID,
                    Message = "Payment initiated successfully. Check your phone to complete payment"
                });
            }
            catch (MpesaException ex)
            {
                _logger.LogError(ex, "MPesa payment initiation failed");
                return StatusCode(500, new { Code = "MPESA_ERROR", Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment initiation error");
                return StatusCode(500, new { Code = "SERVER_ERROR", Message = "An error occurred" });
            }
        }

        [HttpPost("callback")]
        public async Task<IActionResult> HandleMpesaCallback()
        {

            try
            {
                Request.EnableBuffering();

                // Read raw body first
                var rawBody = await GetRawRequestBodyAsync();
                _logger.LogInformation("Raw callback body: {RawBody}", rawBody);

                // Try to deserialize
                MpesaCallbackWrapper callbackWrapper;
                try
                {
                    callbackWrapper = JsonConvert.DeserializeObject<MpesaCallbackWrapper>(rawBody);
                    _logger.LogInformation("Callback deserialized successfully");
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize callback payload: {Error}", jsonEx.Message);
                    // Return OK anyway to prevent M-Pesa retries
                    return Ok(new { Status = "Callback received with parsing error" });
                }

                if (callbackWrapper?.Body?.stkCallback == null)
                {
                    _logger.LogError("Invalid callback payload structure. Callback wrapper: {Wrapper}",
                        callbackWrapper != null ? "Not null" : "Null");
                    return Ok(new { Status = "Callback structure invalid" });
                }

                var callback = callbackWrapper.Body.stkCallback;
                _logger.LogInformation("Processing callback for CheckoutRequestID: {CheckoutRequestID}", callback.CheckoutRequestID);

                // First check if payment exists before proceeding
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.CheckoutRequestID == callback.CheckoutRequestID);
                if (payment.ScheduledPaymentId != null)
                {
                    // Directly apply payment to the specific installment
                    var installment = await _context.ScheduledPayment.FindAsync(payment.ScheduledPaymentId);
                    if (installment != null)
                    {
                        installment.Amount -= payment.Amount;
                        if (installment.Amount <= 0)
                            installment.Status = ScheduledPaymentStatus.Paid;
                        payment.Status = PaymentTransactionStatus.Paid;
                    }
                } 

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for CheckoutRequestID: {CheckoutRequestID}", callback.CheckoutRequestID);
                    return Ok(new { Status = "Payment not found" });
                }

                // Load the debt separately to avoid issues
                _logger.LogInformation("Loading debt for payment with ID: {PaymentId}", payment.Id);
                var debt = await _context.Debt
                    .FirstOrDefaultAsync(d => d.Id == payment.DebtId);

                if (debt == null)
                {
                    _logger.LogWarning("Debt not found for Payment with ID: {PaymentId}", payment.Id);
                    return Ok(new { Status = "Debt not found" });
                }

                // Update payment status
                _logger.LogInformation("Updating payment status to: {Status}", callback.ResultCode == 0 ? "Paid" : "Failed");
                payment.Status = callback.ResultCode == 0
                    ? PaymentTransactionStatus.Paid
                    : PaymentTransactionStatus.Failed;
                payment.ResultDescription = callback.ResultDesc;
                payment.UpdatedAt = DateTime.UtcNow;

                try
                {
                    if (payment.Status == PaymentTransactionStatus.Paid)
                    {
                        payment.MpesaReceiptNumber = callback.CallbackMetadata?.ReceiptNumber;
                        payment.PaymentDate = DateTime.UtcNow;

                        // Update debt
                        _logger.LogInformation("Updating debt balance from {OldBalance} to {NewBalance}",
                            debt.CurrentBalance, debt.CurrentBalance - payment.Amount);
                        debt.CurrentBalance -= payment.Amount;
                        if (debt.CurrentBalance <= 0)
                            debt.Status = DebtStatus.Paid;

                        // Handle installments separately
                        try
                        {
                            await AllocatePaymentToInstallments(payment);
                        }
                        catch (Exception allocEx)
                        {
                            _logger.LogError(allocEx, "Failed to allocate payment to installments, but continuing");
                            // Don't fail the whole transaction for this
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Callback processed successfully for CheckoutRequestID: {CheckoutRequestID}",
                        callback.CheckoutRequestID);
                    return Ok(new { Status = "Callback processed" });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save payment updates");
                    return StatusCode(500, new { Status = "Error saving payment updates" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MPesa callback processing failed with error: {Error}", ex.Message);
                return StatusCode(500, new { Status = "Error processing callback" });
            }
        }


        private async Task<string> GetRawRequestBodyAsync()
        {
            Request.Body.Position = 0;  // Ensure the stream position is at the beginning
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;  // Reset the position so model binding or other reads aren't affected
            return body;
        }

        private async Task AllocatePaymentToInstallments(Payment payment)
        {
            _logger.LogInformation("Starting payment allocation for payment ID: {PaymentId}", payment.Id);

            var debt = await _context.Debt
                .Include(d => d.ScheduledPayments)
                .FirstOrDefaultAsync(d => d.Id == payment.DebtId);

            if (debt == null)
            {
                _logger.LogWarning("Debt not found during allocation for payment ID: {PaymentId}", payment.Id);
                return;
            }

            var remainingAmount = payment.Amount;
            _logger.LogInformation("Allocating payment amount: {Amount}", remainingAmount);

            var unpaidInstallments = debt.ScheduledPayments
                .Where(sp => sp.Status != ScheduledPaymentStatus.Paid)
                .OrderBy(sp => sp.DueDate)
                .ToList();

            _logger.LogInformation("Found {Count} unpaid installments", unpaidInstallments.Count);

            foreach (var installment in unpaidInstallments)
            {
                if (remainingAmount <= 0) break;

                var amountToApply = Math.Min(remainingAmount, installment.Amount);
                _logger.LogInformation("Applying {Amount} to installment ID: {InstallmentId}",
                    amountToApply, installment.Id);

                // Create payment record for this installment - CRITICAL FIX HERE
                var installmentPayment = new Payment
                {
                    DebtId = payment.DebtId,
                    ScheduledPaymentId = installment.Id,
                    Amount = amountToApply,
                    PhoneNumber = payment.PhoneNumber,
                    Status = PaymentTransactionStatus.Paid,
                    MpesaReceiptNumber = payment.MpesaReceiptNumber,
                    PaymentDate = payment.PaymentDate ?? DateTime.UtcNow,
                    CheckoutRequestID = payment.CheckoutRequestID + "-alloc-" + installment.Id.Substring(0, 8),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ResultDescription = "Installment allocation"
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
                _logger.LogInformation("Handling overpayment of {Amount}", remainingAmount);

                // Optionally create a credit record - CRITICAL FIX HERE TOO
                var overpayment = new Payment
                {
                    DebtId = payment.DebtId,
                    Amount = remainingAmount,
                    PhoneNumber = payment.PhoneNumber,
                    Status = PaymentTransactionStatus.Paid,
                    MpesaReceiptNumber = payment.MpesaReceiptNumber,
                    PaymentDate = payment.PaymentDate ?? DateTime.UtcNow,
                    CheckoutRequestID = payment.CheckoutRequestID + "-overpay",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ResultDescription = "Overpayment credit"
                };
                _context.Payments.Add(overpayment);
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment allocation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving payment allocations");
                throw; // Re-throw to be caught by caller
            }
        }
        [HttpGet("test-mpesa")]
        public async Task<IActionResult> TestMpesa()
        {
            try
            {
                // Create a basic HTTP client
                using var client = new HttpClient();
                client.BaseAddress = new Uri("https://sandbox.safaricom.co.ke/");

                // Get auth token
                var consumerKey = _configuration["MpesaConfiguration:ConsumerKey"];
                var consumerSecret = _configuration["MpesaConfiguration:ConsumerSecret"];

                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    $"{consumerKey}:{consumerSecret}"));
                var useNgrok = _configuration.GetValue<bool>("MpesaConfiguration:UseNgrok");
                var ngrokService = HttpContext.RequestServices.GetRequiredService<NgrokService>();
                var callbackUrl = useNgrok && !string.IsNullOrEmpty(ngrokService.PublicUrl)
            ? $"{ngrokService.PublicUrl}/api/payment/callback"
            : _configuration["MpesaConfiguration:CallbackUrl"];

                // Test that the callback URL is valid
                _logger.LogInformation($"Using callback URL: {callbackUrl}");
                var authRequest = new HttpRequestMessage(HttpMethod.Get, "oauth/v1/generate?grant_type=client_credentials");
                authRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

                var authResponse = await client.SendAsync(authRequest);
                authResponse.EnsureSuccessStatusCode();

                var authContent = await authResponse.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<dynamic>(authContent);
                var accessToken = tokenResponse.access_token.ToString();

                // Prepare STK Push
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var businessShortCode = "174379";
                var passKey = "bfb279f9aa9bdbcf158e97dd71a467cd2e0c893059b10f78e6b72ada1ed2c919";
                var password = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    $"{businessShortCode}{passKey}{timestamp}"));

                var stkRequest = new
                {
                    BusinessShortCode = businessShortCode,
                    Password = password,
                    Timestamp = timestamp,
                    TransactionType = "CustomerPayBillOnline",
                    Amount = "1",
                    PartyA = "254724373737",
                    PartyB = businessShortCode,
                    PhoneNumber = "254724373737",
                    CallBackURL = callbackUrl,
                    AccountReference = "Test",
                    TransactionDesc = "Test payment"
                };

                var stkJson = JsonConvert.SerializeObject(stkRequest);
                _logger.LogInformation("STK Request: {Request}", stkJson);

                var stkContent = new StringContent(stkJson, Encoding.UTF8, "application/json");

                var stkMessage = new HttpRequestMessage(HttpMethod.Post, "mpesa/stkpush/v1/processrequest");
                stkMessage.Content = stkContent;
                stkMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var stkResponse = await client.SendAsync(stkMessage);
                var responseContent = await stkResponse.Content.ReadAsStringAsync();

                return Ok(new
                {
                    IsSuccess = stkResponse.IsSuccessStatusCode,
                    StatusCode = stkResponse.StatusCode,
                    Response = responseContent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestMpesa error");
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("status/{checkoutRequestId}")]
        public async Task<IActionResult> CheckPaymentStatus(string checkoutRequestId)
        {
            try
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.CheckoutRequestID == checkoutRequestId);

                if (payment == null)
                    return NotFound(new { Status = "Not found", CheckoutRequestID = checkoutRequestId });

                return Ok(new
                {
                    status = payment.Status.ToString(),
                    confirmed = payment.Status == PaymentTransactionStatus.Paid,
                    amount = payment.Amount,
                    receipt = payment.MpesaReceiptNumber,
                    checkoutRequestId = payment.CheckoutRequestID,
                    lastUpdated = payment.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status");
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }

    }
}
