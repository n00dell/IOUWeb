namespace IOU.Web.Models.ViewModels
{
    public class ScheduledPaymentViewModel
    {
        public string Id { get; set; }
        public string DebtId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PrincipalPortion { get; set; }
        public decimal InterestPortion { get; set; }
        public decimal LateFeesPortion { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public ScheduledPaymentStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public bool IsOverdue => DueDate < DateTime.Now && Status != ScheduledPaymentStatus.Paid;
    }
}
