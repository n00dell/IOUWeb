using IOU.Web.Models;
namespace IOU.Web.Services.Interfaces
{
    public interface ICreditReportService
    {
        Task<CreditReport> GenerateCreditReport(string studentUserId);
    }
}
