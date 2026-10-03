using System.Net;
using Refit;
using Registry.Client.Accounts;
using Registry.Client.Catalogs;
using Registry.Client.Groups;
using Registry.Contracts.Accounts;
using Registry.Contracts.Catalogs;
using Registry.Contracts.Groups;

namespace Registry.IntegrationTests.Groups;

public class GroupManagementTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    private sealed record Session(Guid UserId, IGroupsApi Groups, ICatalogsApi Catalogs);

    private async Task<Session> SignInAsync(string email)
    {
        var http = factory.CreateClient();
        var login = await RestService.For<IAuthApi>(http)
            .ExternalLoginAsync("google", new ExternalLoginRequest(factory.Google.Issue($"google-{email}", email)));
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => ValueTask.FromResult(login.Content!.AccessToken),
        };

        var account = await RestService.For<IAccountApi>(http, settings).GetMeAsync();
        return new Session(account.Id, RestService.For<IGroupsApi>(http, settings), RestService.For<ICatalogsApi>(http, settings));
    }

    private static async Task<Guid> JoinAsync(Session owner, Guid groupId, Session guest, GroupRole role)
    {
        var invitation = await owner.Groups.InviteAsync(groupId, new InvitationRequest(role));
        await guest.Groups.AcceptInvitationAsync(invitation.Code);
        return guest.UserId;
    }

    private static async Task<HttpStatusCode> StatusOf(Func<Task> call) =>
        (await Assert.ThrowsAnyAsync<ApiException>(call)).StatusCode;

    [Fact]
    public async Task Owner_renames_and_deletes_group_with_its_catalogs()
    {
        var owner = await SignInAsync("rename-owner@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Dom"));
        await owner.Catalogs.CreateAsync(group.Id, new CatalogRequest("Wina"));

        var renamed = await owner.Groups.UpdateAsync(group.Id, new UpdateGroupRequest("Dom Kowalskich"));
        Assert.Equal("Dom Kowalskich", renamed.Name);

        await owner.Groups.DeleteAsync(group.Id);

        Assert.Empty(await owner.Groups.ListAsync());
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => owner.Catalogs.ListAsync(group.Id)));
    }

    [Fact]
    public async Task Admin_cannot_delete_group()
    {
        var owner = await SignInAsync("delete-owner@example.com");
        var admin = await SignInAsync("delete-admin@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Firma"));
        await JoinAsync(owner, group.Id, admin, GroupRole.Admin);

        Assert.Equal(HttpStatusCode.Forbidden, await StatusOf(() => admin.Groups.DeleteAsync(group.Id)));
    }

    [Fact]
    public async Task Owner_changes_roles_and_removes_members()
    {
        var owner = await SignInAsync("roles-owner@example.com");
        var member = await SignInAsync("roles-member@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Firma"));
        var memberId = await JoinAsync(owner, group.Id, member, GroupRole.Member);

        await owner.Groups.ChangeRoleAsync(group.Id, memberId, new ChangeRoleRequest(GroupRole.Admin));
        Assert.Equal(GroupRole.Admin, (await member.Groups.GetAsync(group.Id)).MyRole);

        await owner.Groups.RemoveMemberAsync(group.Id, memberId);
        Assert.Empty(await member.Groups.ListAsync());
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => member.Groups.GetAsync(group.Id)));
    }

    [Fact]
    public async Task Member_leaves_but_last_owner_cannot()
    {
        var owner = await SignInAsync("leave-owner@example.com");
        var member = await SignInAsync("leave-member@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Dom"));
        var memberId = await JoinAsync(owner, group.Id, member, GroupRole.Member);

        await member.Groups.RemoveMemberAsync(group.Id, memberId);

        Assert.Empty(await member.Groups.ListAsync());
        Assert.Equal(HttpStatusCode.Conflict, await StatusOf(() => owner.Groups.RemoveMemberAsync(group.Id, owner.UserId)));
    }

    [Fact]
    public async Task Invitations_are_listed_and_revoked()
    {
        var owner = await SignInAsync("revoke-owner@example.com");
        var guest = await SignInAsync("revoke-guest@example.com");
        var group = await owner.Groups.CreateAsync(new CreateGroupRequest("Dom"));
        var invitation = await owner.Groups.InviteAsync(group.Id, new InvitationRequest(GroupRole.Member));

        Assert.Equal([invitation.Code], (await owner.Groups.ListInvitationsAsync(group.Id)).Select(i => i.Code));

        await owner.Groups.RevokeInvitationAsync(group.Id, invitation.Code);

        Assert.Empty(await owner.Groups.ListInvitationsAsync(group.Id));
        Assert.Equal(HttpStatusCode.NotFound, await StatusOf(() => guest.Groups.AcceptInvitationAsync(invitation.Code)));
    }
}
