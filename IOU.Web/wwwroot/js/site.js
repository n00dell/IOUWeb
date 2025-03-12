class NotificationManager {
    constructor() {
        this.initializeEventListeners();
        this.loadNotifications();
        this.updateUnreadCount();
        setInterval(() => {
            this.loadNotifications();
            this.updateUnreadCount();
        }, 60000); // Refresh every 60 seconds
    }

    initializeEventListeners() {
        // Mark individual notification as read when clicking "View Details"
        $(document).on('click', '.notification-item a', (e) => {
            const notificationId = $(e.currentTarget).closest('.notification-item').data('id');
            this.markAsRead(notificationId);
        });

        // Mark all notifications as read
        $('#markAllRead').on('click', (e) => {
            e.preventDefault();
            this.markAllAsRead();
        });
    }

    async loadNotifications() {
        try {
            const response = await $.get('/Notifications/GetLatestNotifications');
            this.renderNotifications(response);
        } catch (error) {
            console.error('Failed to load notifications:', error);
        }
    }

    renderNotifications(notifications) {
        const $notificationList = $('#notificationList');
        $notificationList.empty();

        if (notifications.length === 0) {
            $notificationList.append(`
                <div class="notification-item text-center py-3">
                    <div class="text-muted">No new notifications</div>
                </div>
            `);
            return;
        }

        notifications.forEach(notification => {
            const notificationHtml = `
                <div class="notification-item ${notification.isRead ? '' : 'unread'}" 
                     data-id="${notification.id}">
                    <div class="notification-time">
                        ${new Date(notification.createdAt).toLocaleString()}
                    </div>
                    <div class="notification-title">${notification.title}</div>
                    <div class="notification-message">${notification.message}</div>
                    ${notification.actionUrl ? `
                    <div class="mt-2">
                        <a href="${notification.actionUrl}" 
                           class="btn btn-sm btn-outline-primary">
                            View Details
                        </a>
                    </div>` : ''}
                </div>`;
            $notificationList.append(notificationHtml);
        });
    }

    async updateUnreadCount() {
        try {
            const response = await $.get('/Notifications/GetUnreadCount');
            const count = response.count || 0;
            const $badge = $('.notification-count');

            $badge.text(count);
            count > 0 ? $badge.show() : $badge.hide();
        } catch (error) {
            console.error('Error updating unread count:', error);
        }
    }

    async markAsRead(notificationId) {
        try {
            const response = await $.ajax({
                url: '/Notifications/MarkAsRead',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ Id: notificationId })
            });

            if (response.success) {
                this.loadNotifications();
                this.updateUnreadCount();
            }
        } catch (error) {
            console.error('Failed to mark notification as read:', error.responseJSON?.error || error.statusText);
        }
    }

    async markAllAsRead() {
        try {
            const response = await $.post('/Notifications/MarkAllAsRead');
            if (response.success) {
                this.loadNotifications();
                this.updateUnreadCount();
            }
        } catch (error) {
            console.error('Failed to mark all notifications as read:', error.responseJSON?.error || error.statusText);
        }
    }
    // Update event listener for automatic marking
    initializeEventListeners() {
        // Mark as read when clicking anywhere in the notification item
        $(document).on('click', '.notification-item', (e) => {
            const notificationId = $(e.currentTarget).data('id');
            if (!$(e.target).is('a')) { // Don't mark read if clicking a link
                this.markAsRead(notificationId);
            }
        });

        // Mark all as read
        $('#markAllRead').on('click', (e) => {
            e.preventDefault();
            this.markAllAsRead();
        });
    }
}

// Initialize when the document is ready
$(document).ready(() => new NotificationManager());