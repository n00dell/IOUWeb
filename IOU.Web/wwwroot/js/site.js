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

class PaymentManager {
    constructor() {
        this.paymentForm = document.getElementById('mpesaPaymentForm');
        if (!this.paymentForm) return;

        this.amountInput = document.getElementById('amount');
        this.phoneInput = document.getElementById('phone');
        this.debtIdInput = document.getElementById('debtId');
        this.submitButton = this.paymentForm.querySelector('button[type="submit"]');
        this.pollingInterval = null;

        this.initializeEventListeners();
    }

    initializeEventListeners() {
        this.paymentForm.addEventListener('submit', this.handlePaymentSubmit.bind(this));
        this.amountInput?.addEventListener('input', this.validateAmount.bind(this));
        this.phoneInput?.addEventListener('input', this.validatePhoneNumber.bind(this));
    }

    validateAmount() {
        const amount = parseFloat(this.amountInput.value);
        const isValid = !isNaN(amount) && amount >= 10; // Minimum KES 10
        this.toggleInputValidity(this.amountInput, isValid);
        return isValid;
    }

    validatePhoneNumber() {
        const phone = this.phoneInput.value.trim();
        const isValid = /^(?:254|\+254|0)?(7[0-9]{8})$/.test(phone);
        this.toggleInputValidity(this.phoneInput, isValid);
        return isValid;
    }

    toggleInputValidity(input, isValid) {
        input.classList.toggle('is-invalid', !isValid);
        input.classList.toggle('is-valid', isValid);
    }

    async handlePaymentSubmit(event) {
        event.preventDefault();

        if (!this.validateAmount() || !this.validatePhoneNumber()) {
            this.showToast('error', 'Amount must be at least KES 10 and phone number valid');
            return;
        }

        this.disableSubmitButton();

        try {
            const response = await fetch('/api/payment/initiate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    debtId: this.debtIdInput.value,
                    amount: Math.max(10, parseFloat(this.amountInput.value)), // Ensure minimum KES 10
                    phoneNumber: this.formatPhoneNumber(this.phoneInput.value.trim())
                })
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Payment failed');
            }

            const result = await response.json();
            this.startStatusPolling(result.CheckoutRequestID);

        } catch (error) {
            console.error('Payment error:', error);
            this.showToast('error', error.message.includes('{') ?
                JSON.parse(error.message).Message : error.message);
            this.enableSubmitButton();
        }
    }
    formatPhoneNumber(phone) {
        // Convert 07... to 2547...
        return phone.replace(/^0/, '254');
    }

    startStatusPolling(checkoutRequestId) {
        let attempts = 0;
        const maxAttempts = 20;
        const baseDelay = 3000;

        const poll = async () => {
            attempts++;

            try {
                const response = await fetch(`/api/payment/payment-status/${encodeURIComponent(checkoutRequestId)}`);
                const status = await response.json();

                if (!response.ok) throw new Error(status.error || 'Status check failed');

                if (status.confirmed) {
                    this.showToast('success', `Payment of ${status.amount} confirmed! Receipt: ${status.receipt}`);
                    window.location.href = `/Student/PaymentSuccess?debtId=${this.debtIdInput.value}&receiptNo=${status.receipt}&amount=${status.amount}`;
                    return;
                }

                if (status.status === 'Failed' || attempts >= maxAttempts) {
                    throw new Error(status.error || 'Payment confirmation timeout');
                }

                setTimeout(poll, baseDelay * Math.pow(1.5, attempts));

            } catch (error) {
                this.showToast('error', error.message);
            }
        };

        poll();
    }

    stopPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = null;
        }
    }

    disableSubmitButton() {
        if (this.submitButton) {
            this.submitButton.disabled = true;
            this.submitButton.innerHTML = `
                <span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                Processing...
            `;
        }
    }

    enableSubmitButton() {
        if (this.submitButton) {
            this.submitButton.disabled = false;
            this.submitButton.textContent = 'Submit Payment';
        }
    }

    showToast(type, message) {
        const toastContainer = this.getToastContainer();
        const icon = { success: '✓', error: '✗', info: 'ⓘ' }[type];

        const toast = document.createElement('div');
        toast.className = `toast toast-${type} align-items-center border-0`;
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${icon} ${message}</div>
                <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        toastContainer.appendChild(toast);
        new bootstrap.Toast(toast, {
            autohide: true,
            delay: type === 'error' ? 5000 : 3000
        }).show();
    }

    getToastContainer() {
        let container = document.getElementById('toastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            document.body.appendChild(container);
        }
        return container;
    }
}

// Initialize all managers when DOM is ready
$(document).ready(() => {
    new DebtManager();
    new NotificationManager();
    new PaymentManager();
});