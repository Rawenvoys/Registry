using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using Registry.Client.Accounts;

namespace Registry.PresentationKit.Auth;

/// <summary>Tells Blazor who is signed in, based on <see cref="AuthSession"/> and <c>GET /account/me</c>.</summary>
internal sealed class SessionAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly AuthSession _session;
    private readonly IAccountApi _accountApi;

    public SessionAuthenticationStateProvider(AuthSession session, IAccountApi accountApi)
    {
        _session = session;
        _accountApi = accountApi;
        _session.Changed += OnSessionChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (await _session.GetAccessTokenAsync(CancellationToken.None) is null)
        {
            return Anonymous;
        }

        try
        {
            var account = await _accountApi.GetMeAsync();
            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Name, account.DisplayName ?? account.Email ?? string.Empty),
            ];
            if (account.Email is not null)
            {
                claims.Add(new Claim(ClaimTypes.Email, account.Email));
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "Registry")));
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _session.SignOutAsync();
            return Anonymous;
        }
    }

    private void OnSessionChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() => _session.Changed -= OnSessionChanged;
}
