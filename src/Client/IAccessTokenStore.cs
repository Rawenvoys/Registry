namespace Registry.Client;

/// <summary>
/// Where the signed-in user's access token lives. Hosts can replace the in-memory default,
/// e.g. MobileApp with SecureStorage.
/// </summary>
public interface IAccessTokenStore
{
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken);

    ValueTask SetAccessTokenAsync(string? accessToken, CancellationToken cancellationToken);
}

public sealed class InMemoryAccessTokenStore : IAccessTokenStore
{
    private string? _accessToken;

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken) => ValueTask.FromResult(_accessToken);

    public ValueTask SetAccessTokenAsync(string? accessToken, CancellationToken cancellationToken)
    {
        _accessToken = accessToken;
        return ValueTask.CompletedTask;
    }
}
