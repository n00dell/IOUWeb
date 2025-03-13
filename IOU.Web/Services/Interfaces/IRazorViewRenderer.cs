namespace IOU.Web.Services.Interfaces
{
    public interface IRazorViewRenderer
    {
        Task<string> RenderViewToStringAsync(string viewName, object model);
    }
}
