using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sistema.APP.Profiles;
using Sistema.APP.Services;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;

namespace Sistema.APP;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IPerfilAppService, PerfilAppService>();
        services.AddScoped<IUsuarioAppService, UsuarioAppService>();
        services.AddScoped<IFuncionalidadeAppService, FuncionalidadeAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<ITemaAppService, TemaAppService>();
        services.AddScoped<IConfiguracaoAppService, ConfiguracaoAppService>();
        services.AddScoped<IMensagemAppService, MensagemAppService>();
        services.AddScoped<ILogAppService, LogAppService>();

        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
