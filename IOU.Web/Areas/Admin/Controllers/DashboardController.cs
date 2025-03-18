using IOU.Web.Areas.Admin.Models.ViewModels;
using IOU.Web.Data;
using IOU.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Admin.Admin.Controllers
{     
    [Area("Admin")]

    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IOUWebContext _context;
        
        public DashboardController(IOUWebContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _context.Users.CountAsync();

            // Active Loans (Debts with status "Active")
            var activeLoans = await _context.Debt
                .CountAsync(d => d.Status == DebtStatus.Active);

            // Total Outstanding Amount (Sum of CurrentBalance for all active loans)
            var totalAmount = await _context.Debt
                .Where(d => d.Status == DebtStatus.Active)
                .SumAsync(d => d.CurrentBalance);

            // Overdue Loans (Debts with status "Overdue")
            var overdueLoans = await _context.Debt
                .CountAsync(d => d.Status == DebtStatus.Overdue);

            // Recent Loans (Last 10 active loans)
            var recentLoans = await _context.Debt
                .Include(d => d.Student) // Include related Student data
                .Include(d => d.Lender)   // Include related Lender data
                .Where(d => d.Status == DebtStatus.Active)
                .OrderByDescending(d => d.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Recent Notifications (Last 5 notifications)
            var recentNotifications = await _context.Notification
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                ActiveLoans = activeLoans,
                TotalAmount = totalAmount,
                OverdueLoans = overdueLoans,
                RecentLoans = recentLoans,
                RecentNotifications = recentNotifications
            };

            return View(viewModel);
        }
    }
}
