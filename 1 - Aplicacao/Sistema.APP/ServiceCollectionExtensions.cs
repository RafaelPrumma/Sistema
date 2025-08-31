using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Sistema.APP.Profiles;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;
using Sistema.CORE.Interfaces;

namespace Sistema.APP;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IFuncionalidadeService, FuncionalidadeService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITemaService, TemaService>();
        services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
        services.AddScoped<IMensagemService, MensagemService>();
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
