namespace IOU.Web.Models.ViewModels
{
    public class DisputeNotificationViewModel
    {
        public string NotificationId { get; set; }
        public string DisputeId { get; set; }
        public string DebtId { get; set; }
        public DisputeStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public string ActionUrl { get; set; }
    }
}
