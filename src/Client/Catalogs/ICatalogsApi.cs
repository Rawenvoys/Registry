using Refit;
using Registry.Contracts.Catalogs;

namespace Registry.Client.Catalogs;

[Headers("Authorization: Bearer")]
public interface ICatalogsApi
{
    [Get("/groups/{groupId}/catalogs")]
    Task<IReadOnlyList<CatalogResponse>> ListAsync(Guid groupId, CancellationToken cancellationToken = default);

    [Get("/groups/{groupId}/catalogs/{catalogId}")]
    Task<CatalogResponse> GetAsync(Guid groupId, Guid catalogId, CancellationToken cancellationToken = default);

    [Post("/groups/{groupId}/catalogs")]
    Task<CatalogResponse> CreateAsync(Guid groupId, [Body] CatalogRequest request, CancellationToken cancellationToken = default);

    [Put("/groups/{groupId}/catalogs/{catalogId}")]
    Task<CatalogResponse> RenameAsync(Guid groupId, Guid catalogId, [Body] CatalogRequest request, CancellationToken cancellationToken = default);

    [Delete("/groups/{groupId}/catalogs/{catalogId}")]
    Task DeleteAsync(Guid groupId, Guid catalogId, CancellationToken cancellationToken = default);
}
