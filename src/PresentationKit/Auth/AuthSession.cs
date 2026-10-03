using System.Text.Json;
using Registry.Client;
using Registry.Client.Accounts;
using Registry.Contracts.Accounts;

namespace Registry.PresentationKit.Auth;

/// <summary>
/// The signed-in user's tokens. Hands out the access token to the typed clients,
/// refreshes it shortly before it expires and persists the session through <see cref="ISessionStorage"/>.
/// </summary>
public sealed class AuthSession(IAuthApi authApi, ISessionStorage storage, TimeProvider time) : IAccessTokenStore
{
    private const string StorageKey = "registry.session";
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private StoredTokens? _tokens;
    private bool _loaded;

    /// <summary>Raised after sign-in, sign-out, or when a refresh fails and the session ends.</summary>
    public event Action? Changed;

    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var ended = false;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_loaded)
            {
                _tokens = Deserialize(await storage.GetAsync(StorageKey));
                _loaded = true;
            }

            if (_tokens is null || _tokens.ExpiresAt - RefreshMargin > time.GetUtcNow())
            {
                return _tokens?.AccessToken;
            }

            var response = await authApi.RefreshAsync(new RefreshRequest(_tokens.RefreshToken), cancellationToken);
            if (response is { IsSuccessful: true, Content: { } refreshed })
            {
                await StoreAsync(refreshed);
                return _tokens.AccessToken;
            }

            // The refresh token expired or the password changed: the user has to sign in again.
            await StoreAsync(null);
            ended = true;
            return null;
        }
        finally
        {
            _lock.Release();
            if (ended)
            {
                Changed?.Invoke();
            }
        }
    }

    public async Task SignInAsync(TokenResponse tokens)
    {
        await _lock.WaitAsync();
        try
        {
            await StoreAsync(tokens);
            _loaded = true;
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke();
    }

    /// <summary>Bearer tokens are not revoked server-side, so signing out forgets them on this device.</summary>
    public async Task SignOutAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await StoreAsync(null);
            _loaded = true;
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke();
    }

    private async Task StoreAsync(TokenResponse? tokens)
    {
        _tokens = tokens is null
            ? null
            : new StoredTokens(tokens.AccessToken, tokens.RefreshToken, time.GetUtcNow().AddSeconds(tokens.ExpiresIn));
        await storage.SetAsync(StorageKey, _tokens is null ? null : JsonSerializer.Serialize(_tokens));
    }

    private static StoredTokens? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StoredTokens>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record StoredTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
