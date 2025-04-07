using Newtonsoft.Json.Linq;

namespace IOU.Web.Services
{
    public class NgrokService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public event EventHandler<string> UrlChanged;
        public string _publicUrl { get; private set; }

        public NgrokService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            RefreshUrl().Wait(); // Initial fetch
        }

        public string PublicUrl
        {
            get => _publicUrl;
            private set
            {
                if (_publicUrl != value)
                {
                    _publicUrl = value;
                    UrlChanged?.Invoke(this, value);
                }
            }
        }
        public async Task RefreshUrl()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetStringAsync("http://127.0.0.1:4040/api/tunnels");
                var tunnels = JObject.Parse(response)["tunnels"];
                PublicUrl = tunnels.First(t => t["proto"].ToString() == "https")["public_url"].ToString();
            }
            catch
            {
                PublicUrl = null;
            }
        }
    }
}
