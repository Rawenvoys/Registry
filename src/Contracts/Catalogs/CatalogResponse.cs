namespace Registry.Contracts.Catalogs;

public sealed record CatalogResponse(Guid Id, Guid GroupId, string Name, DateTimeOffset CreatedAt);
