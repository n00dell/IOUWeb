using static IOU.Web.Models.MpesaModels;
using IOU.Web.Models;
namespace IOU.Web.Services.Interfaces
{
    public interface IMpesaService
    {
        Task<MpesaInitiateResponse> InitiateStkPushAsync(PaymentRequest request);
        Task<bool> VerifyCallbackSignature(string signature, string body);

    }
}
