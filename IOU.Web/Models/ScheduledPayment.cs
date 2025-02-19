namespace IOU.Web.Models
{
    public class ScheduledPayment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Relationships
        public string DebtId { get; set; }
        public Debt Debt { get; set; }

        // Payment Details
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal PrincipalPortion { get; set; }
        public decimal InterestPortion { get; set; }
        public decimal LateFeesPortion { get; set; }

        // Status
        public PaymentStatus Status { get; set; }

        // Tracking
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Payment Method (if paid)
        public string? PaymentMethodId { get; set; }
        public string? TransactionReference { get; set; }
    }

    public enum PaymentStatus
    {
        Scheduled,
        Pending,
        Paid,
        Overdue,
        Cancelled
    }
}
