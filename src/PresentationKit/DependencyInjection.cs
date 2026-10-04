using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Registry.Client;
using Registry.PresentationKit.Auth;
using Registry.PresentationKit.Components;

namespace Registry.PresentationKit;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the shared screens' services and the API clients. Hosts may register their own
    /// <see cref="ISessionStorage"/> and <see cref="IExternalSignIn"/> before calling this.
    /// </summary>
    public static IServiceCollection AddPresentationKit(this IServiceCollection services, Uri apiBaseAddress)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISessionStorage, InMemorySessionStorage>();
        services.TryAddSingleton<IExternalSignIn, NoExternalSignIn>();
        services.AddSingleton<AuthSession>();
        services.AddSingleton<IAccessTokenStore>(sp => sp.GetRequiredService<AuthSession>());
        services.AddScoped<AuthenticationStateProvider, SessionAuthenticationStateProvider>();
        services.AddScoped<NavigationState>();
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();

        return services.AddRegistryClient(apiBaseAddress);
    }
}
