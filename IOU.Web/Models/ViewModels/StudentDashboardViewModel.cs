namespace IOU.Web.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public decimal TotalOwed { get; set; }
        public List<Debt> ActiveDebts { get; set; }
    }
}
