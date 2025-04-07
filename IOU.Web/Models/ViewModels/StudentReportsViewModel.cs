namespace IOU.Web.Models.ViewModels
{
    public class StudentReportsViewModel
    {
        public TotalDebtOverviewViewModel TotalDebtOverview { get; set; }
        public PaymentHistoryViewModel PaymentHistory { get; set; }
        public UpcomingPaymentsViewModel UpcomingPayments { get; set; }
    }
}
