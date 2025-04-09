class DebtManager {
    constructor() {
        this.initializeDebtForm();
        this.initializePaymentScheduleCalculator();
    }

    initializeDebtForm() {
        const $debtForm = $('#debtForm');
        if ($debtForm.length) {
            $debtForm.on('submit', this.handleDebtFormSubmit.bind(this));
        }
    }

    initializePaymentScheduleCalculator() {
        if ($('#PrincipalAmount').length && $('#DueDate').length) {
            this.setupPaymentCalculator();
        }
    }

    setupPaymentCalculator() {
        const calculateLoanDetails = () => {
            const principal = parseFloat($('#PrincipalAmount').val()) || 0;
            const interestRate = parseFloat($('#InterestRate').val()) || 0;
            const interestType = $('#InterestType').val();
            const calculationPeriod = $('#CalculationPeriod').val();
            const dueDate = new Date($('#DueDate').val());
            const firstPaymentDate = new Date($('#FirstPaymentDate').val());
            const today = new Date();
            const numberOfPayments = parseInt($('#NumberOfPayments').val()) || null;

            if (principal > 0 && interestRate > 0 && dueDate > today) {
                let paymentPeriod = numberOfPayments ||
                    ((dueDate.getFullYear() - firstPaymentDate.getFullYear()) * 12) +
                    (dueDate.getMonth() - firstPaymentDate.getMonth());

                let totalInterest, monthlyPayment;

                if (interestType === 'Simple') {
                    totalInterest = principal * (interestRate / 100) * (paymentPeriod / 12);
                    monthlyPayment = (principal + totalInterest) / paymentPeriod;
                } else {
                    const n = calculationPeriod === 'Monthly' ? 12 :
                        calculationPeriod === 'Quarterly' ? 4 : 1;
                    const r = interestRate / (100 * n);
                    const t = paymentPeriod / 12;
                    const amount = principal * Math.pow(1 + r, n * t);
                    totalInterest = amount - principal;
                    monthlyPayment = amount / paymentPeriod;
                }

                $('#monthlyPayment').text(`Ksh${monthlyPayment.toFixed(2)}`);
                $('#totalInterest').text(`Ksh${totalInterest.toFixed(2)}`);
                $('#totalAmount').text(`Ksh${(principal + totalInterest).toFixed(2)}`);
            }
        };

        $('#PrincipalAmount, #InterestRate, #InterestType, #CalculationPeriod, ' +
            '#DueDate, #FirstPaymentDate, #NumberOfPayments, #PaymentFrequency')
            .on('input change', calculateLoanDetails);

        calculateLoanDetails();
    }

    async handleDebtFormSubmit(e) {
        e.preventDefault();
        const $form = $(e.target);
        const $submitBtn = $form.find('button[type="submit"]');

        try {
            $submitBtn.prop('disabled', true)
                .html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...');

            const response = await $.ajax({
                url: $form.attr('action'),
                method: $form.attr('method'),
                data: $form.serialize()
            });

            if (response.redirectUrl) {
                window.location.href = response.redirectUrl;
            }
        } catch (error) {
            console.error('Error submitting debt form:', error);
            this.showAlert('error', error.responseJSON?.message || 'Error creating debt');
            $submitBtn.prop('disabled', false).text('Create Debt');
        }
    }

    showAlert(type, message) {
        const alertClass = `alert-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'info'}`;
        const $alert = $(`
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `);

        $('.alert-container').prepend($alert);
        setTimeout(() => $alert.alert('close'), 5000);
    }
}

class NotificationManager {
    constructor() {
        this.initializeEventListeners();
        this.loadNotifications();
        this.updateUnreadCount();
        this.pollingInterval = setInterval(() => {
            this.loadNotifications();
            this.updateUnreadCount();
        }, 60000);
    }

    initializeEventListeners() {
        $(document).on('click', '.notification-item', (e) => {
            if (!$(e.target).is('a')) {
                this.markAsRead($(e.currentTarget).data('id'));
            }
        });

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

        if (!notifications?.length) {
            $notificationList.append('<div class="notification-item text-center py-3"><div class="text-muted">No new notifications</div></div>');
            return;
        }

        notifications.forEach(notification => {
            const actionBtn = notification.actionUrl ?
                `<div class="mt-2"><a href="${notification.actionUrl}" class="btn btn-sm btn-outline-primary">View Details</a></div>` : '';

            $notificationList.append(`
                <div class="notification-item ${notification.isRead ? '' : 'unread'}" data-id="${notification.id}">
                    <div class="notification-time">${new Date(notification.createdAt).toLocaleString()}</div>
                    <div class="notification-title">${notification.title}</div>
                    <div class="notification-message">${notification.message}</div>
                    ${actionBtn}
                </div>
            `);
        });
    }

    async updateUnreadCount() {
        try {
            const response = await $.get('/Notifications/GetUnreadCount');
            const count = response.count || 0;
            const $badge = $('.notification-count');
            $badge.text(count).toggle(count > 0);
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



// Initialize all managers when DOM is ready
$(document).ready(() => {
    new DebtManager();
    new NotificationManager();
});