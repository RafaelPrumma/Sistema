using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Sistema.MVC.DependencyInjection;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
            });

        services.AddAuthorization();

        return services;
    }
}
