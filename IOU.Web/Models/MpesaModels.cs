using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models
{
    public class MpesaModels
    {
        public class PaymentRequest
        {
            [Required] public string DebtId { get; set; }
            [Range(1.00, double.MaxValue)] public decimal Amount { get; set; }
            [Required, RegularExpression(@"^(0|254)?[7][0-9]{8}$")]
            public string PhoneNumber { get; set; }
            public string? ScheduledPaymentId { get; set; }
        }

        public class MpesaToken
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("expires_in")]
            public string ExpiresIn { get; set; }
        }

        public class MpesaInitiateResponse
        {
            public string MerchantRequestID { get; set; }
            public string CheckoutRequestID { get; set; }
            public string ResponseCode { get; set; }
            public string CustomerMessage { get; set; }
            public bool IsSuccess => ResponseCode == "0";
        }

        public class CallbackItem
        {
            [JsonProperty(PropertyName = "Name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "Value")]
            public object Value { get; set; }
        }

        public class CallbackMetadata
        {
            [JsonProperty(PropertyName = "Item")]
            public List<CallbackItem> Item { get; set; }

            public decimal Amount => GetItemValue<decimal>("Amount");
            public string ReceiptNumber => GetItemValue<string>("MpesaReceiptNumber");

            private T GetItemValue<T>(string name) =>
                Item?.FirstOrDefault(i => i.Name == name)?.Value is T value ? value : default;
        }

        public class MpesaStkCallback
        {
            [JsonProperty(PropertyName = "MerchantRequestID")]
            public string MerchantRequestID { get; set; }

            [JsonProperty(PropertyName = "CheckoutRequestID")]
            public string CheckoutRequestID { get; set; }

            [JsonProperty(PropertyName = "ResultCode")]
            public int ResultCode { get; set; }

            [JsonProperty(PropertyName = "ResultDesc")]
            public string ResultDesc { get; set; }

            [JsonProperty(PropertyName = "CallbackMetadata")]
            public CallbackMetadata CallbackMetadata { get; set; }
        }

        public class MpesaCallbackBody
        {
            [JsonProperty(PropertyName = "stkCallback")]
            public MpesaStkCallback stkCallback { get; set; }
        }

        public class MpesaCallbackWrapper
        {
            [JsonProperty(PropertyName = "Body")]
            public MpesaCallbackBody Body { get; set; }
        }

    }
}
