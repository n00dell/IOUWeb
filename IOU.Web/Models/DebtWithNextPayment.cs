namespace IOU.Web.Models
{
    public class DebtWithNextPayment
    {
        public Debt Debt { get; set; }
        public ScheduledPayment NextPayment { get; set; }
    }
}
