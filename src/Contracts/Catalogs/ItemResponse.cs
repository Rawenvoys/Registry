namespace Registry.Contracts.Catalogs;

public sealed record ItemResponse(
    Guid Id,
    Guid CatalogId,
    string Title,
    string? ReleaseDate,
    Guid? PublisherId,
    string? PublisherName,
    DateTimeOffset CreatedAt);
