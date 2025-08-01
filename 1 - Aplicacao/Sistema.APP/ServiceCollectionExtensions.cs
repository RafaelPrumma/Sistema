using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Sistema.APP.Profiles;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;

namespace Sistema.APP;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
