class NotificationManager {
    constructor() {
        this.initializeEventListeners();
        this.startPeriodicRefresh();
        this.loadNotifications(); // Initial load
    }

    initializeEventListeners() {
        // Click handler for notification dropdown to load notifications
        $('#notificationDropdown').on('show.bs.dropdown', () => this.loadNotifications());

        // Mark all as read
        $('.mark-all-read').on('click', (e) => {
            e.preventDefault();
            this.markAllAsRead();
        });

        // Mark individual notification as read when clicked
        $(document).on('click', '.notification-item', (e) => {
            const notificationId = $(e.currentTarget).data('notification-id');
            if (notificationId) {
                this.markAsRead(notificationId);
            }
            const actionUrl = $(e.currentTarget).data('action-url');
            if (actionUrl) {
                window.location.href = actionUrl;
            }
        });
    }

    loadNotifications() {
        $.get('/Notification/GetLatestNotifications', (data) => {
            let notificationHtml = '';
            if (data && data.length > 0) {
                data.forEach(notification => {
                    notificationHtml += this.createNotificationHtml(notification);
                });
            } else {
                notificationHtml = this.createEmptyNotificationHtml();
            }
            $('#notificationList').html(notificationHtml);
            this.updateNotificationCount(); // Update count after loading
        }).fail(() => console.error('Failed to load notifications'));
    }

    createNotificationHtml(notification) {
        return `
            <div class="dropdown-item notification-item ${notification.IsRead ? '' : 'unread'}" 
                 data-action-url="${notification.ActionUrl || ''}"
                 data-notification-id="${notification.Id}">
                <div class="d-flex justify-content-between align-items-center">
                    <div class="fw-bold">${notification.Title}</div>
                    <small class="text-muted">${notification.CreatedAt}</small>
                </div>
                <div class="small">${notification.Message}</div>
                ${notification.ActionUrl ?
                `<a href="${notification.ActionUrl}" class="btn btn-link btn-sm px-0">View Details</a>` : ''}
            </div>`;
    }

    createEmptyNotificationHtml() {
        return `<div class="p-3 text-center text-muted">No new notifications</div>`;
    }

    updateNotificationCount() {
        $.get('/Notification/GetUnreadCount', (data) => {
            const badge = $('.notification-count');
            data.count > 0 ? badge.text(data.count).show() : badge.hide();
        }).fail(() => console.error('Failed to update notification count'));
    }

    markAsRead(notificationId) {
        $.post('/Notification/MarkAsRead', { id: notificationId }, () => {
            this.loadNotifications();
        });
    }

    markAllAsRead() {
        $.post('/Notification/MarkAllAsRead', () => {
            this.loadNotifications();
        });
    }

    startPeriodicRefresh() {
        setInterval(() => {
            this.loadNotifications();
        }, 60000);
    }
}

// Initialize when the document is ready
$(document).ready(() => new NotificationManager());