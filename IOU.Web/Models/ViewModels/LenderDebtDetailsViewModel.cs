namespace IOU.Web.Models.ViewModels
{
    public class LenderDebtDetailsViewModel
    {
        public Debt Debt { get; set; }
        public List<ScheduledPayment> ScheduledPayments { get; set; }

        // Payment statistics
        public decimal TotalPaid => ScheduledPayments?.Where(p => p.Status == ScheduledPaymentStatus.Paid)?.Sum(p => p.Amount) ?? 0;
        public int RemainingPaymentsCount => ScheduledPayments?.Count(p => p.Status != ScheduledPaymentStatus.Paid) ?? 0;
        public decimal RemainingBalance => Debt?.CurrentBalance ?? 0;

        // Payment status information
        public int OnTimePayments => ScheduledPayments?.Count(p => p.Status == ScheduledPaymentStatus.Paid && p.PaymentDate <= p.DueDate) ?? 0;
        public int LatePayments => ScheduledPayments?.Count(p => p.Status == ScheduledPaymentStatus.Paid && p.PaymentDate > p.DueDate) ?? 0;
        public int MissedPayments => ScheduledPayments?.Count(p => p.Status == ScheduledPaymentStatus.Overdue) ?? 0;

        // Outstanding payments
        public List<ScheduledPayment> OverduePayments => ScheduledPayments?
            .Where(p => p.Status != ScheduledPaymentStatus.Paid && p.DueDate < DateTime.Now)
            .OrderBy(p => p.DueDate)
            .ToList() ?? new List<ScheduledPayment>();

        public List<ScheduledPayment> UpcomingPayments => ScheduledPayments?
            .Where(p => p.Status != ScheduledPaymentStatus.Paid && p.DueDate >= DateTime.Now)
            .OrderBy(p => p.DueDate)
            .Take(5)
            .ToList() ?? new List<ScheduledPayment>();

        // Payment progress
        public decimal PaymentProgress => TotalPaid > 0 ? (TotalPaid / (TotalPaid + RemainingBalance) * 100) : 0;
    }
}
