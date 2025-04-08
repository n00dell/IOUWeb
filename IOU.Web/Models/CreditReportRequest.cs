using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{
    public class CreditReportRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string LenderUserId { get; set; }
        public string StudentEmail { get; set; }

        public string StudentUserId { get; set; } 
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? ResponseDate { get; set; }
        public bool? IsApproved { get; set; }
        public string Purpose { get; set; }

        [ForeignKey("LenderUserId")]
        public virtual Lender Lender { get; set; }
        public string? CreditReportId { get; set; } // Add this
        [ForeignKey("CreditReportId")]
        public virtual CreditReport CreditReport { get; set; }
        public virtual Student Student { get; set; }
    }
}
