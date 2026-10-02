namespace Registry.Contracts.Accounts;

/// <summary>Token obtained from the provider: an ID token for Google and Microsoft, an access token for Facebook.</summary>
public sealed record ExternalLoginRequest(string Token);
