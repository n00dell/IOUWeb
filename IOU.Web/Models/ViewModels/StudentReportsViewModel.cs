namespace IOU.Web.Models.ViewModels
{
    public class StudentReportsViewModel
    {
        public string ReportType { get; set; }
        public Student Student { get; set; }
        public List<object> Data { get; set; }
    }
}
