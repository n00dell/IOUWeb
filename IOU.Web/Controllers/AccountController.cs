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
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOUWebContext context, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register(UserType userType)
        {
            
            switch (userType){
                
                case UserType.Lender:
                    return View("LenderRegister", new LenderRegisterViewModel());
                case UserType.Student:
                    var studentModel = new StudentRegisterViewModel
                    {
                        ExpectedGraduationDate = DateTime.Today.AddMonths(1)
                    };
                    return View("StudentRegister", studentModel);
                default:
                    return RedirectToAction("Index", "Home");
                }
           
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStudent(StudentRegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("StudentRegister", model);
                }

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
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View("StudentRegister", model);
                    }
                    
                    // Create student record
                    var student = new Student
                    {
                        UserId = user.Id,
                        StudentId = model.StudentId,
                        ExpectedGraduationDate = model.ExpectedGraduationDate,
                        University = model.University
                    };

                    // Add role and claims
                    var roleResult = await _userManager.AddToRoleAsync(user, "Student");
                    if (!roleResult.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to assign student role");
                        return View("StudentRegister", model);
                    }

                    var claimResult = await _userManager.AddClaimAsync(user,
                        new System.Security.Claims.Claim("UserType", "Student"));
                    if (!claimResult.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to add user claims");
                        return View("StudentRegister", model);
                    }

                    _context.Student.Add(student);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Sign in
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect to dashboard
                    return RedirectToAction("Dashboard", "Student");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty,
                        $"An error occurred during registration: {ex.Message}");
                    return View("StudentRegister", model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty,
                    "An unexpected error occurred. Please try again.");
                return View("StudentRegister", model);
            }
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
                        UserType = UserType.Lender,
                        IsActive = true,
                        PhoneNumber = model.PhoneNumber
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        
                        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("UserType", "Lender"));
                        var roleResult = await _userManager.AddToRoleAsync(user, "Lender");
                        if (!roleResult.Succeeded)
                        {
                            // Log role assignment failure
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, $"Role assignment failed: {error.Description}");
                            }
                            throw new Exception("Role assignment failed");
                        }
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
                try
                {
                    // Find user
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        _logger.LogWarning($"Login attempt failed: User not found for email {model.Email}");
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }

                    // Check if user is active
                    if (!user.IsActive)
                    {
                        _logger.LogWarning($"Login attempt failed: User {model.Email} is not active");
                        ModelState.AddModelError(string.Empty, "This account is not active.");
                        return View(model);
                    }

                    // Attempt sign in
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,  // Use email directly since it's the username
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User {model.Email} logged in successfully");

                        // Get user's role
                        var roles = await _userManager.GetRolesAsync(user);
                        var userRole = roles.FirstOrDefault();

                        _logger.LogInformation($"User {model.Email} has role: {userRole}");

                        // Redirect based on role
                        switch (userRole)
                        {
                            case "Student":
                                return RedirectToAction("Dashboard", "Student");
                            case "Lender":
                                return RedirectToAction("Dashboard", "Lender");
                            case "Admin":
                                return RedirectToAction("Index", "Dashboard" , new { area = "Admin"});
                            default:
                                return RedirectToAction("Index", "Home");
                        }
                    }

                    // Log failed attempt
                    _logger.LogWarning($"Failed login attempt for user {model.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Login error: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An error occurred during login.");
                }
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
