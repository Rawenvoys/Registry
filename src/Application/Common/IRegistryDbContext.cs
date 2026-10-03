using Microsoft.EntityFrameworkCore;
using Registry.Domain.Groups;

namespace Registry.Application.Common;

public interface IRegistryDbContext
{
    DbSet<Group> Groups { get; }

    DbSet<Invitation> Invitations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
