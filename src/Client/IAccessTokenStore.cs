namespace Registry.Client;

/// <summary>
/// Where the typed clients get the signed-in user's access token from.
/// Hosts replace the in-memory default with a store that also refreshes and persists the session.
/// </summary>
public interface IAccessTokenStore
{
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken);
}

public sealed class InMemoryAccessTokenStore : IAccessTokenStore
{
    public string? AccessToken { get; set; }

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken) => ValueTask.FromResult(AccessToken);
}
