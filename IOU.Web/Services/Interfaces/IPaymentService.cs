namespace IOU.Web.Services.Interfaces
{
    public interface IPaymentService
    {
        Task ProcessPaymentAsync(Payment payment);
        Task AllocateToInstallmentsAsync(Payment payment);
    }
}
