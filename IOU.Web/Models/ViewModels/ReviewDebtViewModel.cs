using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class ReviewDebtViewModel
    {
        // Basic debt information
        public Debt Debt { get; set; }
        public List<ScheduledPayment> ScheduledPayments { get; set; }

        // Summary statistics
        public decimal TotalInterest => CalculateTotalInterest();
        public decimal TotalAmountToRepay => Debt?.PrincipalAmount + TotalInterest ?? 0;
        public decimal MonthlyPayment => CalculateAverageMonthlyPayment();
        public DateTime LastPaymentDate => ScheduledPayments?.OrderByDescending(p => p.DueDate)?.FirstOrDefault()?.DueDate ?? DateTime.MinValue;

        // Review/approval related properties
        [Display(Name = "Your Decision")]
        public DebtReviewDecision Decision { get; set; }

        [Display(Name = "Comments")]
        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
        public string ReviewComments { get; set; }

        private decimal CalculateTotalInterest()
        {
            if (ScheduledPayments == null || !ScheduledPayments.Any())
                return 0;

            return ScheduledPayments.Sum(p => p.Amount) - (Debt?.PrincipalAmount ?? 0);
        }

        private decimal CalculateAverageMonthlyPayment()
        {
            if (ScheduledPayments == null || !ScheduledPayments.Any())
                return 0;

            // Group by month and calculate average
            var paymentsByMonth = ScheduledPayments
                .GroupBy(p => new { p.DueDate.Year, p.DueDate.Month })
                .Select(g => g.Sum(p => p.Amount));

            return paymentsByMonth.Any() ? paymentsByMonth.Average() : 0;
        }
    }

    public enum DebtReviewDecision
    {
        [Display(Name = "Accept Debt")]
        Accept,

        [Display(Name = "Decline")]
        Decline,

        [Display(Name = "Request Changes")]
        RequestChanges
    }
}
