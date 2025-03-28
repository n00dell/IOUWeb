namespace IOU.Web.Models
{
    public class CreateScheduledPaymentsRequest
    {
        public string DebtId { get; set; }
        public int? NumberOfPayments { get; set; }
        public DateTime FirstPaymentDate { get; set; }
        public PaymentFrequency Frequency { get; set; }
        public bool IncludeInterestInCalculation { get; set; } = true;
    }
    public enum PaymentFrequency
    {
        Weekly,
        Biweekly,
        Monthly,
        Quarterly,
        Annually,
        SemiAnnually
    }
}
