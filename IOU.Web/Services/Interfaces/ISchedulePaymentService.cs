using IOU.Web.Models;

namespace IOU.Web.Services.Interfaces
{
    public interface ISchedulePaymentService
    {
        Task<List<ScheduledPayment>> GeneratePaymentScheduleAsync(CreateScheduledPaymentsRequest request);
        Task<ScheduledPayment> GetScheduledPaymentAsync(string id);
        Task<List<ScheduledPayment>> GetPaymentsByDebtIdAsync(string debtId);
        Task<ScheduledPayment> ProcessPaymentAsync(string paymentId, decimal amount, string paymentMethodId);
        Task UpdatePaymentStatusesAsync(string debtId);
        Task RecalculatePaymentScheduleAsync(string debtId);
    }
}
