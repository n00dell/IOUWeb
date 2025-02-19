using IOU.Web.Models;
using IOU.Web.Services.Interfaces;

namespace IOU.Web.Services
{
    /// <summary>
    /// Service responsible for calculating debt-related financial values including interest and late fees
    /// </summary>
    public class DebtCalculationService : IDebtCalculationService
    {
        private readonly ILogger<DebtCalculationService> _logger;

        public DebtCalculationService(ILogger<DebtCalculationService> logger)
        {
            _logger = logger;
        }

        public decimal CalculateInterest(Debt debt, DateTime calculationDate)
        {
            if (debt == null)
                throw new ArgumentNullException(nameof(debt));

            // Normalize dates to UTC if they aren't already
            var normalizedCalculationDate = calculationDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(calculationDate, DateTimeKind.Utc)
                : calculationDate.ToUniversalTime();

            var normalizedLastCalculationDate = debt.LastInterestCalculationDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(debt.LastInterestCalculationDate, DateTimeKind.Utc)
                : debt.LastInterestCalculationDate.ToUniversalTime();

            // Log the dates we're working with
            _logger.LogInformation(
                "Calculating interest for debt {DebtId}. Calculation Date: {CalcDate}, Last Calculation: {LastCalc}",
                debt.Id, normalizedCalculationDate, normalizedLastCalculationDate);

            // If dates are the same or very close (within a second), return 0
            if ((normalizedCalculationDate - normalizedLastCalculationDate).TotalSeconds <= 1)
            {
                return 0;
            }

            // If calculation date is earlier, use the later date to avoid negative interest
            var effectiveCalculationDate = normalizedCalculationDate > normalizedLastCalculationDate
                ? normalizedCalculationDate
                : normalizedLastCalculationDate;

            int daysSinceLastCalculation = (int)(effectiveCalculationDate.Date - normalizedLastCalculationDate.Date).TotalDays;

            if (daysSinceLastCalculation <= 0)
            {
                return 0;
            }

            // For periods other than daily, check if we should calculate
            if (debt.CalculationPeriod != InterestCalculationPeriod.Daily)
            {
                if (!ShouldCalculateForPeriod(debt.CalculationPeriod, normalizedLastCalculationDate, effectiveCalculationDate))
                {
                    return 0;
                }
            }

            decimal interest = debt.InterestType switch
            {
                InterestType.Simple => CalculateSimpleInterest(debt.PrincipalAmount, debt.InterestRate, daysSinceLastCalculation),
                InterestType.Compound => CalculateCompoundInterest(debt.PrincipalAmount, debt.InterestRate, daysSinceLastCalculation),
                _ => throw new ArgumentException($"Unsupported interest type: {debt.InterestType}")
            };

            _logger.LogInformation(
                "Calculated interest for debt {DebtId}: Days={Days}, Rate={Rate}, Interest={Interest}",
                debt.Id, daysSinceLastCalculation, debt.InterestRate, interest);

            return interest;
        }

        private bool ShouldCalculateForPeriod(InterestCalculationPeriod period, DateTime lastCalculation, DateTime currentDate)
        {
            return period switch
            {
                InterestCalculationPeriod.Daily => true,
                InterestCalculationPeriod.Monthly => IsNewMonth(lastCalculation, currentDate),
                InterestCalculationPeriod.Quarterly => IsNewQuarter(lastCalculation, currentDate),
                InterestCalculationPeriod.Annually => IsNewYear(lastCalculation, currentDate),
                _ => throw new ArgumentException($"Unsupported calculation period: {period}")
            };
        }

        private bool IsNewMonth(DateTime lastCalculation, DateTime currentDate)
        {
            return lastCalculation.Month != currentDate.Month || lastCalculation.Year != currentDate.Year;
        }

        private bool IsNewQuarter(DateTime lastCalculation, DateTime currentDate)
        {
            int lastQuarter = (lastCalculation.Month - 1) / 3;
            int currentQuarter = (currentDate.Month - 1) / 3;
            return lastQuarter != currentQuarter || lastCalculation.Year != currentDate.Year;
        }

        private bool IsNewYear(DateTime lastCalculation, DateTime currentDate)
        {
            return lastCalculation.Year != currentDate.Year;
        }

        private decimal CalculateSimpleInterest(decimal principal, decimal rate, int days)
        {
            if (days == 0) return 0;

            decimal dailyInterest = principal * (rate / 100);
            return Math.Round(dailyInterest * days, 2);
        }

        private decimal CalculateCompoundInterest(decimal principal, decimal rate, int days)
        {
            if (days == 0) return 0;

            decimal dailyRate = rate / 100;
            var interest = principal * (decimal)(Math.Pow((double)(1 + dailyRate), days) - 1);
            return Math.Round(interest, 2);
        }
        public decimal CalculateLateFees(Debt debt, DateTime currentDate)
        {
            if (debt == null)
                throw new ArgumentNullException(nameof(debt));

            bool isWithinGracePeriod = currentDate <= debt.DueDate.AddDays(debt.GracePeriodDays);

            _logger.LogInformation(
                "Calculating late fees for debt {DebtId}. Within grace period: {IsWithinGracePeriod}",
                debt.Id, isWithinGracePeriod);

            return isWithinGracePeriod ? 0 : debt.LateFeeAmount;
        }

        public decimal CalculateTotalAmount(Debt debt)
        {
            if (debt == null)
                throw new ArgumentNullException(nameof(debt));

            var total = debt.PrincipalAmount + debt.AccumulatedInterest + debt.AccumulatedLateFees;

            _logger.LogInformation(
                "Calculated total amount for debt {DebtId}: Principal={Principal}, Interest={Interest}, LateFees={LateFees}, Total={Total}",
                debt.Id, debt.PrincipalAmount, debt.AccumulatedInterest, debt.AccumulatedLateFees, total);

            return Math.Round(total, 2);
        }

        private void ValidateInterestCalculationInputs(Debt debt, DateTime calculationDate)
        {
            if (debt == null)
                throw new ArgumentNullException(nameof(debt));

            if (calculationDate < debt.LastInterestCalculationDate)
                throw new ArgumentException("Calculation date cannot be earlier than last calculation date");

            if (debt.InterestRate < 0)
                throw new ArgumentException("Interest rate cannot be negative");
        }
    }
}