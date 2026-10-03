using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Groups;

namespace Registry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<GroupService>();
        return services;
    }
}
