using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOU.Web.Admin.Admin.Controllers
{     
    [Area("Admin")]

    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        
        public IActionResult Index()
        {
            return View();
        }
    }
}
