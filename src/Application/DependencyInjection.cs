using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Catalogs;
using Registry.Application.Groups;

namespace Registry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<GroupService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<ItemService>();
        services.AddScoped<PublisherService>();
        return services;
    }
}
