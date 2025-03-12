using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IOU.Web.Services.Interfaces;
using IOU.Web.Services;
using System.Security.Claims;

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
        private readonly IWebHostEnvironment _env;

        public LenderController(
            IOUWebContext context,
            UserManager<ApplicationUser> userManager,
            IDebtCalculationService calculationService,
            INotificationService notificationService,
            IDebtService debtService,
            ILogger<LenderController> logger,
            ISchedulePaymentService paymentService,
            IWebHostEnvironment env)
        {
            _paymentService = paymentService;
            _context = context;
            _userManager = userManager;
            _calculationService = calculationService;
            _notificationService = notificationService;
            _logger = logger;
            _debtService = debtService;
            _env = env;
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
                .Where(d => d.LenderUserId == lender.UserId)
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
                    relatedEntityType: RelatedEntityType.Debt,
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
                .FirstOrDefaultAsync(d => d.Id == id && d.LenderUserId == currentUser.Id);

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
                LenderUserId = lender.UserId,
                StudentUserId = student.UserId,
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
        [HttpGet]
        public async Task<IActionResult> Disputes()
        {
            var user = await _userManager.GetUserAsync(User);
            var disputes = await _context.Dispute
                .Include(d => d.Debt)
                .Include(d => d.User)
                .Include(d => d.DisputeDetail)
                .Where(d => d.Debt.Lender.UserId == user.Id)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();

            return View(disputes);
        }

        [HttpGet]
        public async Task<IActionResult> DisputeDetails(string id)
        {
            var dispute = await _context.Dispute
                .Include(d => d.Debt)
                    .ThenInclude(d => d.Lender)
                        .ThenInclude(l => l.User) // Ensure Lender.User is loaded
                .Include(d => d.User)
                .Include(d => d.DisputeDetail)
                .Include(d => d.SupportingDocuments)
                .Include(d => d.LenderEvidence)
                    .ThenInclude(e => e.Lender) // Include Lender for DebtEvidence
                .FirstOrDefaultAsync(d => d.DisputeId == id);

            if (dispute == null)
            {
                return NotFound();
            }

            // Handle potential null references
            if (dispute.Debt == null || dispute.Debt.Lender == null || dispute.User == null || dispute.DisputeDetail == null)
            {
                _logger.LogError("Critical navigation properties are null for dispute {DisputeId}", id);
                return NotFound();
            }

            // Safely map SupportingDocuments (handle null collection)
            var supportingDocuments = dispute.SupportingDocuments?
                .Select(d => new SupportingDocumentViewModel
                {
                    DocumentId = d.DocumentId,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    UploadDate = d.UploadDate,
                    Description = d.Description,
                    DocumentType = d.DocumentType,
                    DownloadUrl = Url.Content(d.FilePath)
                })?.ToList() ?? new List<SupportingDocumentViewModel>();

            // Safely map LenderEvidence (handle null collection)
            var lenderEvidence = dispute.LenderEvidence?
                .Select(e => new DebtEvidenceViewModel
                {
                    EvidenceId = e.EvidenceId,
                    LenderName = e.Lender?.User?.FullName ?? "Unknown Lender",
                    FileName = e.FileName,
                    ContentType = e.ContentType,
                    UploadDate = e.UploadDate,
                    Description = e.Description,
                    DownloadUrl = Url.Content(e.FilePath)
                })?.ToList() ?? new List<DebtEvidenceViewModel>();

            var viewModel = new DisputeDetailsViewModel
            {
                DisputeBasicInfo = new DisputeViewModel
                {
                    DisputeId = dispute.DisputeId,
                    DebtId = dispute.DebtId,
                    DebtorName = dispute.User.FullName,
                    LenderName = dispute.Debt.Lender.User.FullName,
                    DebtAmount = dispute.Debt.CurrentBalance,
                    Status = dispute.Status,
                    CreatedDate = dispute.CreatedDate,
                    ResolvedDate = dispute.ResolvedDate,
                    Reason = dispute.DisputeDetail.Reason,
                    RequestedResolution = dispute.DisputeDetail.RequestedResolution
                },
                DisputeExplanation = dispute.DisputeDetail.DisputeExplanation,
                RequestedReductionAmount = dispute.DisputeDetail.RequestedReductionAmount,
                DigitalSignature = dispute.DisputeDetail.DigitalSignature,
                SignatureDate = dispute.DisputeDetail.SignatureDate,
                StudentDocuments = supportingDocuments,
                LenderEvidence = lenderEvidence,
                AdminNotes = dispute.AdminNotes,
                CanSubmitEvidence = true,
                CanResolveDispute = true
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitEvidence(string disputeId, List<IFormFile> files, List<string> descriptions)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var dispute = await _context.Dispute
                .Include(d => d.Debt)
                .FirstOrDefaultAsync(d => d.DisputeId == disputeId);

            if (dispute?.Debt == null) return NotFound();

            var evidenceDir = Path.Combine(_env.WebRootPath, "evidence", disputeId);
            Directory.CreateDirectory(evidenceDir);

            foreach (var (file, index) in files.Select((f, i) => (f, i)))
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(evidenceDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Ensure the FilePath starts with a '/'
                var relativePath = $"/evidence/{disputeId}/{fileName}";

                _context.DebtEvidence.Add(new DebtEvidence
                {
                    EvidenceId = Guid.NewGuid().ToString(),
                    DisputeId = disputeId,
                    LenderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    FileName = file.FileName,
                    FilePath = relativePath, // Ensure this starts with '/'
                    ContentType = file.ContentType,
                    UploadDate = DateTime.Now,
                    Description = descriptions.ElementAtOrDefault(index)
                });
            }

            await _context.SaveChangesAsync();
            var studentUserId = dispute.Debt.StudentUserId;
            await _notificationService.CreateNotification(
                userId: studentUserId,
                title: "New Evidence Submitted",
                message: $"The lender has submitted new evidence for Dispute #{disputeId}. Please review the details.",
                type: NotificationType.EvidenceSubmitted,
                relatedEntityId: disputeId,
                relatedEntityType: RelatedEntityType.Dispute,
                actionUrl: $"/Student/DisputeDetails/{disputeId}"
            );
            return View("EvidenceSubmissionSuccess", disputeId);
        }
    }
}