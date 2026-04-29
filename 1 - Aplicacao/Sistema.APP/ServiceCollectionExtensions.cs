using Microsoft.Extensions.DependencyInjection;
using Sistema.APP.DependencyInjection;

namespace Sistema.APP;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
        => services.AddApplication();
}
