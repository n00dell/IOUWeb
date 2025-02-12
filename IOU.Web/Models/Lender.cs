using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{
    public class Lender
    {
        [Key]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public string CompanyName { get; set; }

        [Required]
        public string BusinessRegistrationNumber { get; set; }

        public virtual List<Debt> IssuedDebts { get; set; }
    }
}
