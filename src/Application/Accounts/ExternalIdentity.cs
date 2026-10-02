namespace Registry.Application.Accounts;

/// <summary>
/// Identity confirmed by an external provider (Google, Microsoft, Facebook) after its token was validated.
/// </summary>
public sealed record ExternalIdentity(
    string Provider,
    string ProviderKey,
    string? Email,
    bool EmailVerified,
    string? DisplayName);
