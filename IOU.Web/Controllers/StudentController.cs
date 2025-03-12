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
        private readonly INotificationService _notificationService;
        public StudentController(IDebtService debtService, IOUWebContext context, UserManager<ApplicationUser> userManager, ISchedulePaymentService paymentService, ILogger<StudentController> logger, INotificationService notificationService)
        {
            _debtService = debtService;
            _context = context;
            _userManager = userManager;
            _paymentService = paymentService;
            _logger = logger;
            _notificationService = notificationService;
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
                .Where(d => d.StudentUserId == student.UserId)
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
                    .FirstOrDefaultAsync(d => d.Id == id && d.StudentUserId == currentUser.Id);

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
                .Where(d => d.StudentUserId == student.UserId)
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
                .Where(p => p.Debt.StudentUserId == currentUser.Id)
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
                .FirstOrDefaultAsync(d => d.Id == id && d.StudentUserId == currentUser.Id);

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
                    .FirstOrDefaultAsync(d => d.Id == model.Debt.Id && d.StudentUserId == currentUser.Id);

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
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CreateDispute(string debtId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found.");
                return RedirectToAction("Error", "Home");
            }

            _logger.LogInformation("Debt ID: {DebtId}, Current User ID: {UserId}", debtId, currentUser.Id);

            var debt = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .Include(d => d.Student)
                .FirstOrDefaultAsync(d => d.Id == debtId && d.StudentUserId == currentUser.Id);

            if (debt == null)
            {
                _logger.LogWarning("Debt not found for Debt ID: {DebtId}", debtId);
                return NotFound("Debt not found.");
            }

            if (debt.Lender == null || debt.Lender.User == null)
            {
                _logger.LogWarning("Lender information missing for Debt ID: {DebtId}", debtId);
                return NotFound("Lender information is missing.");
            }

            var viewModel = new CreateDisputeViewModel
            {
                DebtId = debt.Id,
                StudentUserId = currentUser.Id,
                DebtAmount = debt.CurrentBalance,
                LenderName = debt.Lender.User.FullName
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitDispute(CreateDisputeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateDispute", model);
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var debt = await _context.Debt
                    .Include(d => d.Lender)
                    .FirstOrDefaultAsync(d => d.Id == model.DebtId && d.StudentUserId == currentUser.Id);

                if (debt == null)
                {
                    return NotFound("Debt not found.");
                }

                // Create the dispute
                var dispute = new Dispute
                {
                    DisputeId = Guid.NewGuid().ToString(),
                    UserId = currentUser.Id,
                    DebtId = model.DebtId,
                    Status = DisputeStatus.Submitted,
                    CreatedDate = DateTime.Now,
                    AdminNotes = string.Empty, // Fix for AdminNotes
                    DisputeDetail = new DisputeDetail
                    {
                        DisputeId = Guid.NewGuid().ToString(), // Ensure this matches the parent DisputeId
                        Reason = model.Reason,
                        OtherReasonDetail = model.OtherReasonDetail,
                        DisputeExplanation = model.DisputeExplanation,
                        RequestedResolution = model.RequestedResolution,
                        OtherResolutionDetail = model.OtherResolutionDetail,
                        RequestedReductionAmount = model.RequestedReductionAmount,
                        DeclarationConfirmed = model.DeclarationConfirmed,
                        DigitalSignature = model.DigitalSignature,
                        SignatureDate = DateTime.Now
                    }
                };

                // Save supporting documents
                if (model.SupportingDocuments != null)
                {
                    dispute.SupportingDocuments = new List<SupportingDocument>();
                    foreach (var file in model.SupportingDocuments)
                    {
                        var document = new SupportingDocument
                        {
                            DocumentId = Guid.NewGuid().ToString(),
                            FileName = file.FileName,
                            FilePath = await SaveFileAsync(file),
                            ContentType = file.ContentType,
                            UploadDate = DateTime.Now,
                            Description = model.DocumentDescriptions?.FirstOrDefault(),
                            DocumentType = "Dispute Evidence" // Required field
                        };
                        dispute.SupportingDocuments.Add(document);
                    }
                }

                _context.Dispute.Add(dispute);
                await _context.SaveChangesAsync();
                // After saving the dispute:
                await _notificationService.CreateNotification(
                    userId: debt.Lender.UserId, // Lender's user ID
                    title: "New Dispute Created",
                    message: $"Student {currentUser.FullName} has disputed debt {debt.Id}",
                    type: NotificationType.DisputeCreated,
                    relatedEntityId: dispute.DisputeId,
                    relatedEntityType: RelatedEntityType.Dispute,
                    actionUrl: $"/lender/disputes/{dispute.DisputeId}"
                );

                return RedirectToAction("DisputeConfirmation", new { disputeId = dispute.DisputeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting dispute.");
                ModelState.AddModelError("", "An error occurred while submitting the dispute. Please try again.");
                return View("CreateDispute", model);
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            // Implement logic to save the file to a storage location (e.g., Azure Blob Storage, local disk)
            // Return the file path or URL
            var uploadsFolder = Path.Combine("wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return the relative path (without wwwroot)
            return $"/uploads/{uniqueFileName}";
        }
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DisputeConfirmation(string disputeId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var dispute = await _context.Dispute
                .Include(d => d.Debt)
                    .ThenInclude(d => d.Lender)
                        .ThenInclude(l => l.User)
                .Include(d => d.DisputeDetail)
                .FirstOrDefaultAsync(d => d.DisputeId == disputeId && d.UserId == currentUser.Id);

            if (dispute == null)
            {
                return NotFound("Dispute not found.");
            }

            var viewModel = new DisputeConfirmationViewModel
            {
                DisputeId = dispute.DisputeId,
                DebtId = dispute.DebtId,
                LenderName = dispute.Debt.Lender.User.FullName,
                DebtAmount = dispute.Debt.CurrentBalance,
                Status = dispute.Status,
                CreatedDate = dispute.CreatedDate,
                Reason = dispute.DisputeDetail.Reason,
                RequestedResolution = dispute.DisputeDetail.RequestedResolution
            };

            return View(viewModel);
        }
    }
}