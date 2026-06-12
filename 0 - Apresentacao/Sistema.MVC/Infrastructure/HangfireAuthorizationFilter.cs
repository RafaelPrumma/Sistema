using Hangfire.Dashboard;

namespace Sistema.MVC.Infrastructure;

// Protege o dashboard do Hangfire (/jobs): só usuários autenticados podem ver.
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User?.Identity?.IsAuthenticated == true;
    }
}
