using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.APP.Services.Interfaces;
using Sistema.INFRA.Data;
using Sistema.INFRA.Repositories;
using Sistema.INFRA.Services;

namespace Sistema.INFRA;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IFuncionalidadeRepository, FuncionalidadeRepository>();
        services.AddScoped<IPerfilFuncionalidadeRepository, PerfilFuncionalidadeRepository>();
        services.AddScoped<ITemaRepository, TemaRepository>();
        services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
        services.AddScoped<IMensagemRepository, MensagemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmailAppService, EmailService>();
        services.Configure<EmailOptions>(configuration.GetSection("AzureAd"));

        return services;
    }
}
