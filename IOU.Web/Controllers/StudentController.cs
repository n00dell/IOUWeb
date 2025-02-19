using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Models.ViewModels;
using IOU.Web.Services;
using IOU.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly IDebtService _debtService;
        private readonly IOUWebContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(IDebtService debtService, IOUWebContext context, UserManager<ApplicationUser> userManager)
        {
            _debtService = debtService;
            _context = context;
            _userManager = userManager;
        }


        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            if (student == null)
                return RedirectToAction("Error", "Home");

            var debts = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .Where(d => d.StudentId == student.UserId)
                .ToListAsync();

            foreach (var debt in debts)
                await _debtService.UpdateDebtCalculations(debt.Id);

            var viewModel = new StudentDashboardViewModel
            {
                TotalOwed = debts.Sum(d => d.CurrentBalance),
                ActiveDebts = debts.OrderByDescending(d => d.DueDate).ToList()
            };

            return View(viewModel);
        }
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DebtDetails(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var debt = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(d => d.Id == id && d.StudentId == currentUser.Id);

            if (debt == null)
                return NotFound();

            await _debtService.UpdateDebtCalculations(debt.Id);

            return View(debt);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyDebts()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

            var debts = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .Where(d => d.StudentId == student.UserId)
                .ToListAsync();

            foreach (var debt in debts)
                await _debtService.UpdateDebtCalculations(debt.Id);

            return View(debts);
        }

        [Authorize(Roles = "Student")]
        public IActionResult PaymentHistory()
        {
            return View();
        }
    }
}
