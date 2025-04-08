using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{
    public class CreditReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StudentUserId { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

        // Creditworthiness metrics
        public decimal CreditScore { get; set; } // 0-1000 scale
        public int ActiveDebtsCount { get; set; }
        public decimal TotalDebtObligations { get; set; }
        public decimal PaymentCompletionRate { get; set; } // 0-100%
        public decimal AveragePaymentDelayDays { get; set; }

        // Derived from existing data
        public string RiskCategory { get; set; } // Low/Medium/High risk

        [ForeignKey("StudentUserId")]
        public virtual Student Student { get; set; }
    }
}
