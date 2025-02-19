namespace IOU.Web.Models.ViewModels
{
    public class LenderDashboardViewModel
    {
        // Summary Statistics
        public decimal TotalActiveDebts { get; set; }
        public decimal TotalAmountLoaned { get; set; }
        public decimal OutstandingPayments { get; set; }
        public decimal OverduePayments { get; set; }
        public int TotalActiveBorrowers { get; set; }
        public decimal RepaymentRate { get; set; }

        // Recent Activities
        //public List<DebtActivity> RecentActivities { get; set; }

        // Pending Actions
        public int PendingApplications { get; set; }
        public int UpcomingPayments { get; set; }
        public int UnreadNotifications { get; set; }

        // Performance Metrics
        public decimal MonthlyCollectionRate { get; set; }
        public decimal DefaultRate { get; set; }

        // Charts Data
        public Dictionary<string, decimal> MonthlyLoanDistribution { get; set; }
        public Dictionary<string, decimal> RepaymentTrends { get; set; }

        public List<Debt> ActiveDebts { get; set; }

    }
}
