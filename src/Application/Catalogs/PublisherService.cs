using Microsoft.EntityFrameworkCore;
using Registry.Application.Common;
using Registry.Contracts.Catalogs;
using Registry.Domain;
using Registry.Domain.Catalogs;

namespace Registry.Application.Catalogs;

/// <summary>
/// Publishers of a catalog. Like items, every member of the catalog's group manages them.
/// Methods return null or false when the catalog is not in one of the user's groups.
/// </summary>
public sealed class PublisherService(IRegistryDbContext db)
{
    public async Task<IReadOnlyList<PublisherResponse>?> ListAsync(Guid userId, Guid groupId, Guid catalogId, CancellationToken cancellationToken)
    {
        if (!await db.IsCatalogMemberAsync(userId, groupId, catalogId, cancellationToken))
        {
            return null;
        }

        var publishers = await db.Publishers.Where(p => p.CatalogId == catalogId).AsNoTracking().ToListAsync(cancellationToken);
        var counts = await db.Items
            .Where(i => i.CatalogId == catalogId && i.PublisherId != null)
            .GroupBy(i => i.PublisherId!.Value)
            .Select(g => new { PublisherId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.PublisherId, g => g.Count, cancellationToken);

        return publishers
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => ToResponse(p, counts.GetValueOrDefault(p.Id)))
            .ToList();
    }

    public async Task<PublisherResponse?> CreateAsync(Guid userId, Guid groupId, Guid catalogId, PublisherRequest request, CancellationToken cancellationToken)
    {
        if (!await db.IsCatalogMemberAsync(userId, groupId, catalogId, cancellationToken))
        {
            return null;
        }

        var publisher = Publisher.Create(catalogId, request.Name);
        await RequireUniqueNameAsync(catalogId, publisher.Name, exceptId: null, cancellationToken);

        db.Publishers.Add(publisher);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(publisher, 0);
    }

    public async Task<PublisherResponse?> RenameAsync(Guid userId, Guid groupId, Guid catalogId, Guid publisherId, PublisherRequest request, CancellationToken cancellationToken)
    {
        if (!await db.IsCatalogMemberAsync(userId, groupId, catalogId, cancellationToken))
        {
            return null;
        }

        var publisher = await db.Publishers.SingleOrDefaultAsync(p => p.Id == publisherId && p.CatalogId == catalogId, cancellationToken);
        if (publisher is null)
        {
            return null;
        }

        publisher.Rename(request.Name);
        await RequireUniqueNameAsync(catalogId, publisher.Name, exceptId: publisher.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(publisher, await db.Items.CountAsync(i => i.PublisherId == publisher.Id, cancellationToken));
    }

    /// <summary>Items of a deleted publisher stay in the catalog without a publisher.</summary>
    public async Task<bool> DeleteAsync(Guid userId, Guid groupId, Guid catalogId, Guid publisherId, CancellationToken cancellationToken)
    {
        if (!await db.IsCatalogMemberAsync(userId, groupId, catalogId, cancellationToken))
        {
            return false;
        }

        var publisher = await db.Publishers.SingleOrDefaultAsync(p => p.Id == publisherId && p.CatalogId == catalogId, cancellationToken);
        if (publisher is null)
        {
            return false;
        }

        db.Publishers.Remove(publisher);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task RequireUniqueNameAsync(Guid catalogId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var names = await db.Publishers
            .Where(p => p.CatalogId == catalogId && p.Id != exceptId)
            .Select(p => p.Name)
            .ToListAsync(cancellationToken);

        if (names.Contains(name, StringComparer.CurrentCultureIgnoreCase))
        {
            throw new DomainException("name_taken", $"Wydawca „{name}” już jest w tym katalogu.");
        }
    }

    private static PublisherResponse ToResponse(Publisher publisher, int itemCount) =>
        new(publisher.Id, publisher.CatalogId, publisher.Name, itemCount);
}
