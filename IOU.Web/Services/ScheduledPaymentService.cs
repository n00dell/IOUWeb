using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Services
{
    public class ScheduledPaymentService : ISchedulePaymentService
    {
        private readonly IOUWebContext _context;
        private readonly IDebtService _debtService;
        private readonly IDebtCalculationService _calculationService;
        private readonly ILogger<ScheduledPaymentService> _logger;

        public ScheduledPaymentService(
            IOUWebContext context,
            IDebtService debtService,
            IDebtCalculationService calculationService,
            ILogger<ScheduledPaymentService> logger)
        {
            _context = context;
            _debtService = debtService;
            _calculationService = calculationService;
            _logger = logger;
        }

        public async Task<List<ScheduledPayment>> GeneratePaymentScheduleAsync(CreateScheduledPaymentsRequest request)
        {
            var debt = await _context.Debt
                .FirstOrDefaultAsync(d => d.Id == request.DebtId)
                ?? throw new ArgumentException("Debt not found");

            // Calculate total amount including projected interest if needed
            decimal totalAmount = debt.CurrentBalance;
            if (request.IncludeInterestInCalculation)
            {
                // Project interest for the payment period
                var projectedEndDate = CalculatePaymentEndDate(request.FirstPaymentDate, request.Frequency, request.NumberOfPayments);
                totalAmount += ProjectInterestAmount(debt, request.FirstPaymentDate, projectedEndDate);
            }

            var payments = new List<ScheduledPayment>();
            var basePaymentAmount = Math.Round(totalAmount / request.NumberOfPayments, 2);
            var currentDate = request.FirstPaymentDate;

            for (int i = 0; i < request.NumberOfPayments; i++)
            {
                decimal paymentAmount = basePaymentAmount;
                if (i == request.NumberOfPayments - 1)
                {
                    // Adjust last payment to account for rounding differences
                    paymentAmount = totalAmount - (basePaymentAmount * (request.NumberOfPayments - 1));
                }

                var payment = new ScheduledPayment
                {
                    DebtId = debt.Id,
                    Amount = paymentAmount,
                    DueDate = currentDate,
                    Status = PaymentStatus.Scheduled,
                    PrincipalPortion = paymentAmount, // Will be recalculated when payment is processed
                };

                payments.Add(payment);
                currentDate = CalculateNextPaymentDate(currentDate, request.Frequency);
            }

            await _context.ScheduledPayment.AddRangeAsync(payments);
            await _context.SaveChangesAsync();

            return payments;
        }

        public async Task<ScheduledPayment> ProcessPaymentAsync(string paymentId, decimal amount, string paymentMethodId)
        {
            var payment = await _context.ScheduledPayment
                .Include(p => p.Debt)
                .FirstOrDefaultAsync(p => p.Id == paymentId)
                ?? throw new ArgumentException("Payment not found");

            // Update debt calculations before processing payment
            await _debtService.UpdateDebtCalculations(payment.DebtId);

            // Calculate payment portions
            var (principalPortion, interestPortion, lateFeesPortion) = CalculatePaymentPortions(payment.Debt, amount);

            payment.Status = PaymentStatus.Paid;
            payment.PaymentDate = DateTime.UtcNow;
            payment.Amount = amount;
            payment.PrincipalPortion = principalPortion;
            payment.InterestPortion = interestPortion;
            payment.LateFeesPortion = lateFeesPortion;

            // Update debt amounts
            payment.Debt.PrincipalAmount -= principalPortion;
            payment.Debt.AccumulatedInterest -= interestPortion;
            payment.Debt.AccumulatedLateFees -= lateFeesPortion;

            await _context.SaveChangesAsync();

            // Recalculate remaining payments if needed
            await RecalculatePaymentScheduleAsync(payment.DebtId);

            return payment;
        }

        public async Task UpdatePaymentStatusesAsync(string debtId)
        {
            var payments = await _context.ScheduledPayment
                .Where(p => p.DebtId == debtId && p.Status != PaymentStatus.Paid)
                .ToListAsync();

            var currentDate = DateTime.UtcNow;

            foreach (var payment in payments)
            {
                if (payment.DueDate < currentDate && payment.Status != PaymentStatus.Overdue)
                {
                    payment.Status = PaymentStatus.Overdue;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecalculatePaymentScheduleAsync(string debtId)
        {
            var remainingPayments = await _context.ScheduledPayment
                .Where(p => p.DebtId == debtId && p.Status == PaymentStatus.Scheduled)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

            var debt = await _context.Debt.FindAsync(debtId);
            if (debt == null || !remainingPayments.Any()) return;

            var totalRemaining = debt.CurrentBalance;
            var basePaymentAmount = Math.Round(totalRemaining / remainingPayments.Count, 2);

            for (int i = 0; i < remainingPayments.Count; i++)
            {
                if (i == remainingPayments.Count - 1)
                {
                    // Adjust last payment for rounding
                    remainingPayments[i].Amount = totalRemaining;
                }
                else
                {
                    remainingPayments[i].Amount = basePaymentAmount;
                    totalRemaining -= basePaymentAmount;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ScheduledPayment> GetScheduledPaymentAsync(string id)
        {
            return await _context.ScheduledPayment
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ArgumentException("Scheduled payment not found");
        }

        public async Task<List<ScheduledPayment>> GetPaymentsByDebtIdAsync(string debtId)
        {
            return await _context.ScheduledPayment
                .Where(p => p.DebtId == debtId)
                .ToListAsync();
        }

        private (decimal principal, decimal interest, decimal lateFees) CalculatePaymentPortions(Debt debt, decimal paymentAmount)
        {
            decimal remainingPayment = paymentAmount;
            decimal lateFees = Math.Min(remainingPayment, debt.AccumulatedLateFees);
            remainingPayment -= lateFees;

            decimal interest = Math.Min(remainingPayment, debt.AccumulatedInterest);
            remainingPayment -= interest;

            decimal principal = Math.Min(remainingPayment, debt.PrincipalAmount);

            return (principal, interest, lateFees);
        }

        private DateTime CalculateNextPaymentDate(DateTime currentDate, PaymentFrequency frequency)
        {
            return frequency switch
            {
                PaymentFrequency.Weekly => currentDate.AddDays(7),
                PaymentFrequency.Biweekly => currentDate.AddDays(14),
                PaymentFrequency.Monthly => currentDate.AddMonths(1),
                PaymentFrequency.Quarterly => currentDate.AddMonths(3),
                _ => throw new ArgumentException("Invalid frequency")
            };
        }

        private DateTime CalculatePaymentEndDate(DateTime startDate, PaymentFrequency frequency, int numberOfPayments)
        {
            return frequency switch
            {
                PaymentFrequency.Weekly => startDate.AddDays(7 * numberOfPayments),
                PaymentFrequency.Biweekly => startDate.AddDays(14 * numberOfPayments),
                PaymentFrequency.Monthly => startDate.AddMonths(numberOfPayments),
                PaymentFrequency.Quarterly => startDate.AddMonths(3 * numberOfPayments),
                _ => throw new ArgumentException("Invalid frequency")
            };
        }

        private decimal ProjectInterestAmount(Debt debt, DateTime startDate, DateTime endDate)
        {
            // Simple projection - you might want to make this more sophisticated
            var numberOfDays = (endDate - startDate).Days;
            return _calculationService.CalculateInterest(debt, startDate.AddDays(numberOfDays));
        }
    }
}

