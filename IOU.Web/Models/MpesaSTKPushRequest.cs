namespace IOU.Web.Models
{
    public class MpesaSTKPushRequest
    {
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public string StudentId { get; set; }
        public string AccountReference { get; set; }
        public string TransactionDesc { get; set; }
    }
}
