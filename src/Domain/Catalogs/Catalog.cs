using Registry.Domain.Groups;

namespace Registry.Domain.Catalogs;

/// <summary>
/// A list of things a group keeps track of, named by the user: wines, cattle, releases.
/// The application never knows what the catalog is about.
/// </summary>
public class Catalog
{
    public const int NameMaxLength = 100;

    private Catalog()
    {
    }

    public Guid Id { get; private set; }

    public Guid GroupId { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Catalog Create(Guid groupId, string name, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        GroupId = groupId,
        Name = Group.RequireName(name, "catalog_name_required", "Podaj nazwę katalogu."),
        CreatedAt = now,
    };

    public void Rename(string name) => Name = Group.RequireName(name, "catalog_name_required", "Podaj nazwę katalogu.");
}
