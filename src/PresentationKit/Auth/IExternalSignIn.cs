namespace Registry.PresentationKit.Auth;

/// <summary>
/// Gets a token from Google, Microsoft or Facebook for <c>POST /auth/external/{provider}</c>.
/// Each host does it its own way (a browser popup on the web), so the host registers the implementation.
/// </summary>
public interface IExternalSignIn
{
    /// <summary>Providers configured in this host: google, microsoft, facebook.</summary>
    IReadOnlyList<string> Providers { get; }

    /// <returns>The provider token, or null when the user closed the window or cancelled.</returns>
    Task<string?> GetTokenAsync(string provider, CancellationToken cancellationToken);
}

internal sealed class NoExternalSignIn : IExternalSignIn
{
    public IReadOnlyList<string> Providers => [];

    public Task<string?> GetTokenAsync(string provider, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
