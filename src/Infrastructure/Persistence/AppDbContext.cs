using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Common;
using Registry.Domain.Catalogs;
using Registry.Domain.Groups;
using Registry.Infrastructure.Identity;

namespace Registry.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IRegistryDbContext
{
    public DbSet<Group> Groups => Set<Group>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Catalog> Catalogs => Set<Catalog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(user => user.Property(u => u.DisplayName).HasMaxLength(200));

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
