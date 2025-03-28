using Newtonsoft.Json;

namespace IOU.Web.Models
{
    public class MpesaCallbackResponse
    {
        [JsonProperty("Body")]
        public Body Body { get; set; }
    }
    public class Body
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
        public string ResultCode { get; set; }
        [JsonProperty("ResultDesc")]
        public string ResultDesc { get; set; }
        [JsonProperty("CallbackMetadata")]
        public CallbackMetadata CallbackMetadata { get; set; }
    }

    public class CallbackMetadata
    {
        [JsonProperty("Item")]
        public List<Item> Item { get; set; }
    }

    public class Item
    {
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Value")]
        public object Value { get; set; }
    }
}
