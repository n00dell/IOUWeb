using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models
{
    public class SupportingDocument
    {
        [Key]
        public string DocumentId { get; set; }

        [Required]
        public string DisputeId { get; set; }

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

        [StringLength(255)]
        public string DocumentType { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Navigation property
        [ForeignKey("DisputeId")]
        public virtual Dispute Dispute { get; set; }
    }
}
