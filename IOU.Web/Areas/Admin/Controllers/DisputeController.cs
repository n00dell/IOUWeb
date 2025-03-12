using Microsoft.AspNetCore.Mvc;

namespace IOU.Web.Areas.Admin.Controllers
{
    public class DisputeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
