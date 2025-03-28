using IOU.Web.Config;
using IOU.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Text;
using IOU.Web.Models;
using System.Reflection.PortableExecutable;
using System.Net.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using IOU.Web.Data;

namespace IOU.Web.Services
{
    public class MpesaPaymentService 
    {
        private readonly MpesaAuthService _authService;
        private readonly MpesaConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MpesaPaymentService> _logger;
        private readonly IOUWebContext _context;

        public MpesaPaymentService(IOptions<MpesaConfiguration> config, MpesaAuthService authService, ILogger<MpesaPaymentService> logger, IHttpClientFactory httpClientFactory, IOUWebContext context)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            _logger = logger;
            _context = context;
        }
        public async Task<dynamic> InitiatePaymentAsync(string phoneNumber, decimal amount, string accountReference)
        {
            try
            {
                // Validate inputs
                if (amount <= 0) throw new ArgumentException("Amount must be greater than 0");
                if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required");

                // Check for existing pending transactions
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber && p.Status == PaymentTransactionStatus.Pending);

                if (existingPayment != null)
                {
                    throw new Exception("A transaction is already in progress for this phone number.");
                }

                // Get authentication token
                var token = await _authService.GetAuthTokenAsync();
                _logger.LogInformation("Successfully retrieved M-Pesa auth token");

                // Generate timestamp and password
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var password = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_config.BusinessShortCode}{_config.LipaNaMpesaOnlinePassKey}{timestamp}"));

                // Create payment request payload
                var paymentRequest = new
                {
                    BusinessShortCode = _config.BusinessShortCode,
                    Password = password,
                    Timestamp = timestamp,
                    TransactionType = "CustomerPayBillOnline",
                    Amount = amount,
                    PartyA = phoneNumber,
                    PartyB = _config.BusinessShortCode,
                    PhoneNumber = phoneNumber,
                    CallBackURL = _config.CallbackUrl,
                    AccountReference = accountReference,
                    TransactionDesc = "Debt Payment"
                };

                // Create HTTP request
                var client = _httpClientFactory.CreateClient("mpesa");
                var request = new HttpRequestMessage(HttpMethod.Post, "mpesa/stkpush/v1/processrequest")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(paymentRequest), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await client.SendAsync(request);
                var rawContent = await response.Content.ReadAsStringAsync(); // Capture raw response

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"MPesa API Error: {response.StatusCode} - {rawContent}");
                    throw new Exception($"MPesa API Error: {response.StatusCode}");
                }

                // Log successful response
                _logger.LogInformation($"MPesa Response: {rawContent}");
                var responseData = JsonConvert.DeserializeObject<dynamic>(rawContent);
                if (responseData?.CheckoutRequestID == null)
                    throw new Exception("Invalid MPesa response format");

                return responseData;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"HTTP error initiating MPESA payment: {httpEx.Message}");
                if (httpEx.InnerException != null)
                    _logger.LogError($"Inner exception: {httpEx.InnerException.Message}");
                throw new Exception($"Failed to initiate MPESA payment due to network error: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in InitiatePaymentAsync: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                throw new Exception($"Failed to initiate MPESA payment: {ex.Message}", ex);
            }
        }
    }
}
