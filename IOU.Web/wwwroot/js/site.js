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
        // Initialize calculator if on debt creation page
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
                // Calculate payment period based on whether number of payments is specified
                let paymentPeriod;
                let paymentFrequency = $('#PaymentFrequency').val();

                if (numberOfPayments) {
                    paymentPeriod = numberOfPayments;
                } else {
                    // Auto-calculate based on frequency
                    const months = ((dueDate.getFullYear() - firstPaymentDate.getFullYear()) * 12 +
                        (dueDate.getMonth() - firstPaymentDate.getMonth()));
                    paymentPeriod = months;
                }

                let totalInterest = 0;
                let monthlyPayment = 0;

                if (interestType === 'Simple') {
                    totalInterest = (principal * (interestRate / 100) * (paymentPeriod / 12));
                    monthlyPayment = (principal + totalInterest) / paymentPeriod;
                } else {
                    // Compound interest calculation
                    const n = calculationPeriod === 'Monthly' ? 12 :
                        calculationPeriod === 'Quarterly' ? 4 : 1;

                    const r = interestRate / (100 * n);
                    const t = paymentPeriod / 12;
                    const amount = principal * Math.pow(1 + r, n * t);
                    totalInterest = amount - principal;
                    monthlyPayment = amount / paymentPeriod;
                }

                $('#monthlyPayment').text('Ksh' + monthlyPayment.toFixed(2));
                $('#totalInterest').text('Ksh' + totalInterest.toFixed(2));
                $('#totalAmount').text('Ksh' + (principal + totalInterest).toFixed(2));
            }
        };

        // Calculate on input change
        $('#PrincipalAmount, #InterestRate, #InterestType, #CalculationPeriod, ' +
            '#DueDate, #FirstPaymentDate, #NumberOfPayments, #PaymentFrequency')
            .on('input change', calculateLoanDetails);

        // Initial calculation
        calculateLoanDetails();
    }

    async handleDebtFormSubmit(e) {
        e.preventDefault();
        const $form = $(e.target);
        const $submitBtn = $form.find('button[type="submit"]');

        try {
            $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...');

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
        const alertClass = type === 'error' ? 'alert-danger' :
            type === 'success' ? 'alert-success' : 'alert-info';

        const $alert = $(`
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `);

        $('.alert-container').prepend($alert);

        setTimeout(() => {
            $alert.alert('close');
        }, 5000);
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
            const notificationId = $(e.currentTarget).data('id');
            if (!$(e.target).is('a')) {
                this.markAsRead(notificationId);
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

// ... (keep all previous code exactly the same until the PaymentManager class)

class PaymentManager {
    constructor() {
        this.paymentForm = document.getElementById('mpesaPaymentForm');
        this.amountInput = document.getElementById('amount');
        this.phoneInput = document.getElementById('phone');
        this.debtIdInput = document.getElementById('debtId');
        this.submitButton = this.paymentForm?.querySelector('button[type="submit"]');
        this.pollingInterval = null;

        this.initializeEventListeners();
    }

    initializeEventListeners() {
        if (!this.paymentForm) return;

        this.paymentForm.addEventListener('submit', this.handlePaymentSubmit.bind(this));
        this.amountInput?.addEventListener('input', this.validateAmount.bind(this));
        this.phoneInput?.addEventListener('input', this.validatePhoneNumber.bind(this));
    }

    validateAmount() {
        const amount = parseFloat(this.amountInput.value);
        const isValid = !isNaN(amount) && amount > 0;
        this.toggleInputValidity(this.amountInput, isValid);
        return isValid;
    }

    validatePhoneNumber() {
        const phoneRegex = /^(0|254)?[7][0-9]{8}$/;
        const isValid = phoneRegex.test(this.phoneInput.value.trim());
        this.toggleInputValidity(this.phoneInput, isValid);
        return isValid;
    }

    toggleInputValidity(inputElement, isValid) {
        inputElement.classList.toggle('is-invalid', !isValid);
        inputElement.classList.toggle('is-valid', isValid);
    }

    async handlePaymentSubmit(event) {
        event.preventDefault();
        if (!this.validateAmount() || !this.validatePhoneNumber()) {
            this.showToast('error', 'Please correct the form errors');
            return;
        }

        this.disableSubmitButton();

        try {
            const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const payload = {
                debtId: this.debtIdInput.value,
                amount: parseFloat(this.amountInput.value),
                phoneNumber: this.phoneInput.value.trim()
            };

            const response = await fetch('/Student/MakeCustomPayment', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': csrfToken
                },
                body: JSON.stringify(payload)
            });

            console.log('Response status:', response.status);
            const text = await response.text();
            console.log('Raw response:', text);

            if (!text) {
                throw new Error('Server returned empty response');
            }

            let result;
            try {
                result = JSON.parse(text);
            } catch (e) {
                throw new Error(`Invalid JSON response: ${text.slice(0, 100)}`);
            }

            if (!response.ok) {
                throw new Error(result.message || `HTTP error ${response.status}`);
            }

            this.showToast('info', 'Check your phone to complete payment...');
            this.startStatusPolling(result.checkoutRequestId);

        } catch (error) {
            console.error('Payment error:', error);
            this.showToast('error', error.message);
            this.enableSubmitButton();
        }
    }

    startStatusPolling(checkoutRequestId) {
        const maxAttempts = 60;
        let attempts = 0;

        this.pollingInterval = setInterval(async () => {
            if (attempts >= maxAttempts) {
                this.stopPolling();
                this.showToast('error', 'Payment confirmation timed out');
                return;
            }

            try {
                const response = await fetch(`/Student/payment-status/${encodeURIComponent(checkoutRequestId)}`);

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const status = await response.json();

                if (status.confirmed) {
                    this.stopPolling();
                    this.showToast('success', 'Payment confirmed! Updating page...');
                    setTimeout(() => window.location.reload(), 2000);
                } else if (status.status === 'Failed') {
                    this.stopPolling();
                    this.showToast('error', `Payment failed: ${status.resultDescription || 'Unknown error'}`);
                    this.enableSubmitButton();
                } else {
                    // Show progress update every 5 checks
                    if (attempts % 5 === 0) {
                        this.showToast('info', `Waiting for payment confirmation (${attempts * 5}s)`);
                    }
                }

                if (++attempts >= maxAttempts) {
                    this.stopPolling();
                    this.showToast('warning', 'Payment confirmation taking longer than expected');
                    this.enableSubmitButton();
                }


            } catch (error) {
                console.error('Polling error:', error);
                this.stopPolling();
                this.showToast('error', 'Failed to check payment status');
                this.enableSubmitButton();
            }
        }, 5000);
    }

    stopPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = null;
        }
        this.enableSubmitButton();
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
        const icon = {
            success: '✓',
            error: '✗',
            info: 'ⓘ'
        }[type];

        const toast = document.createElement('div');
        toast.className = `toast toast-${type} align-items-center border-0`;
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${icon} ${message}</div>
                <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        toastContainer.appendChild(toast);
        this.initializeToastBehavior(toast, type);
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

    initializeToastBehavior(toastElement, type) {
        const bsToast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: type === 'error' ? 5000 : 3000
        });
        bsToast.show();
    }
}

// Main initialization - must be outside all class definitions
$(document).ready(() => {
    new DebtManager();
    new NotificationManager();
    new PaymentManager();
});