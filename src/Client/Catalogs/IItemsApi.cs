using Refit;
using Registry.Contracts.Catalogs;

namespace Registry.Client.Catalogs;

[Headers("Authorization: Bearer")]
public interface IItemsApi
{
    [Get("/groups/{groupId}/catalogs/{catalogId}/items")]
    Task<IReadOnlyList<ItemResponse>> ListAsync(Guid groupId, Guid catalogId, CancellationToken cancellationToken = default);

    [Get("/groups/{groupId}/catalogs/{catalogId}/items/{itemId}")]
    Task<ItemResponse> GetAsync(Guid groupId, Guid catalogId, Guid itemId, CancellationToken cancellationToken = default);

    [Post("/groups/{groupId}/catalogs/{catalogId}/items")]
    Task<ItemResponse> CreateAsync(Guid groupId, Guid catalogId, [Body] ItemRequest request, CancellationToken cancellationToken = default);

    [Put("/groups/{groupId}/catalogs/{catalogId}/items/{itemId}")]
    Task<ItemResponse> UpdateAsync(Guid groupId, Guid catalogId, Guid itemId, [Body] ItemRequest request, CancellationToken cancellationToken = default);

    [Delete("/groups/{groupId}/catalogs/{catalogId}/items/{itemId}")]
    Task DeleteAsync(Guid groupId, Guid catalogId, Guid itemId, CancellationToken cancellationToken = default);

    [Get("/groups/{groupId}/catalogs/{catalogId}/publishers")]
    Task<IReadOnlyList<PublisherResponse>> ListPublishersAsync(Guid groupId, Guid catalogId, CancellationToken cancellationToken = default);
}
