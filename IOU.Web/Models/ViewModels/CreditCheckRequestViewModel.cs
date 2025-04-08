using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class CreditCheckRequestViewModel
    {
        [Required(ErrorMessage = "Student email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Student Email")]
        public string StudentEmail { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        [Display(Name = "Purpose of Check")]
        public string SelectedPurpose { get; set; }
        [BindNever]
        public List<SelectListItem>? PurposeOptions { get; set; }

        [Required(ErrorMessage = "You must confirm compliance")]
        [Display(Name = "I confirm this request complies with financial regulations")]
        public bool RegulatoryCompliance { get; set; }
    }
}
