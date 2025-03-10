using Newtonsoft.Json;

namespace IOU.Web.Models
{
    public class Token
    {
        [JsonProperty ("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty ("expires_in")]
        public string ExpiresIn { get; set; }
    }
}
