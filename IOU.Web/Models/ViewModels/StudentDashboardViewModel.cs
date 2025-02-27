using IOU.Web.Models;

namespace IOU.Web.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public decimal TotalOwed { get; set; }
        public List<Debt> ActiveDebts { get; set; }

        public List<DebtWithNextPayment> DebtWithNextPayments { get; set; } = new List<DebtWithNextPayment>();
    }
}
