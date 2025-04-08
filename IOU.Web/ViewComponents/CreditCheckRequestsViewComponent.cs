using IOU.Web.Data;
using IOU.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.ViewComponents
{
    public class CreditCheckRequestsViewComponent : ViewComponent
    {
        private readonly IOUWebContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreditCheckRequestsViewComponent(
            IOUWebContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null) return Content(string.Empty);

            var count = await _context.CreditReportRequests
                .CountAsync(r => r.StudentEmail == currentUser.Email &&
                               !r.ResponseDate.HasValue);

            return View(count);
        }
    }
}
