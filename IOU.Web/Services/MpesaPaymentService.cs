using IOU.Web.Config;
using IOU.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Text;
using IOU.Web.Models;
using System.Reflection.PortableExecutable;
using System.Net.Http;

namespace IOU.Web.Services
{
    public class MpesaPaymentService 
    {
        private readonly MpesaAuthService _authService;
        private readonly MpesaConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MpesaPaymentService> _logger;

        public MpesaPaymentService(IOptions<MpesaConfiguration> config, MpesaAuthService authService, ILogger<MpesaPaymentService> logger, IHttpClientFactory httpClientFactory)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            _logger = logger;
            
        }
        public async Task<dynamic> InitiatePaymentAsync(string phoneNumber, decimal amount, string accountReference)
        {
            try
            {
                // Get authentication token
                var token = await _authService.GetAuthTokenAsync();

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

                // Send request
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Process response
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Payment initiation successful: {content}");

                // Parse the response to get CheckoutRequestID
                var responseData = JsonConvert.DeserializeObject<dynamic>(content);
                if (responseData == null || responseData.CheckoutRequestID == null)
                {
                    throw new Exception("Invalid response from MPESA API: CheckoutRequestID is missing.");
                }

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
