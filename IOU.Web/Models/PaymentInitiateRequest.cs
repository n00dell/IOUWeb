namespace IOU.Web.Models
{
    public class PaymentInitiateRequest
    {
        public string DebtId { get; set; }
        public decimal Amount { get; set; }
        public string PhoneNumber { get; set; }
    }
}
