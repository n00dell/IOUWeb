using IOU.Web.Models;
using Newtonsoft.Json;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace IOU.Web.Services
{
    public class MpesaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private readonly string _passKey;
        private readonly string _shortCode;
        private readonly string _callbackUrl;
        private readonly string _environment;

        public MpesaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _consumerKey = _configuration["Mpesa:ConsumerKey"];
            _consumerSecret = _configuration["Mpesa:ConsumerSecret"];
            _passKey = _configuration["Mpesa:PassKey"];
            _shortCode = _configuration["Mpesa:ShortCode"];
            _callbackUrl = _configuration["Mpesa:CallbackUrl"];
            _environment = _configuration["Mpesa:Environment"];

            _httpClient.BaseAddress = new Uri(_environment.ToLower() == "production"
                ? "https://api.safaricom.co.ke"
                : "https://sandbox.safaricom.co.ke");
        }
        public async Task<Token> GetAccessTokenAsycn()
        {
            try
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_consumerKey}:{_consumerSecret}"));
                //_httpClient.DefaultRequestHeaders.Authorization = new AuthenicationHeaderValue("Basic", auth);

                var response = await _httpClient.GetAsync("oauth/v1/generate?grant_type=client_credentials");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Token>(content);
            }catch (Exception ex)
            {
                throw new Exception($"Error getting M-Pesa access token: {ex.Message}", ex);
            }
        }
        
    }
}
