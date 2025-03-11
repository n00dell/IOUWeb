using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class ResolveDisputeViewModel
    {
        public string DisputeId { get; set; }
        public string DebtId { get; set; }
        public string StudentName { get; set; }
        public string LenderName { get; set; }
        public decimal OriginalDebtAmount { get; set; }
        public DisputeReason Reason { get; set; }
        public ResolutionType RequestedResolution { get; set; }
        public decimal? RequestedReductionAmount { get; set; }

        [Required]
        [Display(Name = "Resolution Decision")]
        public DisputeStatus ResolutionDecision { get; set; }

        [Required]
        [Display(Name = "Admin Notes")]
        [StringLength(500, MinimumLength = 20, ErrorMessage = "Notes must be between 20 and 500 characters.")]
        public string AdminNotes { get; set; }

        [Display(Name = "Adjusted Debt Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than or equal to $0")]
        public decimal? AdjustedDebtAmount { get; set; }

        public List<SupportingDocumentViewModel> StudentDocuments { get; set; }
        public List<DebtEvidenceViewModel> LenderEvidence { get; set; }
    }
}
