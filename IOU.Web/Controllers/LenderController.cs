using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Models.ViewModels;
using IOU.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IOU.Web.Services.Interfaces;

namespace IOU.Web.Controllers
{
    public class LenderController : Controller
    {
        private readonly IOUWebContext _context;
        public readonly IDebtCalculationService _calculationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LenderController(IOUWebContext context, UserManager<ApplicationUser> userManager, IDebtCalculationService calculationService)
        {
            _context = context;
            _userManager = userManager;
            _calculationService = calculationService;
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

                    var debt = new Debt
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

                    _context.Debt.Add(debt);
                    await _context.SaveChangesAsync();

                    //// Create notification for student
                    //var notification = new Notification
                    //{
                    //    UserId = student.UserId,
                    //    Title = "New Debt Created",
                    //    Message = $"A new {model.DebtType} debt of {model.PrincipalAmount:C} has been created.",
                    //    CreatedAt = DateTime.UtcNow,
                    //    IsRead = false
                    //};

                    //_context.Notifications.Add(notification);
                    //await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Debt created successfully.";
                    return RedirectToAction(nameof(Dashboard));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating debt: " + ex.Message);
                }
            }
            return View(model);
        }
    }
  }

