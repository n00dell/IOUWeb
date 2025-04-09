namespace IOU.Web.Models.ViewModels
{
    public class PaymentHistoryViewModel
    {
        public List<Payment> PaymentTransactions { get; set; }  // Changed from Payments
        public decimal TotalPaid { get; set; }
        public decimal TotalAttempted { get; set; }
        public int PaymentCount { get; set; }
        public int FailedPayments { get; set; }
        public Dictionary<string, decimal> PaymentsByMonth { get; set; }
    }
}
