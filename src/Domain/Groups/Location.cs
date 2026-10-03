namespace Registry.Domain.Groups;

/// <summary>
/// Where the group's stock physically is: a home, a shop, a warehouse.
/// Every group starts with one default location; clients show locations only once there is a second one.
/// </summary>
public class Location
{
    private Location()
    {
    }

    internal Location(Guid groupId, string name, string? address, bool isDefault)
    {
        Id = Guid.NewGuid();
        GroupId = groupId;
        Name = name;
        Address = address;
        IsDefault = isDefault;
    }

    public Guid Id { get; private set; }

    public Guid GroupId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Address { get; private set; }

    public bool IsDefault { get; private set; }
}
