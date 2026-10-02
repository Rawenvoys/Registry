namespace Registry.Contracts.Accounts;

/// <summary>Body of /auth/register and /auth/login.</summary>
public sealed record PasswordCredentials(string Email, string Password);
