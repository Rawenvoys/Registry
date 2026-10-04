using Microsoft.EntityFrameworkCore;
using Registry.Application.Common;
using Registry.Contracts.Catalogs;
using Registry.Domain;
using Registry.Domain.Catalogs;
using Registry.Domain.Groups;

namespace Registry.Application.Catalogs;

/// <summary>
/// Catalogs of a group. Every member sees them; owners and admins create, rename and delete them.
/// Like <see cref="Groups.GroupService"/>, methods return null or false when the user is not a member of the group.
/// </summary>
public sealed class CatalogService(IRegistryDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<CatalogResponse>?> ListAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
    {
        if (await LoadGroupAsync(groupId, cancellationToken) is not { } group || group.MembershipOf(userId) is null)
        {
            return null;
        }

        var catalogs = await db.Catalogs.Where(c => c.GroupId == groupId).AsNoTracking().ToListAsync(cancellationToken);
        return catalogs.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase).Select(ToResponse).ToList();
    }

    public async Task<CatalogResponse?> GetAsync(Guid userId, Guid groupId, Guid catalogId, CancellationToken cancellationToken)
    {
        if (await LoadGroupAsync(groupId, cancellationToken) is not { } group || group.MembershipOf(userId) is null)
        {
            return null;
        }

        var catalog = await db.Catalogs.AsNoTracking().SingleOrDefaultAsync(c => c.Id == catalogId && c.GroupId == groupId, cancellationToken);
        return catalog is null ? null : ToResponse(catalog);
    }

    public async Task<CatalogResponse?> CreateAsync(Guid userId, Guid groupId, CatalogRequest request, CancellationToken cancellationToken)
    {
        if (await LoadGroupAsync(groupId, cancellationToken) is not { } group || group.MembershipOf(userId) is null)
        {
            return null;
        }

        RequireManager(group, userId);
        var catalog = Catalog.Create(groupId, request.Name, clock.GetUtcNow());
        await RequireUniqueNameAsync(groupId, catalog.Name, exceptId: null, cancellationToken);

        db.Catalogs.Add(catalog);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(catalog);
    }

    public async Task<CatalogResponse?> RenameAsync(Guid userId, Guid groupId, Guid catalogId, CatalogRequest request, CancellationToken cancellationToken)
    {
        if (await LoadGroupAsync(groupId, cancellationToken) is not { } group || group.MembershipOf(userId) is null)
        {
            return null;
        }

        var catalog = await db.Catalogs.SingleOrDefaultAsync(c => c.Id == catalogId && c.GroupId == groupId, cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        RequireManager(group, userId);
        catalog.Rename(request.Name);
        await RequireUniqueNameAsync(groupId, catalog.Name, exceptId: catalog.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(catalog);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid groupId, Guid catalogId, CancellationToken cancellationToken)
    {
        if (await LoadGroupAsync(groupId, cancellationToken) is not { } group || group.MembershipOf(userId) is null)
        {
            return false;
        }

        var catalog = await db.Catalogs.SingleOrDefaultAsync(c => c.Id == catalogId && c.GroupId == groupId, cancellationToken);
        if (catalog is null)
        {
            return false;
        }

        RequireManager(group, userId);
        db.Catalogs.Remove(catalog);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<Group?> LoadGroupAsync(Guid groupId, CancellationToken cancellationToken) =>
        db.Groups.Include(g => g.Memberships).AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

    private static void RequireManager(Group group, Guid userId)
    {
        if (!group.CanManage(userId))
        {
            throw new DomainException("forbidden", "Katalogami zarządzają właściciel i administratorzy grupy.");
        }
    }

    private async Task RequireUniqueNameAsync(Guid groupId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var names = await db.Catalogs
            .Where(c => c.GroupId == groupId && c.Id != exceptId)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        if (names.Contains(name, StringComparer.CurrentCultureIgnoreCase))
        {
            throw new DomainException("name_taken", $"Katalog „{name}” już istnieje w tej grupie.");
        }
    }

    private static CatalogResponse ToResponse(Catalog catalog) => new(catalog.Id, catalog.GroupId, catalog.Name, catalog.CreatedAt);
}
