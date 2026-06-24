using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sistema.APP.Profiles;
using Sistema.APP.Services;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;

namespace Sistema.APP.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPerfilAppService, PerfilAppService>();
        services.AddScoped<IUsuarioAppService, UsuarioAppService>();
        services.AddScoped<IFuncionalidadeAppService, FuncionalidadeAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<ITemaAppService, TemaAppService>();
        services.AddScoped<IConfiguracaoAppService, ConfiguracaoAppService>();
        services.AddScoped<IMensagemAppService, MensagemAppService>();
        services.AddScoped<ILogAppService, LogAppService>();
        services.AddScoped<IFinancasAppService, FinancasAppService>();
        services.AddScoped<IPosicaoAtivoProjectionService, PosicaoAtivoProjectionService>();

        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        return services;
    }
}
