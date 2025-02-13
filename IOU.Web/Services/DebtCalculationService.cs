using IOU.Web.Models;

namespace IOU.Web.Services
{
    public class DebtCalculationService
    {
        public decimal CalculateInterest(Debt debt, DateTime calculationDate)
        {
            var timePeriod = GetTimePeriodInYears(debt.LastInterestCalculationDate, calculationDate);
            if(debt.InterestType == InterestType.Simple)
            {
                return CalculateSimpleInterest(debt.CurrentBalance, debt.InterestRate, timePeriod);
            }
            else
            {
                return CalculateCompoundInterest(debt.CurrentBalance, debt.InterestRate, timePeriod, debt.CalculationPeriod);
            }
        }
        private decimal CalculateCompoundInterest(decimal principal, decimal annualRate, decimal timeInYears, InterestCalculationPeriod period)
        {
            int n = GetCompoundingFrequency(period);

            // Compound Interest = P(1 + r/n)^(nt) - P
            decimal rate = annualRate / 100;
            double amount = Math.Pow(1+(double)(rate / n), n * (double)timeInYears);
            return principal * (decimal)amount - principal;
        }
        private int GetCompoundingFrequency(InterestCalculationPeriod period)
        {
            return period switch { 
                InterestCalculationPeriod.Daily => 365,
                InterestCalculationPeriod.Monthly => 12,
                InterestCalculationPeriod.Quarterly => 4,
                InterestCalculationPeriod.Annually => 1,
                _ => throw new ArgumentException("Invalid compounding period")
            };
        }

        private decimal CalculateSimpleInterest(decimal principal, decimal annualRate, decimal timeInYears)
        {
            // Simple Interest = P * R * T
            return principal * (annualRate / 100) * timeInYears;
        }
        public decimal CalculateLateFees(Debt debt, DateTime currentDate)
        {
            if (currentDate <= debt.DueDate.AddDays(debt.GracePeriodDays))
                return 0;

            return debt.LateFeeAmount;
        }
        public decimal CalculateTotalAmount(Debt debt)
        {
            return debt.CurrentBalance + debt.AccumulatedInterest + debt.AccumulatedLateFees;
        }
        private decimal GetTimePeriodInYears(DateTime startDate, DateTime endDate)
        {
            return (decimal)(endDate - startDate).TotalDays / 365;
        }
    }
}
