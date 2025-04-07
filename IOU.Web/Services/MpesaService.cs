using IOU.Web.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using IOU.Web.Data;
using System.Net.Http.Headers;
using IOU.Web.Services.Interfaces;
using static IOU.Web.Models.MpesaModels;
using System.Security.Cryptography;

namespace IOU.Web.Services
{
    public class MpesaService : IMpesaService
    {
        
        private readonly MpesaConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MpesaService> _logger;
        private string _accessToken;
        private DateTime _tokenExpiry;
        private readonly IWebHostEnvironment _environment;
        private readonly NgrokService _ngrokService;
        private string _currentCallbackUrl;
        public MpesaService(
            IOptions<MpesaConfiguration> config,
            IHttpClientFactory httpClientFactory,
            ILogger<MpesaService> logger,
            IWebHostEnvironment environment,
            NgrokService ngrokService)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _environment = environment;
            _ngrokService = ngrokService;
            _ngrokService.UrlChanged += (sender, newUrl) =>
            {
                _currentCallbackUrl = $"{newUrl}/api/mpesa/callback";
                _logger.LogInformation("Ngrok URL updated to: {Url}", _currentCallbackUrl);
            };
        }

        public async Task<string> GetAuthTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            var client = _httpClientFactory.CreateClient();
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{_config.ConsumerKey}:{_config.ConsumerSecret}"));

            var response = await client.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_config.BaseUrl}/oauth/v1/generate?grant_type=client_credentials"),
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", authString) }
            });

            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Auth failed: {content}");

            var tokenResponse = JsonConvert.DeserializeObject<dynamic>(content);
            _accessToken = tokenResponse.access_token;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(Convert.ToDouble(tokenResponse.expires_in) - 60);

            return _accessToken;
        }
        public async Task<MpesaResponse> InitiateStkPushAsync(PaymentRequest request)
        {
            var client = _httpClientFactory.CreateClient("Mpesa");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await GetAuthTokenAsync());

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{_config.BusinessShortCode}{_config.LipaNaMpesaOnlinePassKey}{timestamp}"));

            _logger.LogInformation("Initiating STK Push: {PhoneNumber}, Amount: {Amount}, Timestamp: {Timestamp}",
                request.PhoneNumber, request.Amount, timestamp);
            _logger.LogInformation("Password: {Password}", password);
            _logger.LogInformation("Callback URL: {CallbackUrl}", GetCallbackUrl());
            _logger.LogInformation("Business Short Code: {BusinessShortCode}",
                _config.BusinessShortCode.ToString("D6"));

            var stkRequest = new
            {
                BusinessShortCode = _config.BusinessShortCode,
                Password = password,
                Timestamp = timestamp,
                TransactionType = "CustomerPayBillOnline",
                Amount = Math.Ceiling(request.Amount).ToString("0"),
                PartyA = FormatPhoneNumber(request.PhoneNumber),
                PartyB = _config.BusinessShortCode,
                PhoneNumber = FormatPhoneNumber(request.PhoneNumber),
                CallBackURL = GetCallbackUrl(),
                AccountReference = "IOU-Debt",
                TransactionDesc = $"Debt {request.DebtId}"
            };

            var response = await client.PostAsJsonAsync(
                "mpesa/stkpush/v1/processrequest", stkRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("MPesa API Error: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);
                throw new Exception($"MPesa request failed: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<MpesaResponse>(content);

            if (result == null || string.IsNullOrEmpty(result.CheckoutRequestID))
            {
                throw new Exception("Invalid MPesa response format");
            }

            return result;
        }
        private async Task<T> HandleMpesaResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"MPesa API Error: {content}");
                throw new Exception($"MPesa request failed: {response.StatusCode}");
            }

            var result = JsonConvert.DeserializeObject<T>(content);
            if (result == null) throw new Exception("Failed to parse MPesa response");

            return result;
        }
        private string FormatPhoneNumber(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.StartsWith("254") ? digits : "254" + digits[^9..];
        }

        private string GetCallbackUrl()
        {
            // If using Ngrok in development, get the current URL from NgrokService
            if (_config.UseNgrok && _environment.IsDevelopment())
            {
                var ngrokUrl = _ngrokService.PublicUrl?.TrimEnd('/');
                if (!string.IsNullOrEmpty(ngrokUrl))
                {
                    return $"{ngrokUrl}/api/mpesa/callback";
                }
            }

            // Fall back to configured URL
            return _config.CallbackUrl;
        }

        public async Task<bool> VerifyCallbackSignature(HttpRequest request)
        {
            try
            {
                var signature = request.Headers["Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Missing signature header");
                    return false;
                }

                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.LipaNaMpesaOnlinePassKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var computedSignature = Convert.ToBase64String(hash);

                return signature == computedSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Signature verification failed");
                return false;
            }
        }
    }
}