using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sistema.CORE.Enums;
using System.Globalization;
using System.Security.Claims;

namespace Sistema.MVC.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AuthorizePermissionAttribute(string funcionalidade, Permissao permissaoRequerida) : Attribute, IAuthorizationFilter
{
    private readonly string _funcionalidade = funcionalidade;
    private readonly Permissao _permissaoRequerida = permissaoRequerida;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ForbidResult();
            return;
        }

        var claimValue = user.FindFirstValue($"perm:{_funcionalidade}");
        if (!int.TryParse(claimValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var claimInt))
        {
            context.Result = new ForbidResult();
            return;
        }

        var permissaoUsuario = (Permissao)claimInt;
        if (!permissaoUsuario.HasFlag(_permissaoRequerida))
        {
            context.Result = new ForbidResult();
        }
    }
}
