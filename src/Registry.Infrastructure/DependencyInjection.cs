using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Registry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
