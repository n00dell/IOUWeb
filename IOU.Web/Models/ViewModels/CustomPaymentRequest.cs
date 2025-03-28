using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class CustomPaymentRequest
    {
        [Required(ErrorMessage = "Debt ID is required")]
        public string DebtId { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(0|254)?[7][0-9]{8}$", ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }
        public string? MpesaTransactionId { get; set; }
        public string? MpesaReceiptNumber { get; set; }
    }
}
