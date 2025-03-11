namespace IOU.Web.Models.ViewModels
{
    public class DisputeListViewModel
    {
        public List<DisputeViewModel> Disputes { get; set; }
        public int TotalDisputes { get; set; }
        public int PendingDisputes { get; set; }
        public int ResolvedDisputes { get; set; }
    }
}
