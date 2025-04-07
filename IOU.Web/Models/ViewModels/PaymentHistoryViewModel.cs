namespace IOU.Web.Models.ViewModels
{
    public class PaymentHistoryViewModel
    {
        public List<ScheduledPayment> Payments { get; set; }
        public decimal TotalPaid { get; set; }
        public int PaymentCount { get; set; }
        public Dictionary<string, decimal> PaymentsByMonth { get; set; }
    }
}
