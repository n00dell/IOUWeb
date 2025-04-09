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
using Microsoft.AspNetCore.Mvc.Rendering;

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
        private readonly IScheduledPaymentService _paymentService;
        private readonly IWebHostEnvironment _env;
        private readonly ICreditReportService _creditReportService;

        public LenderController(
            IOUWebContext context,
            UserManager<ApplicationUser> userManager,
            IDebtCalculationService calculationService,
            INotificationService notificationService,
            IDebtService debtService,
            ILogger<LenderController> logger,
            IScheduledPaymentService paymentService,
            IWebHostEnvironment env,
            ICreditReportService creditReportService)
        {
            _paymentService = paymentService;
            _context = context;
            _userManager = userManager;
            _calculationService = calculationService;
            _notificationService = notificationService;
            _logger = logger;
            _debtService = debtService;
            _env = env;
            _creditReportService = creditReportService;
        }

        [HttpGet]
        [Authorize(Roles = "Lender")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var notifications = await _notificationService.GetUserNotifications(currentUser.Id, includeRead: true);
            var latestNotifications = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(3)
                .ToList();
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
                ActiveDebts = debts.OrderByDescending(d => d.DueDate).ToList(),
                Notifications = latestNotifications
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
                PaymentFrequency = PaymentFrequency.Monthly
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDebt(CreateDebtViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!ValidateDebtCreation(model, out string error))
            {
                ModelState.AddModelError("", error);
                return View(model);
            }

            try
            {
                var (user, lender) = await GetCurrentLender();
                if (lender == null) return Forbid();

                var student = await GetStudent(model.StudentEmail);
                if (student == null)
                {
                    ModelState.AddModelError("StudentEmail", "Student not found");
                    return View(model);
                }

                var debt = new Debt
                {
                    Id = Guid.NewGuid().ToString(),
                    LenderUserId = lender.UserId,
                    StudentUserId = student.UserId,
                    PrincipalAmount = model.PrincipalAmount,
                    CurrentBalance = model.PrincipalAmount,
                    InterestRate = model.InterestRate,
                    InterestType = model.InterestType,
                    CalculationPeriod = model.CalculationPeriod,
                    DebtType = model.DebtType,
                    DueDate = model.DueDate,
                    GracePeriodDays = model.GracePeriodDays,
                    LateFeeAmount = model.LateFeeAmount,
                    Purpose = model.Purpose,
                    Status = DebtStatus.Pending,
                    DateIssued = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    LastInterestCalculationDate = DateTime.UtcNow
                };

                _context.Debt.Add(debt);
                await _context.SaveChangesAsync();


                await _paymentService.GeneratePaymentScheduleAsync(new CreateScheduledPaymentsRequest
                {
                    DebtId = debt.Id,
                    FirstPaymentDate = model.FirstPaymentDate,
                    Frequency = model.PaymentFrequency,
                    NumberOfPayments = model.NumberOfPayments,
                    IncludeInterestInCalculation = true
                });

                await NotifyDebtCreation(debt, user, student);
                TempData["SuccessMessage"] = "Debt created successfully";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating debt");
                ModelState.AddModelError("", "Error creating debt: " + ex.Message);
                return View(model);
            }
        }
        private async Task NotifyDebtCreation(Debt debt, ApplicationUser lenderUser, Student student)
        {
            // Notify the student
            await _notificationService.CreateNotification(
                userId: student.UserId,
                title: "New Debt Created",
                message: $"{lenderUser.FullName} has created a new {debt.DebtType} debt of {debt.PrincipalAmount:C}. " +
                        $"Due date: {debt.DueDate:d}. Please review and approve.",
                type: NotificationType.DebtCreated,
                relatedEntityId: debt.Id,
                relatedEntityType: RelatedEntityType.Debt,
                actionUrl: $"/Student/ReviewDebt/{debt.Id}"
            );

            // Notify the admin
            await _notificationService.NotifyAdmin(
                title: "New Debt Created",
                message: $"Lender {lenderUser.FullName} has created a new debt of {debt.PrincipalAmount:C} for student {student.User.FullName}.",
                type: NotificationType.DebtCreated,
                relatedEntityId: debt.Id,
                relatedEntityType: RelatedEntityType.Debt,
                actionUrl: $"/Admin/Debt/Details/{debt.Id}"
            );
        }
        private bool ValidateDebtCreation(CreateDebtViewModel model, out string error)
        {
            error = null;

            if (model.DueDate <= DateTime.Today)
                error = "Due date must be in the future";
            else if (model.FirstPaymentDate >= model.DueDate)
                error = "First payment must be before due date";
            else if (model.NumberOfPayments.HasValue && model.NumberOfPayments < 1)
                error = "Number of payments must be at least 1";

            return error == null;
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

            // Notify the student
            await _notificationService.CreateNotification(
                userId: studentUserId,
                title: "New Evidence Submitted",
                message: $"The lender has submitted new evidence for Dispute #{disputeId}. Please review the details.",
                type: NotificationType.EvidenceSubmitted,
                relatedEntityId: disputeId,
                relatedEntityType: RelatedEntityType.Dispute,
                actionUrl: $"/Student/DisputeDetails/{disputeId}"
            );

            // Notify the admin
            await _notificationService.NotifyAdmin(
                title: "New Evidence Submitted",
                message: $"Lender {user.FullName} has submitted new evidence for Dispute #{disputeId}.",
                type: NotificationType.EvidenceSubmitted,
                relatedEntityId: disputeId,
                relatedEntityType: RelatedEntityType.Dispute,
                actionUrl: $"/Admin/Dispute/Details/{disputeId}"
            );

            return View("EvidenceSubmissionSuccess", disputeId);
        }

        [Authorize(Roles = "Lender")]
        [HttpGet("RequestCreditCheck")]
        public IActionResult RequestCreditCheck()
        {
            var viewModel = new CreditCheckRequestViewModel
            {
                PurposeOptions = GetPurposeOptions()
            };
            return View(viewModel);
        }

        [Authorize(Roles = "Lender")]
        [HttpPost("RequestCreditCheck")]
        public async Task<IActionResult> RequestCreditCheck(CreditCheckRequestViewModel model)
        {
            model.PurposeOptions = GetPurposeOptions();
            if (!ModelState.IsValid)
            {
                model.PurposeOptions = GetPurposeOptions();
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if(currentUser == null) return Challenge();
            var lender = await _context.Lender.FirstOrDefaultAsync(l => l.UserId == currentUser.Id);
            if (lender == null) return Forbid();

            // Check if student exists
            var studentUser = await _userManager.FindByEmailAsync(model.StudentEmail);
            if (studentUser == null)
            {
                ModelState.AddModelError("StudentEmail", "Student not found in system");
                model.PurposeOptions = GetPurposeOptions();
                return View(model);
            }

            // Check for existing pending requests
            var existingRequest = await _context.CreditReportRequests
                .AnyAsync(r => r.LenderUserId == lender.UserId &&
                              r.StudentEmail == model.StudentEmail &&
                              !r.ResponseDate.HasValue);
            if (existingRequest)
            {
                ModelState.AddModelError("", "You already have a pending request for this student");
                model.PurposeOptions = GetPurposeOptions();
                return View(model);
            }

            // Create request
            var request = new CreditReportRequest
            {
                LenderUserId = lender.UserId,
                StudentEmail = model.StudentEmail,
                Purpose = model.SelectedPurpose,
                RequestDate = DateTime.UtcNow,
                StudentUserId = studentUser.Id,
                CreditReportId = null
            };

            _context.CreditReportRequests.Add(request);
            await _context.SaveChangesAsync();

            // Notify student
            await _notificationService.CreateNotification(
                userId: studentUser.Id,
                title: "Credit Check Request",
                message: $"{currentUser.FullName} has requested a credit report for: {model.SelectedPurpose}",
                type: NotificationType.CreditCheckRequest,
                relatedEntityId: request.Id,
                relatedEntityType: RelatedEntityType.CreditCheck,
                actionUrl: $"/Student/CreditCheckRequests"
            );

            TempData["SuccessMessage"] = "Credit check request submitted. Awaiting student approval.";
            return RedirectToAction(nameof(CreditCheckRequests));
        }

        [Authorize(Roles = "Lender")]
        [HttpGet("CreditCheckRequests")]
        public async Task<IActionResult> CreditCheckRequests()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var requests = await _context.CreditReportRequests
                .Include(r => r.Lender)
                .Where(r => r.LenderUserId == currentUser.Id)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = "Lender")]
        [HttpGet("ViewCreditReport/{id}")]
        public async Task<IActionResult> ViewCreditReport(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var request = await _context.CreditReportRequests
                .Include(r => r.CreditReport)
                .Include(r => r.Student)
                    .ThenInclude(s => s.User)
                .Include(r => r.Student)
                    .ThenInclude(s => s.Debts)
                    .ThenInclude(d => d.ScheduledPayments)
                .FirstOrDefaultAsync(r => r.Id == id &&
                                         r.LenderUserId == currentUser.Id &&
                                         r.IsApproved == true);

            if (request?.CreditReport == null)
                return NotFound();

            if (request.ResponseDate.HasValue &&
                request.ResponseDate.Value.AddDays(30) < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "This report has expired";
                return RedirectToAction(nameof(CreditCheckRequests));
            }

            // Calculate payment history
            var allPayments = request.Student.Debts
                .SelectMany(d => d.ScheduledPayments)
                .Where(p => p.Status == ScheduledPaymentStatus.Paid)
                .ToList();

            var paymentHistory = new CreditReportViewModel.PaymentHistorySummary
            {
                TotalPayments = allPayments.Count,
                OnTimePayments = allPayments.Count(p => p.DaysLate <= 0),
                LatePayments = allPayments.Count(p => p.DaysLate > 0),
                AverageDaysLate = allPayments.Any() ?
                    (decimal)allPayments.Average(p => p.DaysLate) : 0
            };

            // Create view model
            var viewModel = new CreditReportViewModel
            {
                StudentName = request.Student.User.FullName,
                CreditScore = request.CreditReport.CreditScore,
                RiskCategory = request.CreditReport.RiskCategory,
                ActiveDebts = request.CreditReport.ActiveDebtsCount,
                TotalObligations = request.CreditReport.TotalDebtObligations,
                PaymentCompletionRate = request.CreditReport.PaymentCompletionRate,
                AverageDelayDays = request.CreditReport.AveragePaymentDelayDays,
                GeneratedDate = request.CreditReport.GeneratedDate,
                RecommendedLimit = CalculateRecommendedLimit(request.CreditReport),
                RiskExplanation = GetRiskExplanation(request.CreditReport.RiskCategory),
                PaymentHistory = paymentHistory,
                Debts = request.Student.Debts.Select(d => new CreditReportViewModel.DebtItem
                {
                    DebtType = d.DebtType.ToString(),
                    PrincipalAmount = d.PrincipalAmount,
                    CurrentBalance = d.CurrentBalance,
                    InterestRate = d.InterestRate,
                    DueDate = d.DueDate,
                    Status = d.Status.ToString()
                }).ToList()
            };

            return View(viewModel);
        }
        [Authorize(Roles = "Lender")]
        [HttpGet("ActiveLoans")]
        public async Task<IActionResult> ActiveLoans()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var lender = await _context.Lender.FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

            if (lender == null) return RedirectToAction("Error", "Home");

            var activeDebts = await _context.Debt
                .Include(d => d.Student)
                    .ThenInclude(s => s.User)
                .Include(d => d.ScheduledPayments)
                .Where(d => d.LenderUserId == lender.UserId &&
                           (d.Status == DebtStatus.Active || d.Status == DebtStatus.Overdue))
                .OrderByDescending(d => d.DueDate)
                .ToListAsync();

            // Update calculations for each debt
            foreach (var debt in activeDebts)
            {
                await _debtService.UpdateDebtCalculations(debt.Id);
            }

            var viewModel = new ActiveLoansViewModel
            {
                ActiveDebts = activeDebts,
                TotalOutstanding = activeDebts.Sum(d => d.CurrentBalance),
                TotalExpectedInterest = activeDebts.Sum(d => d.AccumulatedInterest),
                LoansByType = activeDebts
                    .GroupBy(d => d.DebtType.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.CurrentBalance))
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Lender")]
        [HttpGet("Borrowers")]
        public async Task<IActionResult> Borrowers()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var lender = await _context.Lender.FirstOrDefaultAsync(l => l.UserId == currentUser.Id);

            if (lender == null) return RedirectToAction("Error", "Home");

            var borrowers = await _context.Debt
                .Include(d => d.Student)
                    .ThenInclude(s => s.User)
                .Where(d => d.LenderUserId == lender.UserId)
                .Select(d => d.Student)
                .Distinct()
                .ToListAsync();

            // Calculate active loans per borrower
            var activeLoansByBorrower = await _context.Debt
                .Where(d => d.LenderUserId == lender.UserId &&
                           (d.Status == DebtStatus.Active || d.Status == DebtStatus.Overdue))
                .GroupBy(d => d.Student.User.FullName)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var viewModel = new BorrowersViewModel
            {
                Borrowers = borrowers,
                TotalOwedByBorrower = await _context.Debt
                    .Where(d => d.LenderUserId == lender.UserId)
                    .GroupBy(d => d.Student.User.FullName)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Sum(d => d.CurrentBalance)),
                ActiveLoansByBorrower = activeLoansByBorrower
            };

            return View(viewModel);
        }
        private decimal CalculateRecommendedLimit(CreditReport report)
        {
            // Simple algorithm - adjust based on your business rules
            decimal baseLimit = 10000; // Base limit
            decimal scoreFactor = report.CreditScore / 1000; // Normalize to 0-1 scale
            decimal debtFactor = 1 - (report.TotalDebtObligations / (report.TotalDebtObligations + 5000));

            return baseLimit * scoreFactor * debtFactor;
        }

        private string GetRiskExplanation(string riskCategory)
        {
            return riskCategory switch
            {
                "Low Risk" => "This student has excellent credit history with consistent on-time payments and manageable debt levels.",
                "Medium Risk" => "This student has generally good credit history but may have some late payments or higher debt levels.",
                "High Risk" => "This student has significant credit issues including late payments, defaults, or excessive debt levels.",
                _ => "Risk assessment not available."
            };
        }

        
        private static List<SelectListItem> GetPurposeOptions() => new()
        {
            new("Pre-loan Assessment", "pre_loan"),
            new("Credit Limit Increase", "limit_increase"),
            new("Periodic Review", "periodic_review")
        };
        
    }
}