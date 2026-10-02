using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Registry.Application.Accounts;

namespace Registry.Infrastructure.Identity.External;

/// <summary>
/// Validates a Facebook user access token with the Graph API and reads the user's profile.
/// </summary>
public sealed class FacebookTokenValidator(HttpClient http, IOptions<ExternalAuthOptions> options) : IExternalTokenValidator
{
    public async Task<ExternalIdentity?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var facebook = options.Value.Facebook;
        var appToken = Uri.EscapeDataString($"{facebook.AppId}|{facebook.AppSecret}");

        DebugTokenResponse? debug;
        MeResponse? me;
        try
        {
            debug = await http.GetFromJsonAsync<DebugTokenResponse>(
                $"debug_token?input_token={Uri.EscapeDataString(token)}&access_token={appToken}",
                cancellationToken);
            me = debug?.Data is { IsValid: true }
                ? await http.GetFromJsonAsync<MeResponse>(
                    $"me?fields=id,name,email&access_token={Uri.EscapeDataString(token)}",
                    cancellationToken)
                : null;
        }
        catch (HttpRequestException)
        {
            // Graph answers an expired or malformed token with an error status.
            return null;
        }

        // A token issued to another app must not sign anyone in here.
        if (debug?.Data is not { IsValid: true } data || data.AppId != facebook.AppId || data.UserId is null)
        {
            return null;
        }

        if (me is null || me.Id != data.UserId)
        {
            return null;
        }

        // Graph omits the email when the user has no valid (confirmed) address on the account.
        return new ExternalIdentity(
            ExternalProviders.Facebook,
            ProviderKey: me.Id,
            Email: me.Email,
            EmailVerified: me.Email is not null,
            DisplayName: me.Name);
    }

    private sealed record DebugTokenResponse([property: JsonPropertyName("data")] DebugTokenData? Data);

    private sealed record DebugTokenData(
        [property: JsonPropertyName("is_valid")] bool IsValid,
        [property: JsonPropertyName("app_id")] string? AppId,
        [property: JsonPropertyName("user_id")] string? UserId);

    private sealed record MeResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email);
}
