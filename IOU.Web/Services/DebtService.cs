using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;

namespace IOU.Web.Services
{
    public class DebtService : IDebtService
    {
        private readonly IOUWebContext _context;
        private readonly IDebtCalculationService _calculationService;
        private readonly ILogger<DebtService> _logger;

        public DebtService(
            IOUWebContext context,
            IDebtCalculationService calculationService,
            ILogger<DebtService> logger)
        {
            _context = context;
            _calculationService = calculationService;
            _logger = logger;
        }

        public async Task UpdateDebtCalculations(string debtId)
        {
            var debt = await _context.Debt.FindAsync(debtId);
            if (debt == null)
                throw new ArgumentException("Debt not found");

            var currentDate = DateTime.UtcNow;

            _logger.LogInformation(
        "UpdateDebtCalculations - Debt {DebtId}: Current UTC: {CurrentDate}, Last Calculation: {LastCalc}",
        debtId, currentDate, debt.LastInterestCalculationDate);

            _logger.LogInformation(
                "Starting debt calculation. Debt ID={Id}, Principal={Principal}, LastCalcDate={LastCalcDate}, " +
                "InterestRate={Rate}, Period={Period}, Type={Type}, Current Balance={Balance}",
                debt.Id,
                debt.PrincipalAmount,
                debt.LastInterestCalculationDate,
                debt.InterestRate,
                debt.CalculationPeriod,
                debt.InterestType,
                debt.CurrentBalance);

            try
            {
                // Calculate new interest
                var newInterest = _calculationService.CalculateInterest(debt, currentDate);

                if (newInterest > 0)
                {
                    // Add new interest to accumulated interest
                    debt.AccumulatedInterest += newInterest;
                    debt.LastInterestCalculationDate = currentDate;

                    _logger.LogInformation(
                        "Added interest for debt {DebtId}: New Interest={NewInterest}, Total Accumulated={TotalInterest}",
                        debtId, newInterest, debt.AccumulatedInterest);
                }

                // Calculate late fees if applicable
                var newLateFees = _calculationService.CalculateLateFees(debt, currentDate);
                if (newLateFees > 0 && debt.AccumulatedLateFees == 0)
                {
                    debt.AccumulatedLateFees = newLateFees;
                }

                // Update current balance
                debt.CurrentBalance = _calculationService.CalculateTotalAmount(debt);

                // Update status
                UpdateDebtStatus(debt, currentDate);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating debt calculations for debt {DebtId}", debtId);
                throw;
            }
        }

        private void UpdateDebtStatus(Debt debt, DateTime currentDate)
        {
            if (debt.CurrentBalance <= 0)
            {
                debt.Status = DebtStatus.Paid;
            }
            else if (currentDate > debt.DueDate.AddDays(debt.GracePeriodDays))
            {
                debt.Status = DebtStatus.Overdue;
            }
            else if (debt.Status == DebtStatus.Pending)
            {
                debt.Status = DebtStatus.Active;
            }
        }
    }
}
