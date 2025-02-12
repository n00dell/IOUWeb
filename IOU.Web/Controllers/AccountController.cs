using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IOU.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOUWebContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOUWebContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register(UserType userType)
        {
            switch (userType){
                
                case UserType.Lender:
                    return View("LenderRegister", new LenderRegisterViewModel());
                case UserType.Student:
                    return View("StudentRegister", new StudentRegisterViewModel());
                case UserType.Guardian:
                    return View("GuardianRegister", new GuardianRegisterViewModel());
                default:
                    return RedirectToAction("Index", "Home");
                }
           
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStudent(StudentRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FullName = model.FullName,
                        UserType = UserType.Student,
                        IsActive = true,
                        PhoneNumber = model.PhoneNumber
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        var student = new Student
                        {
                            UserId = user.Id,
                            StudentId = model.StudentId,
                            ExpectedGraduationDate = model.ExpectedGraduationDate,
                            University = model.University
                        };
                        _context.Student.Add(student);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Dashboard", "Student");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                } 
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RegisterLender(LenderRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FullName = model.FullName,
                        UserType = UserType.Student,
                        IsActive = true,
                        PhoneNumber = model.PhoneNumber
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        var lender = new Lender
                        {
                            UserId = user.Id,
                            CompanyName = model.CompanyName,
                            BusinessRegistrationNumber = model.BusinessRegistrationNumber
                        };
                        _context.Lender.Add(lender);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Dashboard", "Lender");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return View(model);
        }



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false
                ).Result;
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    return user.UserType switch
                    {
                        UserType.Admin => RedirectToAction("Dashboard", "Admin"),
                        UserType.Lender => RedirectToAction("Dashboard", "Lender"),
                        UserType.Student => RedirectToAction("Dashboard", "Student"),
                        UserType.Guardian => RedirectToAction("Index", "Guardian"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }

 }
