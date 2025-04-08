using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class CreditReportViewModel
    {
        public string StudentName { get; set; }
        public decimal CreditScore { get; set; }
        public string RiskCategory { get; set; }
        public int ActiveDebts { get; set; }
        public decimal TotalObligations { get; set; }
        public decimal PaymentCompletionRate { get; set; }
        public decimal AverageDelayDays { get; set; }
        public DateTime GeneratedDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal RecommendedLimit { get; set; }

        public string RiskExplanation { get; set; }

        // Add these properties for detailed debt information
        public List<DebtItem> Debts { get; set; } = new();
        public PaymentHistorySummary PaymentHistory { get; set; }

        public class DebtItem
        {
            public string DebtType { get; set; }
            public decimal PrincipalAmount { get; set; }
            public decimal CurrentBalance { get; set; }
            public decimal InterestRate { get; set; }
            public DateTime DueDate { get; set; }
            public string Status { get; set; }
        }

        public class PaymentHistorySummary
        {
            public int TotalPayments { get; set; }
            public int OnTimePayments { get; set; }
            public int LatePayments { get; set; }
            public decimal AverageDaysLate { get; set; }
        }
    }
}
