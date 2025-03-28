using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class CreateDebtViewModel
    {
        [Required]
        [EmailAddress]
        public string StudentEmail { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        public decimal PrincipalAmount { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal InterestRate { get; set; }

        [Required]
        public DebtType DebtType { get; set; }

        [Required]
        public InterestType InterestType { get; set; }

        [Required]
        public InterestCalculationPeriod CalculationPeriod { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal LateFeeAmount { get; set; }

        [Range(0, 30)]
        public int GracePeriodDays { get; set; }

        public string? ActionUrl { get; set; }

        [Required]
        public string Purpose { get; set; }


        [Display(Name = "Number of Payments")]
        [Range(1, 120, ErrorMessage = "Number of payments must be between 1 and 120")]
        public int? NumberOfPayments { get; set; }

        [Required]
        [Display(Name = "First Payment Date")]
        [DataType(DataType.Date)]
        public DateTime FirstPaymentDate { get; set; }

        [Required]
        [Display(Name = "Payment Frequency")]
        public PaymentFrequency PaymentFrequency { get; set; }
    }
}
