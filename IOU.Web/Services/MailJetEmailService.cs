using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
namespace IOU.Web.Services
{
    public class MailJetEmailService: IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IRazorViewRenderer _razorViewRenderer;

        public MailJetEmailService(IConfiguration config, IRazorViewRenderer razorViewRenderer)
        {
            _config = config;
            _razorViewRenderer = razorViewRenderer;

        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string htmlContent)
        {
            var client = new MailjetClient(
                _config["Mailjet:ApiKey"],
                _config["Mailjet:SecretKey"]
            );

            var request = new MailjetRequest
            {
                Resource = Send.Resource,
            }
            .Property(Send.FromEmail, _config["Mailjet:FromEmail"])
            .Property(Send.FromName, _config["Mailjet:DisplayName"])
            .Property(Send.Subject, subject)
            .Property(Send.HtmlPart, htmlContent)
            .Property(Send.Recipients, new JArray {
                new JObject {
                    {"Email", recipientEmail}
                }
            });
            

            await client.PostAsync(request);
        }
    }
}
