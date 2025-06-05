using Microsoft.Extensions.DependencyInjection;
using Sistema.APP.Profiles;
using Sistema.CORE.Services;
using Sistema.CORE.Entities;
using Microsoft.AspNetCore.Identity;

namespace Sistema.APP;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IFuncionalidadeService, FuncionalidadeService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
