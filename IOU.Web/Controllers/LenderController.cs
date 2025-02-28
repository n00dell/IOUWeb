using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IOU.Web.Services.Interfaces;
using IOU.Web.Services;

namespace IOU.Web.Controllers
{
    public class LenderController : Controller
    {
        private readonly IOUWebContext _context;
        private readonly IDebtCalculationService _calculationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LenderController> _logger;
        private readonly IDebtService _debtService;
        private readonly ISchedulePaymentService _paymentService;

        public LenderController(
            IOUWebContext context,
            UserManager<ApplicationUser> userManager,
            IDebtCalculationService calculationService,
            INotificationService notificationService,
            IDebtService debtService,
            ILogger<LenderController> logger,
            ISchedulePaymentService paymentService)
        {
            _paymentService = paymentService;
            _context = context;
            _userManager = userManager;
            _calculationService = calculationService;
            _notificationService = notificationService;
            _logger = logger;
            _debtService = debtService;
        }

        [HttpGet]
        [Authorize(Roles = "Lender")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var lender = await _context.Lender
                .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

            if (lender == null)
                return RedirectToAction("Error", "Home");

            // Get debts with related student and user data
            var debts = await _context.Debt
                .Include(d => d.Student)
                    .ThenInclude(s => s.User)
                .Where(d => d.LenderId == lender.UserId)
                .ToListAsync();

            // Update calculations for each debt
            foreach (var debt in debts)
                await _debtService.UpdateDebtCalculations(debt.Id);

            var viewModel = new LenderDashboardViewModel
            {
                TotalActiveDebts = debts.Sum(d => d.CurrentBalance),
                ActiveDebts = debts.OrderByDescending(d => d.DueDate).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Lender")]
        public IActionResult CreateDebt()
        {
            var viewModel = new CreateDebtViewModel
            {
                DueDate = DateTime.Today.AddMonths(1),
                GracePeriodDays = 7,
                FirstPaymentDate = DateTime.Today.AddDays(7),
                PaymentFrequency = PaymentFrequency.Monthly,
                NumberOfPayments = 12
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lender")]
        public async Task<IActionResult> CreateDebt(CreateDebtViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!ValidateDebtCreation(model, out string errorMessage))
            {
                ModelState.AddModelError("", errorMessage);
                return View(model);
            }

            try
            {
                var (currentUser, lender) = await GetCurrentLender();
                if (lender == null)
                {
                    ModelState.AddModelError("", "Lender profile not found.");
                    return View(model);
                }

                var student = await GetStudent(model.StudentEmail);
                if (student == null)
                {
                    ModelState.AddModelError("StudentEmail", "Student not found.");
                    return View(model);
                }

                var debt = CreateDebtEntity(model, lender, student);

                // First save the debt to get a valid ID
                _context.Debt.Add(debt);
                await _context.SaveChangesAsync();

                // Then generate payment schedule
                var paymentRequest = new CreateScheduledPaymentsRequest
                {
                    DebtId = debt.Id,
                    NumberOfPayments = model.NumberOfPayments,
                    FirstPaymentDate = model.FirstPaymentDate,
                    Frequency = model.PaymentFrequency,
                    IncludeInterestInCalculation = true
                };

                await _paymentService.GeneratePaymentScheduleAsync(paymentRequest);

                // Updated notification creation with lender's name
                await _notificationService.CreateNotification(
                    userId: student.UserId,
                    title: "New Debt Created",
                    message: $"{currentUser.FullName} has created a new {model.DebtType} debt of {model.PrincipalAmount:C}. " +
                            $"Due date: {model.DueDate:d}. Please review and approve.",
                    type: NotificationType.DebtCreated,
                    relatedEntityId: debt.Id,
                    relatedEntityType: "Debt",
                    actionUrl: $"/Student/ReviewDebt/{debt.Id}"
                );

                TempData["SuccessMessage"] = "Debt created successfully.";
                return RedirectToAction(nameof(Dashboard)); // Fixed: using correct action name
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating debt for student {StudentEmail}", model.StudentEmail);
                ModelState.AddModelError("", "Error creating debt: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Lender")]
        public async Task<IActionResult> DebtDetails(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var debt = await _context.Debt
                .Include(d => d.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(d => d.Id == id && d.LenderId == currentUser.Id);

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

        private bool ValidateDebtCreation(CreateDebtViewModel model, out string errorMessage)
        {
            errorMessage = null;

            if (model.DueDate <= DateTime.Today)
            {
                errorMessage = "Due date must be in the future";
                return false;
            }

            if (model.InterestRate < 0 || model.InterestRate > 100)
            {
                errorMessage = "Interest rate must be between 0 and 100";
                return false;
            }

            if (model.GracePeriodDays < 0 || model.GracePeriodDays > 30)
            {
                errorMessage = "Grace period must be between 0 and 30 days";
                return false;
            }

            if (model.FirstPaymentDate <= DateTime.Today)
            {
                errorMessage = "First payment date must be in the future";
                return false;
            }

            if (model.NumberOfPayments <= 0 || model.NumberOfPayments > 120)
            {
                errorMessage = "Number of payments must be between 1 and 120";
                return false;
            }

            return true;
        }

        private async Task<(ApplicationUser User, Lender Lender)> GetCurrentLender()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return (null, null);

            var lender = await _context.Lender
                .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

            return (currentUser, lender);
        }

        private async Task<Student> GetStudent(string email)
        {
            var studentUser = await _userManager.FindByEmailAsync(email);
            if (studentUser == null)
                return null;

            return await _context.Student
                .FirstOrDefaultAsync(s => s.UserId == studentUser.Id);
        }

        private Debt CreateDebtEntity(CreateDebtViewModel model, Lender lender, Student student)
        {
            return new Debt
            {
                Id = Guid.NewGuid().ToString(),
                LenderId = lender.UserId,
                StudentId = student.UserId,
                DebtType = model.DebtType,
                PrincipalAmount = model.PrincipalAmount,
                CurrentBalance = model.PrincipalAmount,
                InterestRate = model.InterestRate,
                InterestType = model.InterestType,
                CalculationPeriod = model.CalculationPeriod,
                DateIssued = DateTime.UtcNow,
                DueDate = model.DueDate,
                CreatedAt = DateTime.UtcNow,
                LastInterestCalculationDate = DateTime.UtcNow,
                LateFeeAmount = model.LateFeeAmount,
                GracePeriodDays = model.GracePeriodDays,
                Purpose = model.Purpose,
                Status = DebtStatus.Pending,
                AccumulatedInterest = 0,
                AccumulatedLateFees = 0
            };
        }
    }
}