using System.Security.Claims;
using Sistema.APP.Services.Interfaces;

namespace Sistema.MVC.Services;

public sealed class HttpExecutionContext(IHttpContextAccessor httpContextAccessor) : IExecutionContext
{
    public string? Usuario =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContextAccessor.HttpContext?.User?.Identity?.Name
        ?? httpContextAccessor.HttpContext?.Session.GetString("UserName");

    public string? CorrelationId => httpContextAccessor.HttpContext?.TraceIdentifier;
}
