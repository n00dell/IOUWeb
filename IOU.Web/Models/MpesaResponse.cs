using Newtonsoft.Json;

namespace IOU.Web.Models
{
    // Add this to your MpesaModels.cs
    public class MpesaResponse
    {
        [JsonProperty("MerchantRequestID")]
        public string MerchantRequestID { get; set; }

        [JsonProperty("CheckoutRequestID")]
        public string CheckoutRequestID { get; set; }

        [JsonProperty("ResponseCode")]
        public string ResponseCode { get; set; }

        [JsonProperty("ResponseDescription")]
        public string ResponseDescription { get; set; }

        [JsonProperty("CustomerMessage")]
        public string CustomerMessage { get; set; }

        public bool IsSuccess => ResponseCode == "0";
    }
}
