namespace Registry.Contracts.Accounts;

public sealed record AccountResponse(Guid Id, string? Email, string? DisplayName, bool HasPassword, string[] ExternalLogins);
