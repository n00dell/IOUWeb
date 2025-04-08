using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{
    [Index(nameof(UserId))]
    [Index(nameof(IsRead))]
    [Index(nameof(CreatedAt))]
    public class Notification
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        [MaxLength(100)]
        [MinLength(5)]
        public string Title { get; set; }
        [Required]
        [MaxLength(500)]
        [MinLength(10)]
        public string Message { get; set; }

        public bool IsDeleted { get; set; } = false;
        [Url]
        public string? ActionUrl { get; set; }
        public NotificationType Type { get; set; }
        
        public DateTime CreatedAt { get; set; } = System.DateTime.Now;
        public bool IsRead { get; set; } = false;
        public string? RelatedEntityId { get; set; }  // e.g., DebtId
        public RelatedEntityType? RelatedEntityType { get; set; }  // e.g., "Debt"

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        
    }
    public enum NotificationType
    {
        DebtCreated,
        PaymentDue,
        PaymentReceived,
        PaymentOverdue,
        DebtApproved,
        DebtRejected,
        InterestAccrued,
        LateFeeAdded,
        EvidenceSubmitted,
        General,
        DisputeCreated,
        DisputeResolved,
        LenderApproved, 
        LenderRejected, 
        UserCreated,    
        PaymentMade,
        CreditCheckRequest,
        CreditCheckApproved,
        CreditCheckDenied
    }
    public enum RelatedEntityType
    {
        Debt,
        Dispute,
        Payment,
        Other,
        ReportRequest,
        CreditCheck
    }
}
