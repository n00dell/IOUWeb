using IOU.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IOU.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LenderController : Controller
    {
        private readonly IOUWebContext _context;

        public LenderController(IOUWebContext context)
        {
            _context = context;
        }
        public async Task<ActionResult> Index()
        {
            var lenders = await _context.Lender
                .Include(l => l.User)
                .Where(l => !l.User.IsActive )
                .ToListAsync();
            return View(lenders);
        }
        // GET: Admin/Lender/Approve/{id}
        public async Task<IActionResult> Approve(string id)
        {
            var lender = await _context.Lender
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == id);

            if (lender == null)
            {
                return NotFound();
            }

            lender.User.IsActive = true; // Approve the lender
            _context.Update(lender);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lender approved successfully.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Reject(string id)
        {
            var lender = await _context.Lender
                .Include(l => l.User)
                .Include(l => l.IssuedDebts) // Include related debts
                    .ThenInclude(d => d.Disputes) // Include related disputes
                .FirstOrDefaultAsync(l => l.UserId == id);

            if (lender == null)
            {
                return NotFound();
            }

            // Delete all related disputes
            foreach (var debt in lender.IssuedDebts)
            {
                _context.Dispute.RemoveRange(debt.Disputes);
            }

            // Delete all related debts
            _context.Debt.RemoveRange(lender.IssuedDebts);

            // Remove the lender
            _context.Lender.Remove(lender);

            // Remove the associated user
            _context.Users.Remove(lender.User);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lender rejected successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
