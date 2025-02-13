using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IOU.Web.Controllers
{
    public class LenderController : Controller
    {
        private readonly IOUWebContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LenderController(IOUWebContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Lender")]
        public IActionResult Dashboard()
        {
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "Lender")]
        public IActionResult CreateDebt()
        {
            var viewModel = new CreateDebtViewModel
            {
                DueDate = DateTime.Today.AddMonths(1),
                GracePeriodDays = 7
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lender")]
        public async Task<IActionResult> CreateDebt(CreateDebtViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get current lender
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        ModelState.AddModelError("", "Current user not found.");
                        return View(model);
                    }

                    var lender = await _context.Lender
                        .FirstOrDefaultAsync(l => l.UserId == currentUser.Id);
                    if (lender == null)
                    {
                        ModelState.AddModelError("", "Lender profile not found.");
                        return View(model);
                    }

                    // Find student
                    var studentUser = await _userManager.FindByEmailAsync(model.StudentEmail);
                    if (studentUser == null)
                    {
                        ModelState.AddModelError("StudentEmail", "Student not found.");
                        return View(model);
                    }

                    var student = await _context.Student
                        .FirstOrDefaultAsync(s => s.UserId == studentUser.Id);
                    if (student == null)
                    {
                        ModelState.AddModelError("StudentEmail", "Student profile not found.");
                        return View(model);
                    }

                    // Validate dates
                    if (model.DueDate <= DateTime.Today)
                    {
                        ModelState.AddModelError("DueDate", "Due date must be in the future.");
                        return View(model);
                    }

                    var debt = new Debt
                    {
                        Id = Guid.NewGuid().ToString(),
                        LenderId = lender.UserId, // Using UserId as it's the primary key
                        StudentId = student.UserId, // Using UserId as it's the primary key
                        PrincipalAmount = model.PrincipalAmount,
                        CurrentBalance = model.PrincipalAmount,
                        InterestRate = model.InterestRate,
                        DateIssued = DateTime.UtcNow,
                        DueDate = model.DueDate,
                        CreatedAt = DateTime.UtcNow,
                        LateFeeAmount = model.LateFeeAmount,
                        GracePeriodDays = model.GracePeriodDays,
                        Purpose = model.Purpose,
                        Status = DebtStatus.Pending,
                        AccumulatedInterest = 0
                    };

                    _context.Debt.Add(debt);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Debt created successfully.";
                    return RedirectToAction(nameof(Dashboard));
                }
                catch (Exception ex)
                {
                    // Log the exception details
                    Console.WriteLine($"Error creating debt: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError("", $"Error creating debt: {ex.Message}");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
}
