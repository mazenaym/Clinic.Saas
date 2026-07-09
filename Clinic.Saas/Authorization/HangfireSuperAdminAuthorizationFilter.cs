using Hangfire.Dashboard;

namespace Clinic.Saas.api.Authorization;

public class HangfireSuperAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("SuperAdmin");
    }
}
