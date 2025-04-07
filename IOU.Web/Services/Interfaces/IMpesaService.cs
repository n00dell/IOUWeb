using static IOU.Web.Models.MpesaModels;

namespace IOU.Web.Services.Interfaces
{
    public interface IMpesaService
    {
        Task<MpesaResponse> InitiateStkPushAsync(PaymentRequest request);
        Task<string> GetAuthTokenAsync();
        Task<bool> VerifyCallbackSignature(HttpRequest request);

    }
}
