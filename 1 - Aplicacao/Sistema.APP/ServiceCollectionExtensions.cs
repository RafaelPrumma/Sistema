using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sistema.APP.Profiles;
using Sistema.APP.Services;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;

namespace Sistema.APP;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<Sistema.CORE.Services.Interfaces.IPerfilDomainService, PerfilService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IUsuarioDomainService, UsuarioService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IFuncionalidadeDomainService, FuncionalidadeService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IAuthDomainService, AuthService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.ITemaDomainService, TemaService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IConfiguracaoDomainService, ConfiguracaoService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IMensagemDomainService, MensagemService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.ILogDomainService, LogService>();

        services.AddScoped<IPerfilAppService, PerfilAppService>();
        services.AddScoped<IUsuarioAppService, UsuarioAppService>();
        services.AddScoped<IFuncionalidadeAppService, FuncionalidadeAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<ITemaAppService, TemaAppService>();
        services.AddScoped<IConfiguracaoAppService, ConfiguracaoAppService>();
        services.AddScoped<IMensagemAppService, MensagemAppService>();
        services.AddScoped<ILogAppService, LogAppService>();
        services.AddScoped<IEmailAppService, EmailAppService>();

        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
