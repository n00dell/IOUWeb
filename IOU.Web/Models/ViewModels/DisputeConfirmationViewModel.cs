namespace IOU.Web.Models.ViewModels
{
    public class DisputeConfirmationViewModel
    {
        public string DisputeId { get; set; }
        public string DebtId { get; set; }
        public string LenderName { get; set; }
        public decimal DebtAmount { get; set; }
        public DisputeStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DisputeReason Reason { get; set; }
        public ResolutionType RequestedResolution { get; set; }
    }
}
