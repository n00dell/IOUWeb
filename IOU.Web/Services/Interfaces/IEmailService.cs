namespace IOU.Web.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipientEmail, string subject, string htmlContent);
    }
}
