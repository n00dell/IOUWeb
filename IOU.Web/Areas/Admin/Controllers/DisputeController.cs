using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Areas.Admin.Controllers
{
    [Area("Admin")]

    [Authorize(Roles = "Admin")]
    public class DisputeController : Controller
    {
        private readonly IOUWebContext _context;
        private readonly INotificationService _notificationService;

        public DisputeController(IOUWebContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var disputes = await _context.Dispute
                .Include(d => d.User)
                .Include(d => d.Debt)
                    .ThenInclude(d => d.Lender)
                .Include(d => d.DisputeDetail)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();

            return View(disputes);
        }
        public async Task<IActionResult> Details(string id)
        {
            var dispute = await _context.Dispute
                .Include(d => d.User)
                .Include(d => d.Debt)
                    .ThenInclude(d => d.Lender)
                        .ThenInclude(l => l.User)
                .Include(d => d.DisputeDetail)
                .Include(d => d.SupportingDocuments)
                .Include(d => d.LenderEvidence)
                .FirstOrDefaultAsync(d => d.DisputeId == id);

            if (dispute == null)
            {
                return NotFound();
            }

            return View(dispute);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToDispute(string id, DisputeStatus status, string adminNotes,
           ResolutionType? resolutionType, decimal? adjustedAmount)
        {
            var dispute = await _context.Dispute
                .Include(d => d.Debt)
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DisputeId == id);

            if (dispute == null)
            {
                return NotFound();
            }

            // Update dispute status and admin notes
            dispute.Status = status;
            dispute.AdminNotes = adminNotes;
            dispute.ResolvedDate = DateTime.UtcNow;

            // Handle resolution actions
            if (status == DisputeStatus.Approved)
            {
                await HandleApprovedDispute(dispute, resolutionType, adjustedAmount);
            }

            _context.Update(dispute);
            await _context.SaveChangesAsync();

            // Send notifications
            await SendNotifications(dispute);

            return RedirectToAction(nameof(Details), new { id });
        }
        private async Task HandleApprovedDispute(Dispute dispute, ResolutionType? resolutionType, decimal? adjustedAmount)
        {
            var debt = dispute.Debt;

            switch (resolutionType)
            {
                case ResolutionType.CompleteDebtCancellation:
                    debt.CurrentBalance = 0;
                    debt.Status = DebtStatus.Cancelled;
                    break;

                case ResolutionType.PartialReduction when adjustedAmount.HasValue:
                    debt.CurrentBalance = Math.Max(0, debt.CurrentBalance - adjustedAmount.Value);
                    break;

                case ResolutionType.InterestRateReduction:
                    // You would need to implement interest rate adjustment logic
                    break;
            }

            _context.Debt.Update(debt);
        }
        private async Task SendNotifications(Dispute dispute)
        {
            // Notify student
            await _notificationService.CreateNotification(
                dispute.UserId,
                "Dispute Resolved",
                $"Your dispute for debt {dispute.DebtId} has been resolved with status: {dispute.Status}",
                NotificationType.DisputeResolved,
                dispute.DisputeId,
                RelatedEntityType.Dispute,
                $"/Student/DisputeDetails/{dispute.DisputeId}"
            );

            // Notify lender
            await _notificationService.CreateNotification(
                dispute.Debt.LenderUserId,
                "Dispute Resolved",
                $"Dispute {dispute.DisputeId} for debt {dispute.DebtId} has been resolved",
                NotificationType.DisputeResolved,
                dispute.DisputeId,
                RelatedEntityType.Dispute,
                $"/Lender/DisputeDetails/{dispute.DisputeId}"
            );
        }
    }
}
