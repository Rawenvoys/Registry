namespace Registry.Contracts.Catalogs;

/// <summary>
/// Body for adding and editing an item. <paramref name="ReleaseDate"/> is "2020", "2020-05" or "2020-05-17".
/// <paramref name="PublisherName"/> picks the catalog's publisher with that name, adding it when it is new.
/// </summary>
public sealed record ItemRequest(string Title, string? ReleaseDate = null, string? PublisherName = null);
