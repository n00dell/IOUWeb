using Newtonsoft.Json;
using static IOU.Web.Models.MpesaModels;

namespace IOU.Web.Models
{
    public class MpesaCallback
    {
        [JsonProperty("Body")]
        public CallbackBody Body { get; set; }
    }

    public class CallbackBody
    {
        [JsonProperty("stkCallback")]
        public StkCallback StkCallback { get; set; }
    }

    public class StkCallback
    {
        [JsonProperty("MerchantRequestID")]
        public string MerchantRequestID { get; set; }

        [JsonProperty("CheckoutRequestID")]
        public string CheckoutRequestID { get; set; }

        [JsonProperty("ResultCode")]
        public int ResultCode { get; set; }

        [JsonProperty("ResultDesc")]
        public string ResultDesc { get; set; }

        [JsonProperty("CallbackMetadata")]
        public CallbackMetadata CallbackMetadata { get; set; }
    }
}
