namespace Registry.Contracts.Groups;

public sealed record GroupDetails(
    Guid Id,
    string Name,
    GroupRole MyRole,
    IReadOnlyList<MemberResponse> Members,
    IReadOnlyList<LocationResponse> Locations);

public sealed record MemberResponse(Guid UserId, string? Email, string? DisplayName, GroupRole Role, DateTimeOffset JoinedAt);

public sealed record LocationResponse(Guid Id, string Name, string? Address, bool IsDefault);
