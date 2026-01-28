using Hangfire.Dashboard;

namespace BlazorDematReports.Application
{
    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity!.IsAuthenticated;
        }
    }
}
