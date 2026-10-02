namespace Registry.Contracts.Groups;

/// <param name="FirstLocation">Required for <see cref="GroupKind.Business"/>, ignored otherwise.</param>
public sealed record CreateGroupRequest(string Name, GroupKind Kind, LocationRequest? FirstLocation = null);
