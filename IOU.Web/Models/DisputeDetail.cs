using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models
{
    public class DisputeDetail
    {

        [Key]
        [ForeignKey("DisputeId")]
        public string DisputeId { get; set; }

        [Required]
        public DisputeReason Reason { get; set; }

        [StringLength(500)]
        public string OtherReasonDetail { get; set; }

        [Required]
        [StringLength(2000)]
        public string DisputeExplanation { get; set; }

        [Required]
        public ResolutionType RequestedResolution { get; set; }

        [StringLength(500)]
        public string OtherResolutionDetail { get; set; }

        [Range(0, 100000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RequestedReductionAmount { get; set; }

        [Required]
        public bool DeclarationConfirmed { get; set; }

        [Required]
        public string DigitalSignature { get; set; }

        [Required]
        public DateTime SignatureDate { get; set; }

        // Navigation property
        
        public virtual Dispute Dispute { get; set; }
    }
}
