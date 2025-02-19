using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{
    public class Notification
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }
        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        public bool IsDeleted { get; set; }
        public string? ActionUrl { get; set; }
        public NotificationType Type { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? RelatedEntityId { get; set; }  // e.g., DebtId
        public string? RelatedEntityType { get; set; }  // e.g., "Debt"

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
        General
    }
}
