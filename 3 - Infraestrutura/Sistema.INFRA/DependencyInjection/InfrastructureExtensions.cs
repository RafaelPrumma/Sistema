using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using Sistema.INFRA.Importers;
using Sistema.INFRA.Repositories;
using Sistema.INFRA.Services;

namespace Sistema.INFRA.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

            var useInMemoryDatabase = configuration.GetValue<bool>("UseInMemoryDatabase");
            if (useInMemoryDatabase)
            {
                options.UseInMemoryDatabase("SistemaMockDb");
                return;
            }

            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IFuncionalidadeRepository, FuncionalidadeRepository>();
        services.AddScoped<IPerfilFuncionalidadeRepository, PerfilFuncionalidadeRepository>();
        services.AddScoped<ITemaRepository, TemaRepository>();
        services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
        services.AddScoped<IMensagemRepository, MensagemRepository>();
        services.AddScoped<IMinhasFinancasRepository, MinhasFinancasRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMinhasFinancasImportador, MinhasFinancasImportador>();
        services.AddScoped<IEmailAppService, EmailService>();
        services.Configure<EmailOptions>(configuration.GetSection("AzureAd"));
        services.AddScoped<IExecutionContext, HttpExecutionContext>();

        return services;
    }
}
