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
        services.AddScoped<Sistema.CORE.Services.Interfaces.IPerfilService, PerfilService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IUsuarioService, UsuarioService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IFuncionalidadeService, FuncionalidadeService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IAuthService, AuthService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.ITemaService, TemaService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IConfiguracaoService, ConfiguracaoService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.IMensagemService, MensagemService>();
        services.AddScoped<Sistema.CORE.Services.Interfaces.ILogService, LogService>();

        services.AddScoped<IPerfilService, PerfilAppService>();
        services.AddScoped<IUsuarioService, UsuarioAppService>();
        services.AddScoped<IFuncionalidadeService, FuncionalidadeAppService>();
        services.AddScoped<IAuthService, AuthAppService>();
        services.AddScoped<ITemaService, TemaAppService>();
        services.AddScoped<IConfiguracaoService, ConfiguracaoAppService>();
        services.AddScoped<IMensagemService, MensagemAppService>();
        services.AddScoped<ILogService, LogAppService>();
        services.AddScoped<IEmailService, EmailAppService>();

        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
