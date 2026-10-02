namespace Registry.Contracts.Groups;

/// <param name="Email">When set, only the account with this email can accept.</param>
public sealed record InvitationRequest(GroupRole Role, string? Email = null);

public sealed record InvitationResponse(string Code, GroupRole Role, string? Email, DateTimeOffset ExpiresAt);
