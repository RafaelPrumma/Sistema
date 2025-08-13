using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;
using Sistema.INFRA.Repositories;
using Sistema.INFRA.Services;

namespace Sistema.INFRA;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("SistemaDB"));
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IFuncionalidadeRepository, FuncionalidadeRepository>();
        services.AddScoped<IPerfilFuncionalidadeRepository, PerfilFuncionalidadeRepository>();
        services.AddScoped<ILayoutRepository, LayoutRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmailService, EmailService>();
 
        return services;
    }
}
