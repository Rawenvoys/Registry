using Microsoft.EntityFrameworkCore;
using Registry.Application.Common;

namespace Registry.Application.Catalogs;

internal static class CatalogAccess
{
    /// <summary>Whether the catalog belongs to the group and the user is a member of that group.</summary>
    public static Task<bool> IsCatalogMemberAsync(this IRegistryDbContext db, Guid userId, Guid groupId, Guid catalogId, CancellationToken cancellationToken) =>
        db.Catalogs.AnyAsync(
            c => c.Id == catalogId && c.GroupId == groupId
                && db.Groups.Any(g => g.Id == groupId && g.Memberships.Any(m => m.UserId == userId)),
            cancellationToken);
}
