using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;

namespace IOU.Web.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationController> _logger;
        private readonly int _pageSize = 10;
        private readonly IOUWebContext _context;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationController> logger,
            IOUWebContext context)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index(int? page)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found during notification retrieval");
                    return Challenge();
                }

                _logger.LogInformation($"Fetching notifications for user: {user.Id}");

                int pageNumber = page ?? 1;
                var (notifications, totalCount) = await _notificationService.GetUserNotificationsPaged(
                    user.Id,
                    pageNumber,
                    _pageSize,
                    includeRead: true);

                _logger.LogInformation($"Retrieved {notifications.Count} notifications out of {totalCount} total for user {user.Id}");

                var pagedList = new StaticPagedList<Notification>(
                    notifications,
                    pageNumber,
                    _pageSize,
                    totalCount);

                return View(pagedList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching notifications");
                // You might want to add a friendly error message to TempData
                TempData["Error"] = "Unable to load notifications. Please try again later.";
                return View(new StaticPagedList<Notification>(
                    new List<Notification>(),
                    1,
                    _pageSize,
                    0));
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Invalid notification ID provided for marking as read");
                    return Json(new { success = false, message = "Invalid notification ID" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found while marking notification as read");
                    return Json(new { success = false, message = "User not found" });
                }

                await _notificationService.MarkAsRead(id);
                _logger.LogInformation($"Notification {id} marked as read for user {user.Id}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {id} as read");
                return Json(new { success = false, message = "An error occurred while marking notification as read" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found while marking all notifications as read");
                    return Json(new { success = false, message = "User not found" });
                }

                await _notificationService.MarkAllAsRead(user.Id);
                _logger.LogInformation($"All notifications marked as read for user {user.Id}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "An error occurred while marking all notifications as read" });
            }
        }
        [Authorize]
        public async Task<IActionResult> GetLatestNotifications()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new List<object>());
                }

                var notifications = await _notificationService.GetUserNotifications(user.Id, includeRead: false);
                var latestNotifications = notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        CreatedAt = n.CreatedAt.ToString("g"),
                        IsRead = n.IsRead,
                        ActionUrl = n.ActionUrl
                    }).ToList();

                return Json(latestNotifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest notifications");
                return Json(new List<object>());
            }
        }

        [Authorize]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.Notification
                    .Where(n => n.UserId == user.Id && !n.IsRead && !n.IsDeleted)
                    .CountAsync();

                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unread count");
                return Json(new { count = 0 });
            }
        }

        // Add a diagnostic endpoint for debugging
        [Authorize]
        public async Task<IActionResult> Debug()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Json(new { error = "User not found" });

                var (notifications, totalCount) = await _notificationService.GetUserNotificationsPaged(
                    user.Id,
                    1,
                    100, // Get more notifications for debugging
                    includeRead: true);

                return Json(new
                {
                    userId = user.Id,
                    userEmail = user.Email,
                    totalNotifications = totalCount,
                    notifications = notifications.Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.CreatedAt,
                        n.IsRead,
                        n.IsDeleted,
                        n.Type
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stack = ex.StackTrace });
            }
        }
    }
}
