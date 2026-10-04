using Registry.Domain;
using Registry.Domain.Catalogs;

namespace Registry.UnitTests.Catalogs;

public class ItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Wines = Guid.NewGuid();

    [Fact]
    public void Item_needs_only_a_title()
    {
        var item = Item.Create(Wines, "  Kindzmarauli 2020  ", releaseDate: null, publisher: null, Now);

        Assert.Equal("Kindzmarauli 2020", item.Title);
        Assert.Null(item.ReleaseDate);
        Assert.Null(item.PublisherId);
        Assert.Equal(Now, item.CreatedAt);
    }

    [Fact]
    public void Item_requires_a_title()
    {
        var error = Assert.Throws<DomainException>(() => Item.Create(Wines, " ", null, null, Now));

        Assert.Equal("item_title_required", error.Code);
    }

    [Fact]
    public void Publisher_must_come_from_the_same_catalog()
    {
        var label = Publisher.Create(Guid.NewGuid(), "Scantraxx");

        var error = Assert.Throws<DomainException>(() => Item.Create(Wines, "Kindzmarauli", null, label, Now));

        Assert.Equal("publisher_other_catalog", error.Code);
    }

    [Fact]
    public void Item_keeps_its_publisher_and_release_date()
    {
        var winery = Publisher.Create(Wines, "Tbilvino");

        var item = Item.Create(Wines, "Kindzmarauli", PartialDate.Parse("2020"), winery, Now);

        Assert.Equal(winery.Id, item.PublisherId);
        Assert.Equal("2020", item.ReleaseDate.ToString());
    }
}
