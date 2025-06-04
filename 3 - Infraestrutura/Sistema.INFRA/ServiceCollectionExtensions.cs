using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;
using Sistema.INFRA.Data.Seeds;
using Sistema.INFRA.Repositories;

namespace Sistema.INFRA;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("SistemaDB"));
        services.AddScoped<IPerfilRepository, PerfilRepository>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        if (!context.Perfis.Any())
        {
            context.Perfis.AddRange(AdminSeed.Get(), UserSeed.Get());
            context.SaveChanges();
        }

        return services;
    }
}
