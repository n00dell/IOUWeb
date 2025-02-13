using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class CreateDebtViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Student Email")]
        public string StudentEmail { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        [Display(Name = "Principal Amount")]
        public decimal PrincipalAmount { get; set; }

        [Required]
        [Range(0, 100)]
        [Display(Name = "Annual Interest Rate (%)")]
        public decimal InterestRate { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Late Fee Amount")]
        public decimal LateFeeAmount { get; set; }

        [Required]
        [Range(0, 30)]
        [Display(Name = "Grace Period (Days)")]
        public int GracePeriodDays { get; set; }

        [Required]
        [StringLength(200)]
        public string Purpose { get; set; }
    }
}
