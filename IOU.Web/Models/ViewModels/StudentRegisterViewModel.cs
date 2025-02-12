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
        [DataType(DataType.Date)]
        public DateTime ExpectedGraduationDate { get; set; }
    }
}
