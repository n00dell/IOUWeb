using IOU.Web.Config;
using IOU.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Text;

namespace IOU.Web.Services
{
    public class MpesaAuthService
    {
        private readonly MpesaConfiguration _config;
        private readonly RestClient _client;
        private readonly ILogger<MpesaAuthService> _logger;

        public MpesaAuthService(IOptions<MpesaConfiguration> config, ILogger<MpesaAuthService> logger)
        {
            _config = config.Value;
            _client = new RestClient(_config.BaseUrl);
            _logger = logger;
        }
        public async Task<string> GetAuthTokenAsync()
        {
            try
            {
                var request = new RestRequest("oauth/v1/generate?grant_type=client_credentials", Method.Get);
                request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ConsumerKey}:{_config.ConsumerSecret}"))}");

                var response = await _client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    return tokenResponse.access_token;
                }
                _logger.LogError($"Failed to authenticate with MPESA API. Status: {response.StatusCode}, Response: {response.Content}");
                throw new Exception("Failed to authenticate with MPESA API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthTokenAsync");
                throw;
            }
            }
    }
}
