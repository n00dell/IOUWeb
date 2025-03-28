namespace IOU.Web.Models.ViewModels
{
    public class DebtDetailsViewModel
    {
        // Basic debt information
        public Debt Debt { get; set; }

        // Payment information
        public List<ScheduledPayment> UpcomingPayments { get; set; }
        public ScheduledPayment NextPayment { get; set; }
        public decimal TotalPaid { get; set; }
        public int RemainingPaymentsCount { get; set; } // Renamed from RemainingAmount for clarity
        public decimal RemainingBalance => Debt?.CurrentBalance ?? 0;

        // Payment progress
        public decimal PaymentProgress => CalculatePaymentProgress();
        public string NextPaymentDueIn => CalculateNextPaymentTimeframe();
        public bool IsOverdue => NextPayment?.DueDate < DateTime.Now && NextPayment?.Status != ScheduledPaymentStatus.Paid;

        private decimal CalculatePaymentProgress()
        {
            if (TotalPaid == 0 && RemainingBalance == 0)
                return 0;

            return TotalPaid / (TotalPaid + RemainingBalance) * 100;
        }

        private string CalculateNextPaymentTimeframe()
        {
            if (NextPayment == null)
                return "No payments scheduled";

            var daysUntilDue = (NextPayment.DueDate - DateTime.Now).Days;

            if (daysUntilDue < 0)
                return "Overdue by " + Math.Abs(daysUntilDue) + " days";
            else if (daysUntilDue == 0)
                return "Due today";
            else if (daysUntilDue == 1)
                return "Due tomorrow";
            else
                return "Due in " + daysUntilDue + " days";
        }
    }
}
