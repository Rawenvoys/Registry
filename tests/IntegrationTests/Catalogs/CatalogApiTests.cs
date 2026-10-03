using System.Net;
using Refit;
using Registry.Client.Accounts;
using Registry.Client.Catalogs;
using Registry.Client.Groups;
using Registry.Contracts.Accounts;
using Registry.Contracts.Catalogs;
using Registry.Contracts.Groups;

namespace Registry.IntegrationTests.Catalogs;

public class CatalogApiTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    private async Task<(IGroupsApi Groups, ICatalogsApi Catalogs)> SignInAsync(string email)
    {
        var http = factory.CreateClient();
        var login = await RestService.For<IAuthApi>(http)
            .ExternalLoginAsync("google", new ExternalLoginRequest(factory.Google.Issue($"google-{email}", email)));
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => ValueTask.FromResult(login.Content!.AccessToken),
        };

        return (RestService.For<IGroupsApi>(http, settings), RestService.For<ICatalogsApi>(http, settings));
    }

    private static async Task<HttpStatusCode> StatusOf(Func<Task> call) =>
        (await Assert.ThrowsAnyAsync<ApiException>(call)).StatusCode;

    [Fact]
    public async Task Catalog_is_created_renamed_listed_and_deleted()
    {
        var (groups, catalogs) = await SignInAsync("catalogs@example.com");
        var group = await groups.CreateAsync(new CreateGroupRequest("Kolekcja"));

        var releases = await catalogs.CreateAsync(group.Id, new CatalogRequest("Wydania"));
        await catalogs.CreateAsync(group.Id, new CatalogRequest("Artyści"));
        await catalogs.RenameAsync(group.Id, releases.Id, new CatalogRequest("Wydania EP"));

        Assert.Equal(["Artyści", "Wydania EP"], (await catalogs.ListAsync(group.Id)).Select(c => c.Name));
        Assert.Equal("Wydania EP", (await catalogs.GetAsync(group.Id, releases.Id)).Name);

        await catalogs.DeleteAsync(group.Id, releases.Id);

        Assert.Equal(["Artyści"], (await catalogs.ListAsync(group.Id)).Select(c => c.Name));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => catalogs.GetAsync(group.Id, releases.Id)));
    }

    [Fact]
    public async Task Catalog_names_are_unique_within_a_group()
    {
        var (groups, catalogs) = await SignInAsync("unique@example.com");
        var group = await groups.CreateAsync(new CreateGroupRequest("Dom"));
        await catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));

        Assert.Equal(HttpStatusCode.Conflict, await StatusOf(() => catalogs.CreateAsync(group.Id, new CatalogRequest(" wina "))));
    }

    [Fact]
    public async Task Members_see_catalogs_but_only_managers_change_them()
    {
        var owner = await SignInAsync("cat-owner@example.com");
        var member = await SignInAsync("cat-member@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Dom"));
        var wines = await owner.Catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));
        await member.Groups.AcceptInvitationAsync((await owner.Groups.InviteAsync(group.Id, new InvitationRequest(GroupRole.Member))).Code);

        Assert.Single(await member.Catalogs.ListAsync(group.Id));
        Assert.Equal(HttpStatusCode.Forbidden, await StatusOf(() => member.Catalogs.CreateAsync(group.Id, new CatalogRequest("Piwa"))));
        Assert.Equal(HttpStatusCode.Forbidden, await StatusOf(() => member.Catalogs.DeleteAsync(group.Id, wines.Id)));
    }

    [Fact]
    public async Task Strangers_do_not_see_catalogs()
    {
        var owner = await SignInAsync("cat-private@example.com");
        var stranger = await SignInAsync("cat-stranger@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Prywatna"));
        var catalog = await owner.Catalogs.CreateAsync(group.Id, new CatalogRequest("Notatki"));

        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Catalogs.ListAsync(group.Id)));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => stranger.Catalogs.RenameAsync(group.Id, catalog.Id, new CatalogRequest("X"))));
    }
}
