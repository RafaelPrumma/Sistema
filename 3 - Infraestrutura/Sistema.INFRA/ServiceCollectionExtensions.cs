using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sistema.INFRA.DependencyInjection;

namespace Sistema.INFRA;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
        => services.AddInfrastructure(configuration);
}
