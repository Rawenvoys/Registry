using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Catalogs;

namespace Registry.Infrastructure.Persistence.Configurations;

internal sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> publisher)
    {
        publisher.Property(p => p.Id).ValueGeneratedNever();
        publisher.Property(p => p.Name).HasMaxLength(Publisher.NameMaxLength);

        // Deleting a catalog deletes its publishers.
        publisher.HasOne<Catalog>().WithMany().HasForeignKey(p => p.CatalogId).OnDelete(DeleteBehavior.Cascade);
    }
}
