using Microsoft.EntityFrameworkCore;
using Registry.Domain.Catalogs;
using Registry.Domain.Groups;

namespace Registry.Application.Common;

public interface IRegistryDbContext
{
    DbSet<Group> Groups { get; }

    DbSet<Invitation> Invitations { get; }

    DbSet<Catalog> Catalogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
