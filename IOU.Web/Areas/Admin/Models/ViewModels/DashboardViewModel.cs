using IOU.Web.Models;
using System.Diagnostics;

namespace IOU.Web.Areas.Admin.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveLoans { get; set; }
        public decimal TotalAmount { get; set; }
        public int OverdueLoans { get; set; }
        public List<Debt> RecentLoans { get; set; }
        public List<Notification> RecentNotifications { get; set; }
    }
}
