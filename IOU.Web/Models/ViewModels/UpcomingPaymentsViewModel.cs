namespace IOU.Web.Models.ViewModels
{
    public class UpcomingPaymentsViewModel
    {
        public List<ScheduledPayment> Payments { get; set; }
        public decimal TotalDue { get; set; }
        public int PaymentCount { get; set; }
        public Dictionary<string, decimal> PaymentsByMonth { get; set; }
    }
}
