using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOU.Web.Controllers
{
    public class StudentController : Controller
    {
        [Authorize(Roles = "Student")]
        public IActionResult Dashboard()
        {
            return View();
        }
        [Authorize(Roles = "Student")]
        public IActionResult MyDebts()
        {
            return View();
        }

        [Authorize(Roles = "Student")]
        public IActionResult PaymentHistory()
        {
            return View();
        }
    }
}
