using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Services
{
    public class CreditReportService : ICreditReportService
    {
        private readonly IOUWebContext _context;

        public CreditReportService(IOUWebContext context)
        {
            _context = context;
        }

        public async Task<CreditReport> GenerateCreditReport(string studentUserId)
        {
            var student = await _context.Student
                .Include(s => s.Debts)
                    .ThenInclude(d => d.ScheduledPayments)
                .FirstOrDefaultAsync(s => s.UserId == studentUserId);

            if (student == null) return null;

            var report = new CreditReport
            {
                StudentUserId = studentUserId,
                GeneratedDate = DateTime.UtcNow,
                ActiveDebtsCount = student.Debts.Count(d => d.Status == DebtStatus.Active),
                TotalDebtObligations = student.Debts.Sum(d => d.CurrentBalance)
            };

            // Get all completed payments (both scheduled and custom)
            var allCompletedPayments = student.Debts
                .SelectMany(d => d.ScheduledPayments)
                .Where(p => p.Status == ScheduledPaymentStatus.Paid)
                .ToList();

            // Calculate payment metrics
            var totalPayments = allCompletedPayments.Count;
            var onTimePayments = allCompletedPayments.Count(p =>
                p.PaymentDate <= p.DueDate && !p.IsLate);

            report.PaymentCompletionRate = totalPayments > 0 ?
                (decimal)onTimePayments / totalPayments * 100 : 100;

            // Calculate average delay using DaysLate property
            var latePayments = allCompletedPayments
                .Where(p => p.DaysLate > 0)
                .ToList();

            report.AveragePaymentDelayDays = latePayments.Any() ?
                (decimal)latePayments.Average(p => p.DaysLate) : 0;

            // Calculate credit score
            report.CreditScore = CalculateCreditScore(report);

            report.RiskCategory = report.CreditScore switch
            {
                >= 800 => "Low Risk",
                >= 600 => "Medium Risk",
                _ => "High Risk"
            };

            return report;
        }

        private decimal CalculateCreditScore(CreditReport report)
        {
            // Simplified scoring algorithm - replace with real logic
            decimal score = 700; // Base score

            // Payment history impact
            score += (report.PaymentCompletionRate - 90) * 2;

            // Debt burden impact
            score -= report.TotalDebtObligations / 1000;

            // Credit mix impact
            score += report.ActiveDebtsCount * 5;

            // Recent credit impact
            score -= report.AveragePaymentDelayDays * 2;

            return Math.Clamp(score, 300, 850);
        }
    }
}
