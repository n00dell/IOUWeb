using IOU.Web.Data;
using IOU.Web.Models;

namespace IOU.Web.Services
{
    public class DebtService
    {
        private readonly IOUWebContext _context;
        private readonly DebtCalculationService _calculationService;

        public DebtService(IOUWebContext context, DebtCalculationService calculationService)
        {
            _context = context;
            _calculationService = calculationService;
        }
        public async Task UpdateDebtCalculations(string debtId)
        {
            var debt = await _context.Debt.FindAsync(debtId);
            if (debt == null)
                throw new ArgumentException("Debt not found");
            var currentDate = DateTime.Now;
            // Calculate new interest
            var newInterest = _calculationService.CalculateInterest(debt, currentDate);
            debt.AccumulatedInterest += newInterest;
            debt.LastInterestCalculationDate = currentDate;

            // Calculate late fees if applicable
            var lateFees = _calculationService.CalculateLateFees(debt, currentDate);
            debt.AccumulatedLateFees += lateFees;

            // Update total balance
            debt.CurrentBalance = _calculationService.CalculateTotalAmount(debt);
            // Update status if needed
            UpdateDebtStatus(debt, currentDate);

            await _context.SaveChangesAsync();
        }
        private void UpdateDebtStatus(Debt debt, DateTime currentDate)
        {
            if (debt.CurrentBalance == 0)
            {
                debt.Status = DebtStatus.Paid;
            }
            else if (currentDate > debt.DueDate.AddDays(debt.GracePeriodDays))
            {
                debt.Status = DebtStatus.Overdue;
            }
        }
    }
}
