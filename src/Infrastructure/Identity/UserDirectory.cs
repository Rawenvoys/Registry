using Microsoft.EntityFrameworkCore;
using Registry.Application.Common;
using Registry.Infrastructure.Persistence;

namespace Registry.Infrastructure.Identity;

internal sealed class UserDirectory(AppDbContext db) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, UserInfo>> GetAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserInfo(u.Id, u.Email, u.DisplayName))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }
}
