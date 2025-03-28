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

        public bool IsCustomPayment { get; set; } = false;

        // Status
        public ScheduledPaymentStatus Status { get; set; } = ScheduledPaymentStatus.Scheduled;

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
       
    }
    public enum ScheduledPaymentStatus
    {
        Scheduled,
        Paid,
        Overdue
    }

}
