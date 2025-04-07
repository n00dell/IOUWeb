using IOU.Web.Models;

namespace IOU.Web.Services.Interfaces
{
    public interface IScheduledPaymentService
    {
        Task<List<ScheduledPayment>> GeneratePaymentScheduleAsync(CreateScheduledPaymentsRequest request);
        Task<ScheduledPayment> GetScheduledPaymentAsync(string id);
        Task<List<ScheduledPayment>> GetPaymentsByDebtIdAsync(string debtId);
        Task<ScheduledPayment> ProcessPaymentAsync(string paymentId, decimal amount, string paymentMethodId);
        Task UpdatePaymentStatusesAsync(string debtId);
        Task RecalculatePaymentScheduleAsync(string debtId);
        Task ProcessPaymentAgainstSchedule(Payment payment);
        (decimal principal, decimal interest, decimal lateFees) CalculatePaymentPortions(Debt debt, decimal paymentAmount);
    }
}
