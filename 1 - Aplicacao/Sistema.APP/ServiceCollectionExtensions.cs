using Microsoft.Extensions.DependencyInjection;
using Sistema.APP.Profiles;
using Sistema.CORE.Services;

namespace Sistema.APP;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
