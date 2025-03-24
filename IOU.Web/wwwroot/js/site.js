class NotificationManager {
    constructor() {
        this.initializeEventListeners();
        this.loadNotifications();
        this.updateUnreadCount();
        this.pollingInterval = setInterval(() => {
            this.loadNotifications();
            this.updateUnreadCount();
        }, 60000); // Refresh every 60 seconds
    }

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

        if (!notifications || notifications.length === 0) {
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
}

class PaymentManager {
    constructor() {
        this.initializePaymentListeners();
    }

    initializePaymentListeners() {
        const paymentForm = document.getElementById('mpesaPaymentForm');
        if (paymentForm) {
            paymentForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                await this.handlePaymentSubmission();
            });
        }
    }

    async handlePaymentSubmission() {
        const debtId = document.getElementById('debtId').value;
        const amount = parseFloat(document.getElementById('amount').value);
        const phone = document.getElementById('phone').value;

        if (!this.validatePaymentInputs(debtId, amount, phone)) {
            return;
        }

        // Show loading indicator
        this.showPaymentProgress("Processing payment...");

        try {
            const response = await fetch('/api/mpesa/initiate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    debtId: debtId,
                    amount: amount,
                    phoneNumber: phone
                })
            });

            // Get the raw response text first
            const responseText = await response.text();
            console.log("Raw response:", responseText);

            // If the response is not OK, throw an error with details
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}, Response: ${responseText}`);
            }

            // Try to parse as JSON if there's content
            let result;
            if (responseText.trim()) {
                try {
                    result = JSON.parse(responseText);
                } catch (jsonError) {
                    console.error("JSON parse error:", jsonError);
                    throw new Error(`Failed to parse response as JSON: ${responseText}`);
                }
            } else {
                throw new Error("Received empty response from server");
            }

            if (result.success) {
                this.showPaymentSuccess();
                this.pollPaymentStatus(result.paymentId, result.checkoutRequestID);
            } else {
                this.showPaymentError(result.message || "Payment failed with no specific error message");
            }
        } catch (error) {
            console.error('Payment error:', error);
            this.showPaymentError('Payment failed: ' + error.message);
        }
    }

    validatePaymentInputs(debtId, amount, phone) {
        if (!debtId || !amount || !phone) {
            this.showPaymentError('Please fill all fields');
            return false;
        }
        if (amount <= 0) {
            this.showPaymentError('Amount must be greater than 0');
            return false;
        }
        // Add phone number validation
        if (!this.isValidPhoneNumber(phone)) {
            this.showPaymentError('Please enter a valid phone number');
            return false;
        }
        return true;
    }

    isValidPhoneNumber(phone) {
        // Basic validation for Kenyan phone numbers
        // Accepts formats like: 07xx, 254xx, +254xx with 9-12 digits
        return /^(\+?254|0)?[7][0-9]{8,9}$/.test(phone);
    }

    async pollPaymentStatus(paymentId, checkoutRequestId) {
        const maxAttempts = 12; // Poll for up to 1 minute (12 * 5 seconds)
        let attempts = 0;

        const interval = setInterval(async () => {
            try {
                console.log(`Polling payment status (attempt ${attempts + 1}/${maxAttempts})...`);
                const response = await fetch(`/api/mpesa/status/${paymentId}`);

                if (!response.ok) {
                    console.warn(`Error response when polling: ${response.status}`);

                    // If we keep getting errors, eventually stop polling
                    attempts++;
                    if (attempts >= maxAttempts) {
                        clearInterval(interval);
                        this.showPaymentError('Polling timed out. Please check your payment status manually.');
                    }
                    return;
                }

                const status = await response.json();
                console.log("Payment status update:", status);

                if (status.status === 'Paid') {
                    clearInterval(interval);
                    this.showPaymentSuccess('Payment completed successfully!');
                    window.location.reload();
                } else if (status.status === 'Failed') {
                    clearInterval(interval);
                    this.showPaymentError('Payment failed: ' + (status.message || 'Unknown reason'));
                } else {
                    // Still pending, continue polling
                    attempts++;
                    if (attempts >= maxAttempts) {
                        clearInterval(interval);
                        this.showPaymentMessage('Payment status not confirmed. Please check your M-Pesa for the transaction status.');
                    }
                }
            } catch (error) {
                console.error('Error polling payment status:', error);
                attempts++;
                if (attempts >= maxAttempts) {
                    clearInterval(interval);
                    this.showPaymentError('Error checking payment status. Please verify manually.');
                }
            }
        }, 5000); // Poll every 5 seconds
    }

    showPaymentProgress(message) {
        // Create or update a progress modal/message
        console.log('Progress:', message);
        // Replace with your UI update code
        this.showAlert('info', message);
    }

    showPaymentSuccess(message = 'Payment initiated successfully! Check your phone to complete payment') {
        console.log('Success:', message);
        // Replace with your UI update code
        this.showAlert('success', message);
    }

    showPaymentError(message) {
        console.error('Error:', message);
        // Replace with your UI update code
        this.showAlert('error', message);
    }

    showPaymentMessage(message) {
        console.log('Message:', message);
        // Replace with your UI update code
        this.showAlert('info', message);
    }

    showAlert(type, message) {
        // Simple implementation - replace with your preferred UI approach
        const alertClass = type === 'error' ? 'alert-danger' :
            type === 'success' ? 'alert-success' : 'alert-info';

        // Create alert element
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert ${alertClass} alert-dismissible fade show`;
        alertDiv.role = 'alert';
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;

        // Find container and append
        const container = document.querySelector('.payment-alerts') || document.querySelector('.container');
        if (container) {
            // Remove any existing alerts
            const existingAlerts = container.querySelectorAll('.alert');
            existingAlerts.forEach(alert => alert.remove());

            // Add new alert at the top
            if (container.firstChild) {
                container.insertBefore(alertDiv, container.firstChild);
            } else {
                container.appendChild(alertDiv);
            }

            // Auto dismiss after 8 seconds
            setTimeout(() => {
                alertDiv.classList.remove('show');
                setTimeout(() => alertDiv.remove(), 300);
            }, 8000);
        } else {
            // Fallback to alert if container not found
            alert(`${type.toUpperCase()}: ${message}`);
        }
    }
}

// Initialize when the document is ready
$(document).ready(() => {
    new NotificationManager();
    new PaymentManager();
});