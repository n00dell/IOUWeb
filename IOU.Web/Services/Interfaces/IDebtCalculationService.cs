using IOU.Web.Models;

namespace IOU.Web.Services.Interfaces
{
    public interface IDebtCalculationService
    {
        decimal CalculateInterest(Debt debt, DateTime calculationDate);
        decimal CalculateLateFees(Debt debt, DateTime currentDate);
        decimal CalculateTotalAmount(Debt debt);
    }
}
