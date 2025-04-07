using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models
{
    public class MpesaModels
    {
        public class PaymentRequest
        {
            [Required] public string DebtId { get; set; }
            [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
            [Required, RegularExpression(@"^(0|254)?[7][0-9]{8}$")]
            public string PhoneNumber { get; set; }
        }

        public class MpesaResponse
        {
            public string MerchantRequestID { get; set; }
            public string CheckoutRequestID { get; set; }
            public string ResponseCode { get; set; }
            public string CustomerMessage { get; set; }
            public bool IsSuccess => ResponseCode == "0";
        }

        public class CallbackMetadata
        {
            public List<CallbackItem> Items { get; set; }
            public decimal Amount => GetItemValue<decimal>("Amount");
            public string ReceiptNumber => GetItemValue<string>("MpesaReceiptNumber");

            private T GetItemValue<T>(string name) =>
                Items?.FirstOrDefault(i => i.Name == name)?.Value is T value ? value : default;
        }

        public class CallbackItem
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}
