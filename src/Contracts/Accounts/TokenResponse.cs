namespace Registry.Contracts.Accounts;

/// <summary>Bearer tokens returned by /auth/login, /auth/refresh and /auth/external/{provider}.</summary>
public sealed record TokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);
