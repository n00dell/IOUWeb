using IOU.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Payment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ForeignKey("ScheduledPaymentId")]
    public string? ScheduledPaymentId { get; set; } // Nullable for custom payments

    [Required]
    [ForeignKey("Debt")]
    public string DebtId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(15)]
    public string PhoneNumber { get; set; }

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    [Required]
    [StringLength(50)]
    public string CheckoutRequestID { get; set; }

    [StringLength(20)]
    public string? MpesaReceiptNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaymentDate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? ResultDescription { get; set; }

    // Navigation properties
    public ScheduledPayment? ScheduledPayment { get; set; }
    public Debt Debt { get; set; }
}

public enum PaymentTransactionStatus
{
    Pending,
    Paid,
    Failed
}