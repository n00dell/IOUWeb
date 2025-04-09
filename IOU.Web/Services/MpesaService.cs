// Simplified MpesaService.cs
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using IOU.Web.Config;
using IOU.Web.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using IOU.Web.Services.Interfaces;
using static IOU.Web.Models.MpesaModels;

namespace IOU.Web.Services
{
    

    public class MpesaService : IMpesaService
    {
        private readonly MpesaConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MpesaService> _logger;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public MpesaService(
            IOptions<MpesaConfiguration> config,
            IHttpClientFactory httpClientFactory,
            ILogger<MpesaService> logger)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<MpesaInitiateResponse> InitiateStkPushAsync(PaymentRequest request)
        {
            var client = _httpClientFactory.CreateClient("Mpesa");

            _logger.LogInformation("MPesa Configuration: {Config}", JsonConvert.SerializeObject(_config));
            _logger.LogInformation("Base address: {BaseAddress}", client.BaseAddress);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await GetAuthTokenAsync());

            var callbackUrl = _config.UseNgrok && !string.IsNullOrEmpty(_config.NgrokUrl)
                 ? $"{_config.NgrokUrl.TrimEnd('/')}/api/payment/callback"
                 : _config.CallbackUrl;

            _logger.LogInformation("Using callback URL: {CallbackUrl}", callbackUrl);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var password = GenerateStkPassword(timestamp);

            var businessShortCode = _config.BusinessShortCode.Trim();

            var phoneNumber = FormatPhoneNumber(request.PhoneNumber);

            // Log the values we're about to send for debugging
            _logger.LogInformation("STK Push Request: BusinessShortCode={ShortCode}, Phone={Phone}, Amount={Amount}",
                businessShortCode, phoneNumber, request.Amount);

            var stkRequest = new
            {
                BusinessShortCode = businessShortCode,
                Password = password,
                Timestamp = timestamp,
                TransactionType = "CustomerPayBillOnline",
                Amount = (int)(request.Amount * 1), // MPesa might expect whole numbers
                PartyA = phoneNumber,
                PartyB = businessShortCode,
                PhoneNumber = phoneNumber, // Use the same formatted phone number
                CallBackURL = callbackUrl,
                AccountReference = $"IOU-Debt-{request.DebtId}",
                TransactionDesc = $"Debt {request.DebtId}"
            };

            var requestUrl = $"mpesa/stkpush/v1/processrequest";
            _logger.LogInformation("Request URL: {RequestUrl}", requestUrl);

            // Log the full request body for debugging
            var requestJson = JsonConvert.SerializeObject(stkRequest);
            _logger.LogInformation("Request Body: {RequestBody}", requestJson);

            // Create proper content
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUrl, content);

            return await HandleMpesaResponse<MpesaInitiateResponse>(response);
        }

        public async Task<bool> VerifyCallbackSignature(string signature, string body)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.LipaNaMpesaOnlinePassKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var computedSignature = Convert.ToBase64String(hash);

                return signature == computedSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MPesa signature verification failed");
                return false;
            }
        }

        private async Task<string> GetAuthTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            // Use a new HttpClient just for auth to avoid any conflicts
            using var client = new HttpClient();

            // Set the base address for this specific client
            client.BaseAddress = new Uri(_config.BaseUrl);

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{_config.ConsumerKey}:{_config.ConsumerSecret}"));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authString);

            // Now use a relative path since BaseAddress is set
            var response = await client.GetAsync("/oauth/v1/generate?grant_type=client_credentials");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Auth failed: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<MpesaToken>(content);

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(Convert.ToInt32(tokenResponse.ExpiresIn) - 60);

            return _accessToken;
        }

        private string GenerateStkPassword(string timestamp)
        {
            
            var data = $"{_config.BusinessShortCode.Trim()}{_config.LipaNaMpesaOnlinePassKey.Trim()}{timestamp}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }

        private string FormatPhoneNumber(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.StartsWith("254") ? digits : $"254{digits.TakeLast(9).ToArray()}";
        }

        private async Task<T> HandleMpesaResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MPesa API Error: {StatusCode} - {Content}",
                    response.StatusCode, content);
                try
                {
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(content);
                    var errorMessage = errorObj?.errorMessage?.ToString() ?? "Unknown error";
                    throw new MpesaException($"MPesa API Error: {response.StatusCode} - {errorMessage}");
                }
                catch (Exception)
                {
                    // Fall back to generic error if parsing fails
                    throw new MpesaException($"MPesa API Error: {response.StatusCode}");
                }
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize MPesa response");
                throw new MpesaException("Invalid MPesa response format");
            }
        }
    }

    public class MpesaException : Exception
    {
        public MpesaException(string message) : base(message) { }
    }
}