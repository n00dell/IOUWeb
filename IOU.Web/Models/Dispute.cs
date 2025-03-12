using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{

    //Student disputes a debt
    [Index(nameof(UserId))]
    [Index(nameof(DebtId))]
    public class Dispute
    {
        [Key]
        public string DisputeId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }// Student User ID

        [Required]
        public string DebtId { get; set; }// Maps to Debt.Id
        [Required]
        public DisputeStatus Status { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public DateTime? ResolvedDate { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } // Student

        [ForeignKey("DebtId")]
        public virtual Debt Debt { get; set; } // Will map to the Id column in Debt table

        public virtual DisputeDetail DisputeDetail { get; set; }

        public virtual ICollection<SupportingDocument> SupportingDocuments { get; set; }

        public virtual ICollection<DebtEvidence> LenderEvidence { get; set; }
    }

    public enum DisputeStatus
    {
        Submitted,
        EvidenceRequested,
        UnderReview,
        Approved,
        Rejected
    }
    public enum DisputeReason
    {
        [Display(Name = "Not my debt")]
        NotMyDebt,

        [Display(Name = "Amount is incorrect")]
        IncorrectAmount,

        [Display(Name = "Already paid")]
        AlreadyPaid,

        [Display(Name = "Debt is too old (statute of limitations)")]
        TooOld,

        [Display(Name = "Payment plan already arranged")]
        PaymentPlanArranged,

        [Display(Name = "Forbearance/deferment should apply")]
        ForbearanceShouldApply,

        [Display(Name = "Debt discharged in bankruptcy")]
        DischargedInBankruptcy,

        [Display(Name = "School closed before completion")]
        SchoolClosed,

        [Display(Name = "Other")]
        Other
    }
    // Enum for requested resolution
    public enum ResolutionType
    {
        [Display(Name = "Complete debt cancellation")]
        CompleteDebtCancellation,

        [Display(Name = "Partial reduction")]
        PartialReduction,

        [Display(Name = "Payment plan adjustment")]
        PaymentPlanAdjustment,

        [Display(Name = "Interest rate reduction")]
        InterestRateReduction,

        [Display(Name = "Other")]
        Other
    }

}
