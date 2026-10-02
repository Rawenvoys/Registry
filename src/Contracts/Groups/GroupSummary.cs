namespace Registry.Contracts.Groups;

/// <summary>A group as seen by one of its members.</summary>
public sealed record GroupSummary(Guid Id, string Name, GroupKind Kind, GroupRole MyRole);
