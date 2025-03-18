using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DebtController : Controller
    {
        private readonly IOUWebContext _context;

        public DebtController(IOUWebContext context)
        {
            _context = context;
        }
        // GET: Admin/Debt
        public async Task<IActionResult> Index()
        {
            var debts = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .Include(d => d.Student)
                    .ThenInclude(s => s.User)
                .ToListAsync();

            return View(debts);
        }
        public async Task<IActionResult> Edit(string id)
        {
            var debt = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .Include(d => d.Student)
                    .ThenInclude(s => s.User)
                .Include(d => d.Disputes)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (debt == null)
            {
                return NotFound();
            }

            var viewModel = new EditDebtViewModel
            {
                Id = debt.Id,
                PrincipalAmount = debt.PrincipalAmount,
                CurrentBalance = debt.CurrentBalance,
                Purpose = debt.Purpose,
                InterestRate = debt.InterestRate,
                Status = debt.Status,
                LenderUserId = debt.LenderUserId,
                StudentUserId = debt.StudentUserId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditDebtViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Load the existing debt from the database
                    var existingDebt = await _context.Debt
                        .Include(d => d.Lender)
                        .Include(d => d.Student)
                        .Include(d => d.Disputes)
                        .FirstOrDefaultAsync(d => d.Id == id);

                    if (existingDebt == null)
                    {
                        return NotFound();
                    }

                    // Update only the editable fields
                    existingDebt.PrincipalAmount = model.PrincipalAmount;
                    existingDebt.CurrentBalance = model.CurrentBalance;
                    existingDebt.Purpose = model.Purpose;
                    existingDebt.InterestRate = model.InterestRate;
                    existingDebt.Status = model.Status;

                    _context.Update(existingDebt);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Debt updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DebtExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // If the model state is invalid, reload the related entities for the view
            return View(model);
        }
        // GET: Admin/Debt/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            var debt = await _context.Debt
                .Include(d => d.Lender)
                    .ThenInclude(l => l.User)
                .Include(d => d.Student)
                    .ThenInclude(s => s.User)
                .Include(d => d.Disputes) // Include related disputes
                .FirstOrDefaultAsync(d => d.Id == id);

            if (debt == null)
            {
                return NotFound();
            }

            return View(debt);
        }

        // POST: Admin/Debt/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var debt = await _context.Debt
                .Include(d => d.Disputes) // Include related disputes
                .FirstOrDefaultAsync(d => d.Id == id);

            if (debt == null)
            {
                return NotFound();
            }

            // Remove related disputes
            _context.Dispute.RemoveRange(debt.Disputes);

            // Remove the debt
            _context.Debt.Remove(debt);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Debt deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        private bool DebtExists(string id)
        {
            return _context.Debt.Any(e => e.Id == id);
        }
    }
}
