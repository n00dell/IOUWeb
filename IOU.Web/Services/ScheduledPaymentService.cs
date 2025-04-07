using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Services
{
    public class ScheduledPaymentService : IScheduledPaymentService
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
            var debt = await _context.Debt.FindAsync(request.DebtId)
                ?? throw new ArgumentException("Debt not found");

            int numberOfPayments = request.NumberOfPayments ??
                CalculateAutoPayments(request.FirstPaymentDate, debt.DueDate, request.Frequency);

            if (numberOfPayments <= 0)
                throw new ArgumentException("Invalid payment schedule configuration");

            return debt.InterestType switch
            {
                InterestType.Compound => await GenerateCompoundSchedule(debt, request.FirstPaymentDate, numberOfPayments, request.Frequency),
                _ => await GenerateSimpleSchedule(debt, request.FirstPaymentDate, numberOfPayments, request.Frequency, request.IncludeInterestInCalculation)
            };
        }
        
        private int GetMonthDifference(DateTime startDate, DateTime endDate)
        {
            return (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month);
        }

        private DateTime GetNextPaymentDate(DateTime currentDate, PaymentFrequency frequency)
        {
            return frequency switch
            {
                PaymentFrequency.Weekly => currentDate.AddDays(7),
                PaymentFrequency.Biweekly => currentDate.AddDays(14),
                PaymentFrequency.Monthly => currentDate.AddMonths(1),
                PaymentFrequency.Quarterly => currentDate.AddMonths(3),
                _ => currentDate.AddMonths(1) // Default to monthly
            };
        }
        private int GetNumberOfPeriods(DateTime startDate, DateTime endDate, InterestCalculationPeriod period)
        {
            TimeSpan duration = endDate - startDate;

            return period switch
            {
                InterestCalculationPeriod.Daily => (int)Math.Ceiling(duration.TotalDays),
                InterestCalculationPeriod.Monthly => (int)Math.Ceiling(duration.TotalDays / 30),
                InterestCalculationPeriod.Quarterly => (int)Math.Ceiling(duration.TotalDays / 91),
                InterestCalculationPeriod.SemiAnnually => (int)Math.Ceiling(duration.TotalDays / 182),
                InterestCalculationPeriod.Annually => (int)Math.Ceiling(duration.TotalDays / 365),
                _ => throw new ArgumentException("Invalid calculation period")
            };
        }
        private async Task<List<ScheduledPayment>> GenerateCompoundSchedule(
        Debt debt, DateTime firstPaymentDate, int numberOfPayments,
        PaymentFrequency frequency)
        {
            // Get number of periods between dates based on calculation period
            int periods = GetNumberOfPeriods(firstPaymentDate, debt.DueDate, debt.CalculationPeriod);

            // Rate is already per period (e.g., 5% daily = 5% per day)
            decimal rate = debt.InterestRate / 100m;

            decimal totalAmount = debt.PrincipalAmount * (decimal)Math.Pow(1 + (double)rate, periods);
            decimal totalInterest = totalAmount - debt.PrincipalAmount;

            var payments = await GenerateInstallments(debt, totalAmount, firstPaymentDate, numberOfPayments, frequency);

            // Split principal/interest portions
            foreach (var payment in payments)
            {
                payment.PrincipalPortion = payment.Amount * (debt.PrincipalAmount / totalAmount);
                payment.InterestPortion = payment.Amount * (totalInterest / totalAmount);
            }

            return payments;
        }
        private async Task<List<ScheduledPayment>> GenerateAmortizedSchedule(
            Debt debt, DateTime firstPaymentDate, int numberOfPayments,
            PaymentFrequency frequency, bool isCustomInstallments)
        {
            decimal ratePerPeriod = GetPeriodicInterestRate(debt);
            decimal remainingPrincipal = debt.PrincipalAmount;
            var payments = new List<ScheduledPayment>();
            DateTime currentDate = firstPaymentDate;

            for (int i = 0; i < numberOfPayments; i++)
            {
                bool isLastPayment = (i == numberOfPayments - 1);
                decimal interest = remainingPrincipal * ratePerPeriod;
                decimal principal = isLastPayment
                    ? remainingPrincipal
                    : CalculatePMT(ratePerPeriod, numberOfPayments - i, remainingPrincipal) - interest;

                payments.Add(new ScheduledPayment
                {
                    DebtId = debt.Id,
                    Amount = principal + interest,
                    DueDate = currentDate,
                    PrincipalPortion = principal,
                    InterestPortion = interest,
                    Status = ScheduledPaymentStatus.Scheduled
                });

                remainingPrincipal -= principal;
                currentDate = GetNextPaymentDate(currentDate, frequency, isCustomInstallments, debt.DueDate, i + 1, numberOfPayments);
            }

            await _context.ScheduledPayment.AddRangeAsync(payments);
            await _context.SaveChangesAsync();
            return payments;
        }

        private DateTime GetNextPaymentDate(DateTime currentDate, PaymentFrequency frequency, bool isCustom, DateTime dueDate, int paymentIndex, int totalPayments)
        {
            if (isCustom)
            {
                // Calculate equal intervals for custom installments
                double totalDays = (dueDate - currentDate).TotalDays;
                double interval = totalDays / (totalPayments - 1);
                return currentDate.AddDays(interval * paymentIndex);
            }

            return frequency switch
            {
                PaymentFrequency.Weekly => currentDate.AddDays(7),
                PaymentFrequency.Biweekly => currentDate.AddDays(14),
                PaymentFrequency.Monthly => currentDate.AddMonths(1),
                PaymentFrequency.Quarterly => currentDate.AddMonths(3),
                _ => throw new ArgumentException("Invalid frequency")
            };
        }

        private DateTime GetLastPaymentDate(DateTime startDate, PaymentFrequency frequency, int numberOfPayments)
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
        private async Task<List<ScheduledPayment>> GenerateSimpleSchedule(
        Debt debt, DateTime firstPaymentDate, int numberOfPayments,
        PaymentFrequency frequency, bool includeInterest)
        {
            decimal totalAmount = debt.PrincipalAmount;

            if (includeInterest)
            {
                // Get number of periods between dates based on calculation period
                int periods = GetNumberOfPeriods(firstPaymentDate, debt.DueDate, debt.CalculationPeriod);

                // Rate is already per period (e.g., 5% monthly = 5% per month)
                decimal rate = debt.InterestRate / 100m;

                totalAmount += debt.PrincipalAmount * rate * periods;
            }

            return await GenerateInstallments(debt, totalAmount, firstPaymentDate, numberOfPayments, frequency);
        }
        public async Task<ScheduledPayment> ProcessPaymentAsync(string paymentId, decimal amount, string paymentMethodId)
        {
            var payment = await _context.ScheduledPayment
                .Include(p => p.Debt)
                .FirstOrDefaultAsync(p => p.Id == paymentId)
                ?? throw new ArgumentException("Payment not found");

            // Update debt calculations first
            await _debtService.UpdateDebtCalculations(payment.DebtId);

            // Create payment record
            var paymentRecord = new Payment
            {
                DebtId = payment.DebtId,
                ScheduledPaymentId = payment.Id,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                Status = PaymentTransactionStatus.Paid
            };

            _context.Payments.Add(paymentRecord);

            // Process the payment
            if (payment.InterestPortion > 0)
            {
                // Amortized payment - use pre-calculated portions
                payment.Status = ScheduledPaymentStatus.Paid;
                payment.PaymentDate = DateTime.UtcNow;

                // Update debt
                payment.Debt.PrincipalAmount -= payment.PrincipalPortion;
                payment.Debt.AccumulatedInterest -= payment.InterestPortion;
            }
            else
            {
                // Simple payment - calculate portions
                var (principal, interest, lateFees) = CalculatePaymentPortions(payment.Debt, amount);

                payment.Amount = amount;
                payment.PrincipalPortion = principal;
                payment.InterestPortion = interest;
                payment.LateFeesPortion = lateFees;

                // Update debt
                payment.Debt.PrincipalAmount -= principal;
                payment.Debt.AccumulatedInterest -= interest;
                payment.Debt.AccumulatedLateFees -= lateFees;
            }

            await _context.SaveChangesAsync();
            await RecalculatePaymentScheduleAsync(payment.DebtId);

            return payment;
        }
        private decimal GetPeriodicInterestRate(Debt debt)
        {
            int periodsPerYear = debt.CalculationPeriod switch
            {
                InterestCalculationPeriod.Daily => 365,
                InterestCalculationPeriod.Monthly => 12,
                InterestCalculationPeriod.Quarterly => 4,
                InterestCalculationPeriod.Annually => 1,
                _ => throw new ArgumentException("Invalid calculation period")
            };
            return debt.InterestRate / 100m;
        }
        private int CalculateAutoPayments(DateTime firstDate, DateTime dueDate, PaymentFrequency frequency)
        {
            if (firstDate >= dueDate) return 0;

            return frequency switch
            {
                PaymentFrequency.Weekly => (int)Math.Ceiling((dueDate - firstDate).TotalDays / 7),
                PaymentFrequency.Biweekly => (int)Math.Ceiling((dueDate - firstDate).TotalDays / 14),
                PaymentFrequency.Monthly => (int)Math.Ceiling((dueDate - firstDate).TotalDays / 30),
                PaymentFrequency.Quarterly => (int)Math.Ceiling((dueDate - firstDate).TotalDays / 91),
                PaymentFrequency.SemiAnnually => (int)Math.Ceiling((dueDate - firstDate).TotalDays / 182),
                PaymentFrequency.Annually => (int)Math.Ceiling((dueDate - firstDate).TotalDays / 365),
                _ => (int)Math.Ceiling((dueDate - firstDate).TotalDays / 30) // Default to monthly
            };
        }


  
        public async Task UpdatePaymentStatusesAsync(string debtId)
        {
            var payments = await _context.ScheduledPayment
                .Where(p => p.DebtId == debtId && p.Status != ScheduledPaymentStatus.Paid)
                .ToListAsync();

            var currentDate = DateTime.UtcNow;

            foreach (var payment in payments)
            {
                if (payment.DueDate < currentDate && payment.Status != ScheduledPaymentStatus.Overdue)
                {
                    payment.Status = ScheduledPaymentStatus.Overdue;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecalculatePaymentScheduleAsync(string debtId)
        {
            var debt = await _context.Debt.FindAsync(debtId);
            if (debt == null) return;

            var remainingPayments = await _context.ScheduledPayment
                .Where(p => p.DebtId == debtId && p.Status == ScheduledPaymentStatus.Scheduled)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

            if (!remainingPayments.Any()) return;

            var totalRemaining = debt.CurrentBalance;

            if (debt.InterestType == InterestType.Compound)
            {
                // For compound interest, recalculate the entire schedule
                await RegenerateCompoundSchedule(debt, remainingPayments);
            }
            else
            {
                // For simple interest, just redistribute remaining amount
                var basePayment = Math.Round(totalRemaining / remainingPayments.Count, 2);

                for (int i = 0; i < remainingPayments.Count; i++)
                {
                    if (i == remainingPayments.Count - 1)
                    {
                        // Last payment gets any remainder due to rounding
                        remainingPayments[i].Amount = totalRemaining;
                    }
                    else
                    {
                        remainingPayments[i].Amount = basePayment;
                        totalRemaining -= basePayment;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        private async Task RegenerateCompoundSchedule(Debt debt, List<ScheduledPayment> remainingPayments)
        {
            // Remove existing scheduled payments
            _context.ScheduledPayment.RemoveRange(remainingPayments);

            // Get the first payment date (either today or the next due date)
            var firstPaymentDate = remainingPayments.Min(p => p.DueDate) > DateTime.UtcNow
                ? remainingPayments.Min(p => p.DueDate)
                : DateTime.UtcNow;

            // Get number of remaining payments
            var numberOfPayments = remainingPayments.Count;

            // Regenerate the schedule
            var newPayments = await GenerateCompoundSchedule(
                debt,
                firstPaymentDate,
                numberOfPayments,
                PaymentFrequency.Monthly); // Or get frequency from debt

            // Add the new payments
            await _context.ScheduledPayment.AddRangeAsync(newPayments);
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

        public (decimal principal, decimal interest, decimal lateFees) CalculatePaymentPortions(Debt debt, decimal paymentAmount)
        {
            decimal remainingPayment = paymentAmount;

            // 1. Pay late fees first
            decimal lateFeesPaid = Math.Min(remainingPayment, debt.AccumulatedLateFees);
            remainingPayment -= lateFeesPaid;

            // 2. Pay interest next
            decimal interestPaid = Math.Min(remainingPayment, debt.AccumulatedInterest);
            remainingPayment -= interestPaid;

            // 3. Remainder goes to principal
            decimal principalPaid = Math.Min(remainingPayment, debt.PrincipalAmount);

            return (principalPaid, interestPaid, lateFeesPaid);
        }

        private async Task<List<ScheduledPayment>> GenerateInstallments(
    Debt debt, decimal totalAmount, DateTime firstPaymentDate,
    int numberOfPayments, PaymentFrequency frequency)
        {
            decimal basePayment = Math.Round(totalAmount / numberOfPayments, 2);
            var payments = new List<ScheduledPayment>();
            DateTime currentDate = firstPaymentDate;

            for (int i = 0; i < numberOfPayments; i++)
            {
                bool isLastPayment = (i == numberOfPayments - 1);
                decimal amount = isLastPayment
                    ? totalAmount - (basePayment * (numberOfPayments - 1))
                    : basePayment;

                payments.Add(new ScheduledPayment
                {
                    DebtId = debt.Id,
                    Amount = amount,
                    DueDate = currentDate,
                    Status = ScheduledPaymentStatus.Scheduled,
                    PrincipalPortion = 0, // Will be set by calling method if needed
                    InterestPortion = 0   // Will be set by calling method if needed
                });

                currentDate = GetNextPaymentDate(currentDate, frequency);
            }

            await _context.ScheduledPayment.AddRangeAsync(payments);
            await _context.SaveChangesAsync();
            return payments;
        }
        public async Task ProcessPaymentAgainstSchedule(Payment payment)
        {
            // Start transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var debt = await _context.Debt.FindAsync(payment.DebtId);
                if (debt == null) throw new Exception("Debt not found");

                // Update debt balance
                debt.CurrentBalance -= payment.Amount;

                // Find earliest unpaid scheduled payment
                var scheduledPayment = await _context.ScheduledPayment
                    .Where(p => p.DebtId == payment.DebtId && p.Status != ScheduledPaymentStatus.Paid)
                    .OrderBy(p => p.DueDate)
                    .FirstOrDefaultAsync();

                if (scheduledPayment != null)
                {
                    // Update scheduled payment
                    scheduledPayment.Amount -= payment.Amount;
                    if (scheduledPayment.Amount <= 0)
                    {
                        scheduledPayment.Status = ScheduledPaymentStatus.Paid;
                        scheduledPayment.PaymentDate = DateTime.UtcNow;
                    }
                }

                // Update payment reference
                if (scheduledPayment != null)
                {
                    payment.ScheduledPaymentId = scheduledPayment.Id;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        private decimal CalculatePMT(decimal ratePerPeriod, int numberOfPeriods, decimal principal)
        {
            if (ratePerPeriod == 0)
                return principal / numberOfPeriods;

            decimal factor = (decimal)Math.Pow((double)(1 + ratePerPeriod), numberOfPeriods);
            return (principal * ratePerPeriod * factor) / (factor - 1);
        }
    }
}

