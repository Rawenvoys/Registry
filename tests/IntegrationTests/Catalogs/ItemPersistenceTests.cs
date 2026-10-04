using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Registry.Domain.Catalogs;
using Registry.Domain.Groups;
using Registry.Infrastructure.Persistence;

namespace Registry.IntegrationTests.Catalogs;

public class ItemPersistenceTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private async Task<(Catalog Catalog, Publisher Publisher, Item Item)> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var group = Group.Create("Dom", Guid.NewGuid(), Now);
        var catalog = Catalog.Create(group.Id, "Wina", Now);
        var winery = Publisher.Create(catalog.Id, "Tbilvino");
        var item = Item.Create(catalog.Id, "Kindzmarauli", PartialDate.Parse("2020-05"), winery, Now);
        db.AddRange(group, catalog, winery, item);
        await db.SaveChangesAsync();
        return (catalog, winery, item);
    }

    private AppDbContext NewContext() => factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Release_date_is_stored_as_partial_date()
    {
        var (_, _, item) = await SeedAsync();

        var loaded = await NewContext().Items.SingleAsync(i => i.Id == item.Id);

        Assert.Equal("2020-05", loaded.ReleaseDate.ToString());
    }

    [Fact]
    public async Task Deleting_a_publisher_keeps_its_items()
    {
        var (_, winery, item) = await SeedAsync();

        await NewContext().Publishers.Where(p => p.Id == winery.Id).ExecuteDeleteAsync();

        var loaded = await NewContext().Items.SingleAsync(i => i.Id == item.Id);
        Assert.Null(loaded.PublisherId);
    }

    [Fact]
    public async Task Deleting_a_catalog_deletes_its_items_and_publishers()
    {
        var (catalog, winery, item) = await SeedAsync();

        await NewContext().Catalogs.Where(c => c.Id == catalog.Id).ExecuteDeleteAsync();

        var db = NewContext();
        Assert.False(await db.Items.AnyAsync(i => i.Id == item.Id));
        Assert.False(await db.Publishers.AnyAsync(p => p.Id == winery.Id));
    }
}
