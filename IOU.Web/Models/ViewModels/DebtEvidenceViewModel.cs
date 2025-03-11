namespace IOU.Web.Models.ViewModels
{
    public class DebtEvidenceViewModel
    {
        public string EvidenceId { get; set; }
        public string LenderName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadDate { get; set; }
        public string Description { get; set; }
        public string DownloadUrl { get; set; }
    }
}
