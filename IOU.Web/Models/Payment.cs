using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models
{
    public class Payment
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Relationships
        public string? ScheduledPaymentId { get; set; } // Nullable for custom payments
        public string DebtId { get; set; }

        // Payment Details
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }

        // MPESA Fields
        public string? MpesaTransactionId { get; set; }
        public string? MpesaReceiptNumber { get; set; }
        public string PhoneNumber { get; set; }
        // Payment Status
        public PaymentStatus Status { get; set; } // Add this line
                                                  // Add these fields
        public string CheckoutRequestID { get; set; }
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? ResultCode { get; set; }
        public string? ResultDescription { get; set; }


        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation Properties
        [ForeignKey("ScheduledPaymentId")]
        public ScheduledPayment? ScheduledPayment { get; set; } // Optional (for installments)
        [ForeignKey("DebtId")]
        public Debt Debt { get; set; }
    }

}
