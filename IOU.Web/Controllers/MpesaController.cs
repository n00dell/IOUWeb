using IOU.Web.Data;
using IOU.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace IOU.Web.Controllers
{
    [ApiController]
    [Route("api/mpesa")]
    public class MpesaController : Controller
    {
        private readonly IOUWebContext _context;

        public MpesaController(IOUWebContext context)
        {
            _context = context;
        }

        [HttpPost("callback")]
        public async Task<IActionResult> MpesaCallback([FromBody] MpesaCallbackResponse callbackData)
        {
            if (callbackData?.Body?.StkCallback == null)
            {
                return BadRequest("Invalid callback data.");
            }

            var transactionStatus = callbackData.Body.StkCallback.ResultCode;
            var transactionId = callbackData.Body.StkCallback.CheckoutRequestID;
            var mpesaReceiptNumber = callbackData.Body.StkCallback.CallbackMetadata?.Item
                ?.FirstOrDefault(i => i.Name == "MpesaReceiptNumber")?.Value?.ToString();

            if (string.IsNullOrEmpty(mpesaReceiptNumber))
            {
                return BadRequest("Mpesa receipt number not found.");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.MpesaTransactionId == transactionId);

            if (payment != null)
            {
                payment.MpesaReceiptNumber = mpesaReceiptNumber;
                payment.PaymentDate = DateTime.UtcNow;

                if (transactionStatus == "0")
                {
                    payment.Status = PaymentStatus.Paid;
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                }

                await _context.SaveChangesAsync();
            }

            return Ok();
        }


    }
}
