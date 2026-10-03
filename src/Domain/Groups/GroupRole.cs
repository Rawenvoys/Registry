namespace Registry.Domain.Groups;

public enum GroupRole
{
    /// <summary>Everything, including deleting the group and handing over ownership.</summary>
    Owner,

    /// <summary>Manages members and locations.</summary>
    Admin,

    /// <summary>Works with the group's data.</summary>
    Member,
}
