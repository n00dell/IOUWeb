using IOU.Web.Models;

namespace IOU.Web.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotification(string userId, string title, string message,
            NotificationType type, string? relatedEntityId = null,
            RelatedEntityType? relatedEntityType = null, string? actionUrl = null);
        Task CreateBulkNotifications(List<Notification> notifications);
        Task<(List<Notification> Items, int TotalCount)> GetUserNotificationsPaged(
            string userId, int page = 1, int pageSize = 20, bool includeRead = false);
        Task<IList<Notification>> GetUserNotifications(string userId, bool includeRead = false);
        Task MarkAsRead(string notificationId);
        Task MarkAllAsRead(string userId);
        Task DeleteNotification(string notificationId);
        Task CleanupOldNotifications(int daysToKeep = 30);
    }
}
