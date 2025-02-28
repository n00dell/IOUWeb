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
        private readonly ISchedulePaymentService _paymentService;
        private readonly ILogger<StudentController> _logger;
        public StudentController(IDebtService debtService, IOUWebContext context, UserManager<ApplicationUser> userManager, ISchedulePaymentService paymentService, ILogger<StudentController> logger)
        {
            _debtService = debtService;
            _context = context;
            _userManager = userManager;
            _paymentService = paymentService;
            _logger = logger;
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
            var debtsWithPayments = new List<DebtWithNextPayment>();

            foreach (var debt in debts)
            {
                await _debtService.UpdateDebtCalculations(debt.Id);
                await _paymentService.UpdatePaymentStatusesAsync(debt.Id);

                var nextPayment = await _context.ScheduledPayment
                    .Where(p => p.DebtId == debt.Id && p.Status != PaymentStatus.Paid)
                    .OrderBy(p => p.DueDate)
                    .FirstOrDefaultAsync();

                debtsWithPayments.Add(new DebtWithNextPayment
                {
                    Debt = debt,
                    NextPayment = nextPayment
                });
            }

            var viewModel = new StudentDashboardViewModel
            {
                TotalOwed = debts.Sum(d => d.CurrentBalance),
                ActiveDebts = debts.OrderByDescending(d => d.DueDate).ToList(),
                DebtWithNextPayments = debtsWithPayments
            };

            return View(viewModel);
        }


        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DebtDetails(string id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return RedirectToAction("Error", "Home");

                var debt = await _context.Debt
                    .Include(d => d.Lender)
                        .ThenInclude(l => l.User)
                    .Include(d => d.Student)
                        .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(d => d.Id == id && d.StudentId == currentUser.Id);

                if (debt == null)
                    return NotFound();

                await _debtService.UpdateDebtCalculations(debt.Id);
                await _paymentService.UpdatePaymentStatusesAsync(debt.Id);

                var payments = await _context.ScheduledPayment
                    .Where(p => p.DebtId == debt.Id)
                    .OrderBy(p => p.DueDate)
                    .ToListAsync();

                var viewModel = new LenderDebtDetailsViewModel
                {
                    Debt = debt,
                    ScheduledPayments = payments
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error fetching debt details for student.");
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyDebts()
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

            var debtsWithPayments = new List<DebtWithNextPayment>();

            foreach (var debt in debts)
            {
                await _debtService.UpdateDebtCalculations(debt.Id);

                var nextPayment = await _context.ScheduledPayment
                    .Where(p => p.DebtId == debt.Id && p.Status != PaymentStatus.Paid)
                    .OrderBy(p => p.DueDate)
                    .FirstOrDefaultAsync();

                debtsWithPayments.Add(new DebtWithNextPayment
                {
                    Debt = debt,
                    NextPayment = nextPayment
                });
            }

            return View(debtsWithPayments);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> PaymentHistory()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Error", "Home");

            var payments = await _context.ScheduledPayment
                .Include(p => p.Debt)
                .Where(p => p.Debt.StudentId == currentUser.Id)
                .OrderByDescending(p => p.DueDate)
                .ToListAsync();

            return View(payments);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReviewDebt(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var debt = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(d => d.Id == id && d.StudentId == currentUser.Id);

            if (debt == null)
                return NotFound();

            var payments = await _context.ScheduledPayment
                .Where(p => p.DebtId == debt.Id)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

            var viewModel = new ReviewDebtViewModel
            {
                Debt = debt,
                ScheduledPayments = payments
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReviewDebtDecision(ReviewDebtViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ReviewDebt", model);

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var debt = await _context.Debt
                    .FirstOrDefaultAsync(d => d.Id == model.Debt.Id && d.StudentId == currentUser.Id);

                if (debt == null)
                    return NotFound();

                // Validate the decision
                if (!Enum.IsDefined(typeof(DebtReviewDecision), model.Decision))
                {
                    ModelState.AddModelError("Decision", "Invalid decision.");
                    return View("ReviewDebt", model);
                }

                // Update debt status based on decision
                switch (model.Decision)
                {
                    case DebtReviewDecision.Accept:
                        debt.Status = DebtStatus.Active;
                        break;
                    case DebtReviewDecision.Decline:
                        debt.Status = DebtStatus.Declined;
                        break;
                    case DebtReviewDecision.RequestChanges:
                        debt.Status = DebtStatus.PendingChanges;
                        break;
                }

                // Save any comments from the student
                //if (!string.IsNullOrEmpty(model.ReviewComments))
                //{
                //    debt.Comments = model.ReviewComments;
                //}

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error processing debt review decision.");
                return RedirectToAction("Error", "Home");
            }
        }
    }
}