using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOU.Web.Models
{
    public class Student
    {
        [Key]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public string StudentId { get; set; }
        [Required]
        public string University { get; set; }

        public DateTime ExpectedGraduationDate { get; set; }

        public List<StudentGuardian> StudentGuardians { get; set; }

        public virtual List<Debt> Debts { get; set; }
    }
}
