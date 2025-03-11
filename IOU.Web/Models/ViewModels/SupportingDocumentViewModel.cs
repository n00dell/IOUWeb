namespace IOU.Web.Models.ViewModels
{
    public class SupportingDocumentViewModel
    {
        public string DocumentId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadDate { get; set; }
        public string Description { get; set; }
        public string DocumentType { get; set; }
        public string DownloadUrl { get; set; }
    }
}
