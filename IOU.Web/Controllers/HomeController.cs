using System.Diagnostics;
using IOU.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOU.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userTypeClaim = User.Claims.FirstOrDefault(c => c.Type == "UserType");
                if (userTypeClaim != null)
                {
                    return userTypeClaim.Value switch
                    {
                        "Student" => RedirectToAction("Index", "StudentDashboard"),
                        "Lender" => RedirectToAction("Index", "LenderDashboard"),
                        "Guardian" => RedirectToAction("Index", "GuardianDashboard"),
                        "Admin" => RedirectToAction("Index", "AdminDashboard"),
                        _ => View()
                    };
                }
            }
            return View();
        
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
