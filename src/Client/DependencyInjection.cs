using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;
using Registry.Client.Accounts;

namespace Registry.Client;

public static class DependencyInjection
{
    /// <summary>Registers typed Refit clients for the Registry API at <paramref name="apiBaseAddress"/>.</summary>
    public static IServiceCollection AddRegistryClient(this IServiceCollection services, Uri apiBaseAddress)
    {
        services.TryAddSingleton<IAccessTokenStore, InMemoryAccessTokenStore>();

        RefitSettings Settings(IServiceProvider sp) => new()
        {
            AuthorizationHeaderValueGetter = async (_, cancellationToken) =>
                await sp.GetRequiredService<IAccessTokenStore>().GetAccessTokenAsync(cancellationToken) ?? string.Empty,
        };

        services.AddRefitClient<IAuthApi>(Settings).ConfigureHttpClient(c => c.BaseAddress = apiBaseAddress);
        services.AddRefitClient<IAccountApi>(Settings).ConfigureHttpClient(c => c.BaseAddress = apiBaseAddress);

        return services;
    }
}
