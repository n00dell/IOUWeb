using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class CreateDisputeViewModel
    {
        public string DebtId { get; set; }

        [Display(Name = "Debt Amount")]
        public decimal DebtAmount { get; set; }

        [Display(Name = "Lender")]
        public string LenderName { get; set; }

        [Required]
        [Display(Name = "Reason for Dispute")]
        public DisputeReason Reason { get; set; }

        [Display(Name = "Other Reason Details")]
        public string OtherReasonDetail { get; set; }

        [Required]
        [Display(Name = "Detailed Explanation")]
        [StringLength(2000, MinimumLength = 50, ErrorMessage = "Explanation must be between 50 and 2000 characters.")]
        public string DisputeExplanation { get; set; }

        [Required]
        [Display(Name = "Requested Resolution")]
        public ResolutionType RequestedResolution { get; set; }

        [Display(Name = "Other Resolution Details")]
        public string OtherResolutionDetail { get; set; }

        [Display(Name = "Requested Reduction Amount")]
        [Range(0, 100000, ErrorMessage = "Amount must be between $0 and $100,000")]
        public decimal? RequestedReductionAmount { get; set; }

        [Required]
        [Display(Name = "I confirm that all information provided is true and accurate to the best of my knowledge")]
        public bool DeclarationConfirmed { get; set; }

        [Required]
        [Display(Name = "Digital Signature (Full Name)")]
        public string DigitalSignature { get; set; }

        [Display(Name = "Supporting Documents")]
        public List<IFormFile> SupportingDocuments { get; set; }

        [Display(Name = "Document Descriptions")]
        public List<string> DocumentDescriptions { get; set; }
    }
}
