using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Registry.Domain.Catalogs;

namespace Registry.Infrastructure.Persistence.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> item)
    {
        item.Property(i => i.Id).ValueGeneratedNever();
        item.Property(i => i.Title).HasMaxLength(Item.TitleMaxLength);

        // Stored as "2020", "2020-05" or "2020-05-17" so sorting by text sorts by date.
        item.Property(i => i.ReleaseDate)
            .HasConversion(new ValueConverter<PartialDate, string>(d => d.ToString(), s => PartialDate.Parse(s)))
            .HasMaxLength(PartialDate.MaxLength);

        // Deleting a catalog deletes its items; deleting a publisher only clears it from items.
        item.HasOne<Catalog>().WithMany().HasForeignKey(i => i.CatalogId).OnDelete(DeleteBehavior.Cascade);
        item.HasOne<Publisher>().WithMany().HasForeignKey(i => i.PublisherId).OnDelete(DeleteBehavior.SetNull);
    }
}
