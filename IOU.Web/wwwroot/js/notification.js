console.log('Notification script loaded!');
class NotificationManager {
    constructor() {
        this.notificationDropdown = document.querySelector('#notificationDropdown');
        this.notificationList = document.querySelector('#notificationList');
        this.notificationCount = document.querySelector('.notification-count');
        this.markAllReadButton = document.querySelector('.mark-all-read');

        if (this.notificationList) {
            this.initialize();
        } else {
            console.error('Notification elements not found in DOM');
        }
    }

    initialize() {
        console.log('Initializing notification manager');
        this.loadNotifications();
        this.setupEventListeners();
        setInterval(() => this.loadNotifications(), 60000);
    }

    async loadNotifications() {
        try {
            console.log('Fetching notifications...');
            const response = await fetch('/Notification/GetLatestNotifications');
            if (!response.ok) {
                throw new Error(`Network response error: ${response.status}`);
            }
            const notifications = await response.json();
            console.log('Notifications received:', notifications);
            this.renderNotifications(notifications);
            this.updateNotificationCount();
        } catch (error) {
            console.error('Failed to load notifications:', error);
        }
    }

    renderNotifications(notifications) {
        if (!this.notificationList) return;

        if (!notifications || notifications.length === 0) {
            this.notificationList.innerHTML = `<div class="dropdown-item text-center text-muted">No notifications</div>`;
            return;
        }

        this.notificationList.innerHTML = notifications.map(n => this.createNotificationHTML(n)).join('');
    }

    createNotificationHTML(notification) {
        const date = new Date(notification.createdAt);
        const formattedDate = date.toLocaleString();

        return `
            <div class="dropdown-item notification-item ${notification.isRead ? '' : 'unread'}" 
                 data-id="${notification.id}">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <div class="small text-muted">${formattedDate}</div>
                        <div class="fw-bold">${notification.title}</div>
                        <div class="small">${notification.message}</div>
                    </div>
                    ${!notification.isRead ? `
                    <button class="btn btn-sm btn-link mark-read p-0">
                        <i class="fas fa-check text-primary"></i>
                    </button>` : ''}
                </div>
                ${notification.actionUrl ? `
                <div class="mt-2">
                    <a href="${notification.actionUrl}" class="btn btn-sm btn-outline-primary">
                        View Details
                    </a>
                </div>` : ''}
            </div>`;
    }

    async updateNotificationCount() {
        try {
            const response = await fetch('/Notification/GetUnreadCount');
            if (!response.ok) throw new Error('Failed to get unread count');
            const data = await response.json();

            if (this.notificationCount) {
                this.notificationCount.textContent = data.count;
                this.notificationCount.style.display = data.count > 0 ? 'block' : 'none';
            }
        } catch (error) {
            console.error('Error updating notification count:', error);
        }
    }

    setupEventListeners() {
        if (this.notificationList) {
            this.notificationList.addEventListener('click', async (e) => {
                const markReadButton = e.target.closest('.mark-read');
                if (markReadButton) {
                    const notificationItem = e.target.closest('.notification-item');
                    if (notificationItem) {
                        const notificationId = notificationItem.dataset.id;
                        await this.markAsRead(notificationId);
                    }
                }
            });
        }

        if (this.markAllReadButton) {
            this.markAllReadButton.addEventListener('click', async (e) => {
                e.preventDefault();
                await this.markAllAsRead();
            });
        }

        // Also find and attach to the button on the notifications index page
        const indexPageMarkAllButton = document.getElementById('markAllRead');
        if (indexPageMarkAllButton) {
            indexPageMarkAllButton.addEventListener('click', async (e) => {
                e.preventDefault();
                await this.markAllAsRead();
            });
        }
    }

    async markAsRead(notificationId) {
        try {
            const response = await fetch('/Notification/MarkAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    // Add anti-forgery token if needed
                },
                body: JSON.stringify({ id: notificationId })
            });

            if (!response.ok) throw new Error('Failed to mark notification as read');

            await this.loadNotifications();
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    }

    async markAllAsRead() {
        try {
            const response = await fetch('/Notification/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    // Add anti-forgery token if needed
                }
            });

            if (!response.ok) throw new Error('Failed to mark all notifications as read');

            await this.loadNotifications();
        } catch (error) {
            console.error('Error marking all notifications as read:', error);
        }
    }
}

// Initialize when DOM is completely loaded
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, initializing notification manager');
    window.notificationManager = new NotificationManager();
});