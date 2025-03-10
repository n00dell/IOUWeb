using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class StudentRegisterViewModel: RegisterViewModel
    {
        [Required]
        public string StudentId { get; set; }

        [Required]
        public string University { get; set; }

        [Required]
        [Display(Name = "Expected Graduation Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime ExpectedGraduationDate { get; set; }
    }
}
