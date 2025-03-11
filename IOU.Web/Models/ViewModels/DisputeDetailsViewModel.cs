namespace IOU.Web.Models.ViewModels
{
    public class DisputeDetailsViewModel
    {
        public DisputeViewModel DisputeBasicInfo { get; set; }

        public string DisputeExplanation { get; set; }
        public decimal? RequestedReductionAmount { get; set; }
        public string DigitalSignature { get; set; }
        public DateTime SignatureDate { get; set; }

        public List<SupportingDocumentViewModel> StudentDocuments { get; set; }
        public List<DebtEvidenceViewModel> LenderEvidence { get; set; }

        public string AdminNotes { get; set; }
        public bool CanSubmitEvidence { get; set; } // For lender
        public bool CanResolveDispute { get; set; } // For admin
    }
}
