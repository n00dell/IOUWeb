using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models.ViewModels
{
    public class DisputeViewModel
    {
        public string DisputeId { get; set; }
        public string StudentUserId { get; set; } // Added
        public string LenderUserId { get; set; } // Added
        public string DebtId { get; set; }
        public string DebtorName { get; set; } // Student name
        public string LenderName { get; set; }
        public decimal DebtAmount { get; set; }
        public DisputeStatus Status { get; set; }
        public string StatusString => Status.ToString();
        public DateTime CreatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DisputeReason Reason { get; set; }
        public string ReasonDisplay => GetDisplayName(Reason);
        public ResolutionType RequestedResolution { get; set; }
        public string ResolutionDisplay => GetDisplayName(RequestedResolution);

        // Helper method to get the display name from enums
        private string GetDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DisplayAttribute), false);

            return attributes.Length > 0
                ? ((DisplayAttribute)attributes[0]).Name
                : value.ToString();
        }
    }
}
