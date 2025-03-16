using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class EditDebtViewModel
    {
        public string Id { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrincipalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; }

        [StringLength(200)]
        public string Purpose { get; set; }

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; }

        [Required]
        public DebtStatus Status { get; set; }

        // Hidden fields for required foreign keys
        public string LenderUserId { get; set; }
        public string StudentUserId { get; set; }
    }
}
