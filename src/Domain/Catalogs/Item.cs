namespace Registry.Domain.Catalogs;

/// <summary>One entry in a catalog: a wine, a release, a track. Quantities and history come later.</summary>
public class Item
{
    public const int TitleMaxLength = 300;

    private Item()
    {
    }

    public Guid Id { get; private set; }

    public Guid CatalogId { get; private set; }

    public string Title { get; private set; } = null!;

    public PartialDate? ReleaseDate { get; private set; }

    public Guid? PublisherId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Item Create(Guid catalogId, string title, PartialDate? releaseDate, Publisher? publisher, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        CatalogId = catalogId,
        Title = RequireTitle(title),
        ReleaseDate = releaseDate,
        PublisherId = PublisherOf(catalogId, publisher),
        CreatedAt = now,
    };

    public void Update(string title, PartialDate? releaseDate, Publisher? publisher)
    {
        Title = RequireTitle(title);
        ReleaseDate = releaseDate;
        PublisherId = PublisherOf(CatalogId, publisher);
    }

    /// <summary>The database only checks that the publisher exists; the same-catalog rule lives here.</summary>
    private static Guid? PublisherOf(Guid catalogId, Publisher? publisher)
    {
        if (publisher is not null && publisher.CatalogId != catalogId)
        {
            throw new DomainException("publisher_other_catalog", "Ten wydawca należy do innego katalogu.");
        }

        return publisher?.Id;
    }

    private static string RequireTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("item_title_required", "Podaj nazwę wpisu.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length > TitleMaxLength)
        {
            throw new DomainException("title_too_long", $"Nazwa może mieć najwyżej {TitleMaxLength} znaków.");
        }

        return trimmed;
    }
}
