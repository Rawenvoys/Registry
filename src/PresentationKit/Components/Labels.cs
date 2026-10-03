using Registry.Contracts.Groups;

namespace Registry.PresentationKit.Components;

public static class Labels
{
    public static string Role(GroupRole role) => role switch
    {
        GroupRole.Owner => "Właściciel",
        GroupRole.Admin => "Administrator",
        _ => "Członek",
    };
}
