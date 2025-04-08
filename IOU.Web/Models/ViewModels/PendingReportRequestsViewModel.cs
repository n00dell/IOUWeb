namespace IOU.Web.Models.ViewModels
{
    public class PendingReportRequestsViewModel
    {
        public List<CreditReportViewModel> PendingRequests { get; set; }
        public List<CreditReportViewModel> ApprovedRequests { get; set; }
        public List<CreditReportViewModel> DeniedRequests { get; set; }
    }
}
