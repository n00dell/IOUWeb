using IOU.Web.Config;
using IOU.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Text;

namespace IOU.Web.Services
{
    public class MpesaPaymentService 
    {
        private readonly MpesaAuthService _authService;
        private readonly MpesaConfiguration _config;
        private readonly RestClient _client;

        public MpesaPaymentService(IOptions<MpesaConfiguration> config, MpesaAuthService authService)
        {
            _config = config.Value;
            _client = new RestClient(_config.BaseUrl);
            _authService = authService;
        }
        public async Task<dynamic> InitiatePaymentAsync(string phoneNumber, decimal amount, string accountReference)
        {
            var token = await _authService.GetAuthTokenAsync();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.BusinessShortCode}{_config.LipaNaMpesaOnlinePassKey}{timestamp}"));

            var request = new RestRequest("mpesa/stkpush/v1/processrequest", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new
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
            });

            var response = await _client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<dynamic>(response.Content);
            }
            throw new Exception("Failed to initiate MPESA payment.");
        }
    }
}
