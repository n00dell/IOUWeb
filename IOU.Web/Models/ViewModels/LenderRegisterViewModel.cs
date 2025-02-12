using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class LenderRegisterViewModel : RegisterViewModel
    {
        [Required]
        public string CompanyName { get; set; }
        [Required]
        public string BusinessRegistrationNumber { get; set; }
        
    }
}
