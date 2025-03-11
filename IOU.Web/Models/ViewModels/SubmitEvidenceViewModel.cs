using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class SubmitEvidenceViewModel
    {
        public string DisputeId { get; set; }
        public string DebtId { get; set; }
        public string StudentName { get; set; }
        public DisputeReason Reason { get; set; }
        public string ReasonDisplay => GetEnumDisplayName(Reason);
        public string DisputeExplanation { get; set; }

        [Required]
        [Display(Name = "Evidence Files")]
        public List<IFormFile> EvidenceFiles { get; set; }

        [Required]
        [Display(Name = "Description of Evidence")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters.")]
        public List<string> EvidenceDescriptions { get; set; }

        // Helper method to get display name
        private string GetEnumDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DisplayAttribute), false);

            return attributes.Length > 0
                ? ((DisplayAttribute)attributes[0]).Name
                : value.ToString();
        }
    }
}
