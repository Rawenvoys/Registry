using System.Net;
using Refit;
using Registry.Client.Accounts;
using Registry.Client.Catalogs;
using Registry.Client.Groups;
using Registry.Contracts.Accounts;
using Registry.Contracts.Catalogs;
using Registry.Contracts.Groups;

namespace Registry.IntegrationTests.Catalogs;

public class ItemApiTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    private async Task<(IGroupsApi Groups, ICatalogsApi Catalogs, IItemsApi Items)> SignInAsync(string email)
    {
        var http = factory.CreateClient();
        var login = await RestService.For<IAuthApi>(http)
            .ExternalLoginAsync("google", new ExternalLoginRequest(factory.Google.Issue($"google-{email}", email)));
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => ValueTask.FromResult(login.Content!.AccessToken),
        };

        return (RestService.For<IGroupsApi>(http, settings), RestService.For<ICatalogsApi>(http, settings), RestService.For<IItemsApi>(http, settings));
    }

    private static async Task<HttpStatusCode> StatusOf(Func<Task> call) =>
        (await Assert.ThrowsAnyAsync<ApiException>(call)).StatusCode;

    [Fact]
    public async Task Item_is_added_edited_listed_and_deleted()
    {
        var (groups, catalogs, items) = await SignInAsync("items@example.com");
        var group = await groups.CreateAsync(new CreateGroupRequest("Dom"));
        var wines = await catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));

        var kindzmarauli = await items.CreateAsync(group.Id, wines.Id, new ItemRequest("Kindzmarauli", "2020", "Tbilvino"));
        await items.CreateAsync(group.Id, wines.Id, new ItemRequest("Amarone"));

        Assert.Equal("2020", kindzmarauli.ReleaseDate);
        Assert.Equal("Tbilvino", kindzmarauli.PublisherName);

        var edited = await items.UpdateAsync(group.Id, wines.Id, kindzmarauli.Id, new ItemRequest(" Kindzmarauli Red ", "2020-05", " tbilvino "));
        Assert.Equal("Kindzmarauli Red", edited.Title);
        Assert.Equal("2020-05", edited.ReleaseDate);
        Assert.Equal(kindzmarauli.PublisherId, edited.PublisherId);

        Assert.Equal(["Amarone", "Kindzmarauli Red"], (await items.ListAsync(group.Id, wines.Id)).Select(i => i.Title));
        Assert.Equal(["Tbilvino"], (await items.ListPublishersAsync(group.Id, wines.Id)).Select(p => p.Name));

        var cleared = await items.UpdateAsync(group.Id, wines.Id, kindzmarauli.Id, new ItemRequest("Kindzmarauli Red"));
        Assert.Null(cleared.ReleaseDate);
        Assert.Null(cleared.PublisherId);

        await items.DeleteAsync(group.Id, wines.Id, kindzmarauli.Id);

        Assert.Equal(["Amarone"], (await items.ListAsync(group.Id, wines.Id)).Select(i => i.Title));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => items.GetAsync(group.Id, wines.Id, kindzmarauli.Id)));
    }

    [Fact]
    public async Task Invalid_item_is_rejected()
    {
        var (groups, catalogs, items) = await SignInAsync("items-invalid@example.com");
        var group = await groups.CreateAsync(new CreateGroupRequest("Dom"));
        var wines = await catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));

        Assert.Equal(HttpStatusCode.BadRequest, await StatusOf(() => items.CreateAsync(group.Id, wines.Id, new ItemRequest(" "))));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusOf(() => items.CreateAsync(group.Id, wines.Id, new ItemRequest("Wino", "2020-13"))));
        Assert.Empty(await items.ListAsync(group.Id, wines.Id));
    }

    [Fact]
    public async Task Publishers_are_kept_per_catalog()
    {
        var (groups, catalogs, items) = await SignInAsync("items-publishers@example.com");
        var group = await groups.CreateAsync(new CreateGroupRequest("Kolekcja"));
        var wines = await catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));
        var releases = await catalogs.CreateAsync(group.Id, new CatalogRequest("Wydania"));

        var wine = await items.CreateAsync(group.Id, wines.Id, new ItemRequest("Wino", PublisherName: "Q-dance"));
        var release = await items.CreateAsync(group.Id, releases.Id, new ItemRequest("EP", PublisherName: "Q-dance"));

        Assert.NotEqual(wine.PublisherId, release.PublisherId);
    }

    [Fact]
    public async Task Members_manage_items_and_strangers_do_not_see_them()
    {
        var owner = await SignInAsync("items-owner@example.com");
        var member = await SignInAsync("items-member@example.com");
        var stranger = await SignInAsync("items-stranger@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Dom"));
        var wines = await owner.Catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));
        await member.Groups.AcceptInvitationAsync((await owner.Groups.InviteAsync(group.Id, new InvitationRequest(GroupRole.Member))).Code);

        var item = await member.Items.CreateAsync(group.Id, wines.Id, new ItemRequest("Prosecco"));
        await member.Items.UpdateAsync(group.Id, wines.Id, item.Id, new ItemRequest("Prosecco DOC"));

        Assert.Equal(["Prosecco DOC"], (await owner.Items.ListAsync(group.Id, wines.Id)).Select(i => i.Title));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Items.ListAsync(group.Id, wines.Id)));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Items.GetAsync(group.Id, wines.Id, item.Id)));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Items.DeleteAsync(group.Id, wines.Id, item.Id)));

        // A catalog id from one group cannot be reached through another group's id.
        var strangerGroup = await stranger.Groups.CreateAsync(new CreateGroupRequest("Obca"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Items.ListAsync(strangerGroup.Id, wines.Id)));
    }

    [Fact]
    public async Task Publisher_is_added_renamed_and_deleted_leaving_its_items()
    {
        var (groups, catalogs, items) = await SignInAsync("publishers@example.com");
        var group = await groups.CreateAsync(new CreateGroupRequest("Dom"));
        var wines = await catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));

        var masi = await items.CreatePublisherAsync(group.Id, wines.Id, new PublisherRequest("Masi"));
        var item = await items.CreateAsync(group.Id, wines.Id, new ItemRequest("Amarone", PublisherName: "masi"));
        await items.CreatePublisherAsync(group.Id, wines.Id, new PublisherRequest("Antinori"));

        Assert.Equal(masi.Id, item.PublisherId);
        Assert.Equal(HttpStatusCode.Conflict, await StatusOf(() => items.CreatePublisherAsync(group.Id, wines.Id, new PublisherRequest(" MASI "))));
        Assert.Equal(HttpStatusCode.Conflict, await StatusOf(() => items.RenamePublisherAsync(group.Id, wines.Id, masi.Id, new PublisherRequest("antinori"))));

        var renamed = await items.RenamePublisherAsync(group.Id, wines.Id, masi.Id, new PublisherRequest("Masi Agricola"));
        Assert.Equal(1, renamed.ItemCount);
        Assert.Equal("Masi Agricola", (await items.GetAsync(group.Id, wines.Id, item.Id)).PublisherName);
        Assert.Equal([("Antinori", 0), ("Masi Agricola", 1)], (await items.ListPublishersAsync(group.Id, wines.Id)).Select(p => (p.Name, p.ItemCount)));

        await items.DeletePublisherAsync(group.Id, wines.Id, masi.Id);

        var orphan = await items.GetAsync(group.Id, wines.Id, item.Id);
        Assert.Null(orphan.PublisherId);
        Assert.Equal(["Antinori"], (await items.ListPublishersAsync(group.Id, wines.Id)).Select(p => p.Name));
    }

    [Fact]
    public async Task Strangers_cannot_manage_publishers()
    {
        var owner = await SignInAsync("publishers-owner@example.com");
        var stranger = await SignInAsync("publishers-stranger@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Dom"));
        var wines = await owner.Catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));
        var masi = await owner.Items.CreatePublisherAsync(group.Id, wines.Id, new PublisherRequest("Masi"));

        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Items.CreatePublisherAsync(group.Id, wines.Id, new PublisherRequest("X"))));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Items.RenamePublisherAsync(group.Id, wines.Id, masi.Id, new PublisherRequest("X"))));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Items.DeletePublisherAsync(group.Id, wines.Id, masi.Id)));
    }
}
