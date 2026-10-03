using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Registry.PresentationKit.Auth;

namespace Registry.WebApp;

/// <summary>
/// Signs in with Google, Microsoft or Facebook in a popup (OAuth implicit flow) and returns the provider token
/// that the API verifies: an ID token from Google and Microsoft, an access token from Facebook.
/// The popup lands on signin-callback.html, which hands the result back over a BroadcastChannel.
/// </summary>
internal sealed class BrowserExternalSignIn(IJSRuntime js, NavigationManager navigation, IConfiguration configuration) : IExternalSignIn
{
    private readonly IConfigurationSection _settings = configuration.GetSection("ExternalLogin");

    public IReadOnlyList<string> Providers =>
        new[] { "google", "microsoft", "facebook" }.Where(p => !string.IsNullOrEmpty(ClientId(p))).ToArray();

    public async Task<string?> GetTokenAsync(string provider, CancellationToken cancellationToken)
    {
        var clientId = ClientId(provider) ?? throw new InvalidOperationException($"Provider '{provider}' is not configured.");
        var state = RandomToken();
        var redirectUri = navigation.ToAbsoluteUri("signin-callback.html").ToString();

        var url = provider switch
        {
            "google" => WithQuery("https://accounts.google.com/o/oauth2/v2/auth", OpenIdParameters(clientId, redirectUri, state)),
            "microsoft" => WithQuery("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", OpenIdParameters(clientId, redirectUri, state)),
            "facebook" => WithQuery("https://www.facebook.com/v21.0/dialog/oauth", new Dictionary<string, string?>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "token",
                ["scope"] = "email",
                ["state"] = state,
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
        };

        await using var module = await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/external-login.js");
        var fragment = await module.InvokeAsync<string?>("signIn", cancellationToken, url);
        if (string.IsNullOrEmpty(fragment))
        {
            return null;
        }

        var result = ParseQuery(fragment);
        if (result.TryGetValue("state", out var returnedState) && returnedState == state)
        {
            var tokenName = provider == "facebook" ? "access_token" : "id_token";
            if (result.TryGetValue(tokenName, out var token) && !string.IsNullOrEmpty(token))
            {
                return token;
            }
        }

        return null;
    }

    private string? ClientId(string provider) => _settings[$"{provider}:ClientId"] is { Length: > 0 } id ? id : null;

    private static Dictionary<string, string?> OpenIdParameters(string clientId, string redirectUri, string state) => new()
    {
        ["client_id"] = clientId,
        ["redirect_uri"] = redirectUri,
        ["response_type"] = "id_token",
        ["response_mode"] = "fragment",
        ["scope"] = "openid email profile",
        ["nonce"] = RandomToken(),
        ["state"] = state,
        ["prompt"] = "select_account",
    };

    private static string WithQuery(string url, IEnumerable<KeyValuePair<string, string?>> parameters) =>
        url + "?" + string.Join('&', parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value ?? string.Empty)}"));

    private static Dictionary<string, string> ParseQuery(string query) => query
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .GroupBy(pair => Uri.UnescapeDataString(pair[0]))
        .ToDictionary(g => g.Key, g => Uri.UnescapeDataString(g.First().ElementAtOrDefault(1)?.Replace('+', ' ') ?? string.Empty));

    private static string RandomToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
