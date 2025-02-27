using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{
    public class Debt
    {
        [Key]
        public string Id { get; set; }
        [Required]
        public string LenderId { get; set; }
        [Required]
        public string StudentId { get; set; }

        // Basic loan details
        [Required]
        [Range(1, double.MaxValue)]
        public decimal PrincipalAmount { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Purpose { get; set; }
        public DebtType DebtType { get; set; }

        // Dates
        public DateTime DateIssued { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastInterestCalculationDate { get; set; }

        // Interest
        [Range(0, 100)]
        public decimal InterestRate { get; set; } // Annual interest rate
        public decimal AccumulatedInterest { get; set; }
        public InterestType InterestType { get; set; }
        public InterestCalculationPeriod CalculationPeriod { get; set; }

        // Late Fees
        public decimal LateFeeAmount { get; set; }
        public int GracePeriodDays { get; set; }

        public decimal AccumulatedLateFees { get; set; }

        // Status
        public DebtStatus Status { get; set; }

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }
        [ForeignKey("LenderId")]
        public virtual Lender Lender { get; set; }
    }

    public enum DebtStatus
    {
        Pending,
        Active,
        Overdue,
        Paid,
        Declined,
        PendingChanges
    }
    public enum DebtType
    {
        StudentLoan,         // Traditional student loan
        EmergencyLoan,      // Short-term emergency funding
        TuitionFee,         // Specific for tuition payments
        EducationalSupplies, // For books, equipment, etc.
        AccommodationLoan,   // For housing/accommodation expenses
        StudyAbroadLoan     // For international study programs
    }

    public enum InterestType
    {
        Simple,             // Simple interest calculation
        Compound            // Compound interest calculation
    }

    public enum InterestCalculationPeriod
    {
        Daily,
        Monthly,
        Quarterly,
        Annually
    }
}
