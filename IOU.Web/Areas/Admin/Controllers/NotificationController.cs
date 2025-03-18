using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata;
using X.PagedList;

namespace IOU.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NotificationController : Controller
    {

        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationController> _logger;
        private readonly int _pageSize = 10;

        public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? page, NotificationType? type, bool? isRead)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found during notification retrieval");
                    return Challenge();
                }

                int pageNumber = page ?? 1;
                var (notifications, totalCount) = await _notificationService.GetAdminNotificationsPaged(
                    pageNumber,
                    _pageSize,
                    type,
                    isRead
                );

                var pagedList = new StaticPagedList<Notification>(
                    notifications,
                    pageNumber,
                    _pageSize,
                    totalCount);

                ViewBag.NotificationTypes = Enum.GetValues<NotificationType>();
                ViewBag.SelectedType = type;
                ViewBag.IsReadFilter = isRead;

                return View(pagedList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching admin notifications");
                TempData["Error"] = "Unable to load notifications. Please try again later.";
                return View(new StaticPagedList<Notification>(
                    new List<Notification>(),
                    1,
                    _pageSize,
                    0));
            }

        }
        [HttpPost("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            try
            {
                await _notificationService.MarkAsRead(request.Id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                await _notificationService.MarkAllAsRead(user.Id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
