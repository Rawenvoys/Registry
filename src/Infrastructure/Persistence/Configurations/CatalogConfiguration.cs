using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Catalogs;
using Registry.Domain.Groups;

namespace Registry.Infrastructure.Persistence.Configurations;

internal sealed class CatalogConfiguration : IEntityTypeConfiguration<Catalog>
{
    public void Configure(EntityTypeBuilder<Catalog> catalog)
    {
        catalog.Property(c => c.Id).ValueGeneratedNever();
        catalog.Property(c => c.Name).HasMaxLength(Catalog.NameMaxLength);

        // Deleting a group deletes its catalogs.
        catalog.HasOne<Group>().WithMany().HasForeignKey(c => c.GroupId).OnDelete(DeleteBehavior.Cascade);
    }
}
