using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;
using Registry.Client.Accounts;
using Registry.Client.Catalogs;
using Registry.Client.Groups;

namespace Registry.Client;

public static class DependencyInjection
{
    /// <summary>
    /// Registers typed Refit clients for the Registry API at <paramref name="apiBaseAddress"/>.
    /// Source-generated, because Blazor WebAssembly and iOS cannot emit Refit's reflection-based clients.
    /// </summary>
    public static IServiceCollection AddRegistryClient(this IServiceCollection services, Uri apiBaseAddress)
    {
        services.TryAddSingleton<IAccessTokenStore, InMemoryAccessTokenStore>();

        RefitSettings Settings(IServiceProvider sp) => new()
        {
            AuthorizationHeaderValueGetter = async (_, cancellationToken) =>
                await sp.GetRequiredService<IAccessTokenStore>().GetAccessTokenAsync(cancellationToken) ?? string.Empty,
        };

        services.AddRefitGeneratedClient<IAuthApi>(Settings).ConfigureHttpClient(c => c.BaseAddress = apiBaseAddress);
        services.AddRefitGeneratedClient<IAccountApi>(Settings).ConfigureHttpClient(c => c.BaseAddress = apiBaseAddress);
        services.AddRefitGeneratedClient<IGroupsApi>(Settings).ConfigureHttpClient(c => c.BaseAddress = apiBaseAddress);
        services.AddRefitGeneratedClient<ICatalogsApi>(Settings).ConfigureHttpClient(c => c.BaseAddress = apiBaseAddress);

        return services;
    }
}
