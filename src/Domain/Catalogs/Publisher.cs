using Registry.Domain.Groups;

namespace Registry.Domain.Catalogs;

/// <summary>
/// Who put an item out: a record label, a winery, a bottler. Each catalog keeps its own list,
/// so wine catalogs never suggest music labels.
/// </summary>
public class Publisher
{
    public const int NameMaxLength = 100;

    private Publisher()
    {
    }

    public Guid Id { get; private set; }

    public Guid CatalogId { get; private set; }

    public string Name { get; private set; } = null!;

    public static Publisher Create(Guid catalogId, string name) => new()
    {
        Id = Guid.NewGuid(),
        CatalogId = catalogId,
        Name = Group.RequireName(name, "publisher_name_required", "Podaj nazwę wydawcy."),
    };

    public void Rename(string name) => Name = Group.RequireName(name, "publisher_name_required", "Podaj nazwę wydawcy.");
}
