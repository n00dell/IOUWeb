using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace IOU.Web.Models
{
    //Lender evidence for a debt dispute
    public class DebtEvidence
    {
        
        [Key]
        public string EvidenceId { get; set; }

        [Required]
        public string DisputeId { get; set; }

        [Required]
        public string LenderUserId { get; set; } //Lender User ID

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(255)]
        public string FilePath { get; set; }

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; }

        [Required]
        public DateTime UploadDate { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Navigation properties
        [ForeignKey("DisputeId")]
        public virtual Dispute Dispute { get; set; }

        [ForeignKey("LenderUserId")]
        public virtual Lender Lender { get; set; } //Lender
    }
}
