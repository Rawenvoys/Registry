using Microsoft.EntityFrameworkCore;
using Registry.Application.Common;
using Registry.Contracts.Catalogs;
using Registry.Domain.Catalogs;

namespace Registry.Application.Catalogs;

/// <summary>
/// Items of a catalog. Every member of the catalog's group adds, edits and deletes them.
/// Like <see cref="CatalogService"/>, methods return null or false when the catalog is not in one of the user's groups.
/// </summary>
public sealed class ItemService(IRegistryDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<ItemResponse>?> ListAsync(Guid userId, Guid groupId, Guid catalogId, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(userId, groupId, catalogId, cancellationToken))
        {
            return null;
        }

        var items = await db.Items.Where(i => i.CatalogId == catalogId).AsNoTracking().ToListAsync(cancellationToken);
        var publishers = await PublisherNamesAsync(catalogId, cancellationToken);
        return items
            .OrderBy(i => i.Title, StringComparer.CurrentCultureIgnoreCase)
            .Select(i => ToResponse(i, publishers))
            .ToList();
    }

    public async Task<ItemResponse?> GetAsync(Guid userId, Guid groupId, Guid catalogId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(userId, groupId, catalogId, cancellationToken))
        {
            return null;
        }

        var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(i => i.Id == itemId && i.CatalogId == catalogId, cancellationToken);
        return item is null ? null : ToResponse(item, await PublisherNamesAsync(catalogId, cancellationToken));
    }

    public async Task<ItemResponse?> CreateAsync(Guid userId, Guid groupId, Guid catalogId, ItemRequest request, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(userId, groupId, catalogId, cancellationToken))
        {
            return null;
        }

        var releaseDate = ParseDate(request.ReleaseDate);
        var publisher = await FindOrAddPublisherAsync(catalogId, request.PublisherName, cancellationToken);
        var item = Item.Create(catalogId, request.Title, releaseDate, publisher, clock.GetUtcNow());

        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(item, publisher);
    }

    public async Task<ItemResponse?> UpdateAsync(Guid userId, Guid groupId, Guid catalogId, Guid itemId, ItemRequest request, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(userId, groupId, catalogId, cancellationToken))
        {
            return null;
        }

        var item = await db.Items.SingleOrDefaultAsync(i => i.Id == itemId && i.CatalogId == catalogId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var releaseDate = ParseDate(request.ReleaseDate);
        var publisher = await FindOrAddPublisherAsync(catalogId, request.PublisherName, cancellationToken);
        item.Update(request.Title, releaseDate, publisher);

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(item, publisher);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid groupId, Guid catalogId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(userId, groupId, catalogId, cancellationToken))
        {
            return false;
        }

        var item = await db.Items.SingleOrDefaultAsync(i => i.Id == itemId && i.CatalogId == catalogId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        db.Items.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<bool> CanAccessAsync(Guid userId, Guid groupId, Guid catalogId, CancellationToken cancellationToken) =>
        db.IsCatalogMemberAsync(userId, groupId, catalogId, cancellationToken);

    private static PartialDate? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : PartialDate.Parse(value);

    /// <summary>Publishers are typed by name; a name the catalog does not know yet becomes a new publisher.</summary>
    private async Task<Publisher?> FindOrAddPublisherAsync(Guid catalogId, string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        var publishers = await db.Publishers.Where(p => p.CatalogId == catalogId).ToListAsync(cancellationToken);
        var existing = publishers.FirstOrDefault(p => string.Equals(p.Name, trimmed, StringComparison.CurrentCultureIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var publisher = Publisher.Create(catalogId, trimmed);
        db.Publishers.Add(publisher);
        return publisher;
    }

    private async Task<Dictionary<Guid, string>> PublisherNamesAsync(Guid catalogId, CancellationToken cancellationToken) =>
        await db.Publishers.Where(p => p.CatalogId == catalogId).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

    private static ItemResponse ToResponse(Item item, Dictionary<Guid, string> publishers) =>
        ToResponse(item, item.PublisherId is { } id && publishers.TryGetValue(id, out var name) ? name : null);

    private static ItemResponse ToResponse(Item item, Publisher? publisher) => ToResponse(item, publisher?.Name);

    private static ItemResponse ToResponse(Item item, string? publisherName) => new(
        item.Id,
        item.CatalogId,
        item.Title,
        item.ReleaseDate?.ToString(),
        item.PublisherId,
        publisherName,
        item.CreatedAt);
}
