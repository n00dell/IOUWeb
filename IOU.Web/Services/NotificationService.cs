using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace IOU.Web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IOUWebContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRazorViewRenderer _razorViewRenderer;

        public NotificationService(
            IOUWebContext context,
            ILogger<NotificationService> logger,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            IRazorViewRenderer razorViewRenderer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService;
            _userManager = userManager;
            _razorViewRenderer = razorViewRenderer;
        }

        public async Task CreateNotification(
            string userId,
            string title,
            string message,
            NotificationType type,
            string? relatedEntityId = null,
            RelatedEntityType? relatedEntityType = null,
            string? actionUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                try
                {



                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found: {UserId}", userId);
                        return;
                    }
                    // Render the Razor view to HTML
                    var htmlContent = await _razorViewRenderer.RenderViewToStringAsync(
                        "/Views/Emails/NotificationEmail.cshtml",
                        new NotificationEmailModel
                        {
                            Title = title,
                            Message = message,
                            ActionUrl = actionUrl
                        }
                    );

                    // Send the email
                    await _emailService.SendEmailAsync(user.Email, title, htmlContent);
                }catch(Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send email notification for user: {UserId}", userId);
                    
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Title = title.Trim(),
                    Message = message.Trim(),
                    Type = type,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    RelatedEntityId = relatedEntityId,  
                    ActionUrl = actionUrl
                };

                _context.Notification.Add(notification);
                await _context.SaveChangesAsync();

                

                _logger.LogInformation(
                    "Created notification: {NotificationId} for user: {UserId}, Type: {NotificationType}",
                    notification.Id, userId, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating notification for user: {UserId}, Type: {NotificationType}",
                    userId, type);
                throw;
            }
        }

        public async Task CreateBulkNotifications(List<Notification> notifications)
        {
            try
            {
                if (notifications == null || !notifications.Any())
                    return;

                foreach (var notification in notifications)
                {
                    notification.Id = Guid.NewGuid().ToString();
                    notification.CreatedAt = DateTime.UtcNow;
                }

                await _context.Notification.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Created {Count} bulk notifications",
                    notifications.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk notifications");
                throw;
            }
        }

        public async Task<(List<Notification> Items, int TotalCount)> GetUserNotificationsPaged(
            string userId,
            int page = 1,
            int pageSize = 20,
            bool includeRead = false)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching notifications for user: {UserId}, Page: {Page}, PageSize: {PageSize}",
                    userId, page, pageSize);

                var query = _context.Notification
                    .Where(n => n.UserId == userId
                           && !n.IsDeleted
                           && (!n.IsRead || includeRead));

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation(
                    "Retrieved {Count} notifications out of {Total} for user: {UserId}",
                    items.Count, totalCount, userId);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching notifications for user: {UserId}, Page: {Page}",
                    userId, page);
                throw;
            }
        }

        public async Task<IList<Notification>> GetUserNotifications(
            string userId,
            bool includeRead = false)
        {
            try
            {
                var notifications = await _context.Notification
                    .Where(n => n.UserId == userId
                           && !n.IsDeleted
                           && (!n.IsRead || includeRead))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation(
                    "Retrieved {Count} notifications for user: {UserId}",
                    notifications.Count, userId);

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving notifications for user: {UserId}",
                    userId);
                throw;
            }
        }

        public async Task MarkAsRead(string notificationId)
        {
            try
            {
                var notification = await _context.Notification.FindAsync(notificationId);
                if (notification == null)
                {
                    _logger.LogWarning(
                        "Notification not found: {NotificationId}",
                        notificationId);
                    return;
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Marked notification as read: {NotificationId}",
                    notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error marking notification as read: {NotificationId}",
                    notificationId);
                throw;
            }
        }

        public async Task MarkAllAsRead(string userId)
        {
            try
            {
                var notifications = await _context.Notification
                    .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                    .ToListAsync();

                if (!notifications.Any())
                    return;

                var now = DateTime.UtcNow;
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Marked {Count} notifications as read for user: {UserId}",
                    notifications.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error marking all notifications as read for user: {UserId}",
                    userId);
                throw;
            }
        }

        public async Task DeleteNotification(string notificationId)
        {
            try
            {
                var notification = await _context.Notification.FindAsync(notificationId);
                if (notification == null)
                {
                    _logger.LogWarning(
                        "Notification not found for deletion: {NotificationId}",
                        notificationId);
                    return;
                }

                notification.IsDeleted = true;
                notification.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Deleted notification: {NotificationId}",
                    notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting notification: {NotificationId}",
                    notificationId);
                throw;
            }
        }

        public async Task CleanupOldNotifications(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var oldNotifications = await _context.Notification
                    .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                    .ToListAsync();

                if (!oldNotifications.Any())
                    return;

                _context.Notification.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Cleaned up {Count} old notifications older than {Days} days",
                    oldNotifications.Count, daysToKeep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error cleaning up old notifications");
                throw;
            }
        }

        public async Task NotifyAdmin(string title, string message, NotificationType type, string? relatedEntityId = null, RelatedEntityType? relatedEntityType = null, string? actionUrl = null)
        {
            try
            {
                // Fetch all admin users
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (!adminUsers.Any())
                {
                    _logger.LogWarning("No admin users found to send notifications to.");
                    return;
                }

                // Create notifications for each admin
                foreach (var admin in adminUsers)
                {
                    await CreateNotification(
                        admin.Id,
                        title,
                        message,
                        type,
                        relatedEntityId,
                        relatedEntityType,
                        actionUrl
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin notifications.");
                throw;
            }
        }
        public async Task<(List<Notification> Items, int TotalCount)> GetAdminNotificationsPaged(
    int page = 1,
    int pageSize = 20,
    NotificationType? type = null,
    bool? isRead = null)
        {
            try
            {
                var query = _context.Notification
                    .Where(n => !n.IsDeleted);

                // Apply filters
                if (type.HasValue)
                {
                    query = query.Where(n => n.Type == type.Value);
                }

                if (isRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == isRead.Value);
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching admin notifications.");
                throw;
            }
        }
    }
}