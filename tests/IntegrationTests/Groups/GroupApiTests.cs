using System.Net;
using Refit;
using Registry.Client.Accounts;
using Registry.Client.Groups;
using Registry.Contracts.Accounts;
using Registry.Contracts.Groups;

namespace Registry.IntegrationTests.Groups;

public class GroupApiTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    private async Task<IGroupsApi> SignInAsync(string email)
    {
        var http = factory.CreateClient();
        var login = await RestService.For<IAuthApi>(http)
            .ExternalLoginAsync("google", new ExternalLoginRequest(factory.Google.Issue($"google-{email}", email)));

        return RestService.For<IGroupsApi>(http, new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => ValueTask.FromResult(login.Content!.AccessToken),
        });
    }

    [Fact]
    public async Task New_account_has_no_groups()
    {
        var api = await SignInAsync("fresh@example.com");

        Assert.Empty(await api.ListAsync());
    }

    [Fact]
    public async Task Created_group_lists_owner_and_default_location()
    {
        var api = await SignInAsync("home@example.com");

        var group = await api.CreateAsync(new CreateGroupRequest("Dom"));

        Assert.Equal(GroupRole.Owner, group.MyRole);
        Assert.Equal("home@example.com", Assert.Single(group.Members).Email);
        Assert.True(Assert.Single(group.Locations).IsDefault);
        Assert.Equal([new GroupSummary(group.Id, "Dom", GroupRole.Owner)], await api.ListAsync());
    }

    [Fact]
    public async Task Group_adds_locations()
    {
        var api = await SignInAsync("shop@example.com");
        var group = await api.CreateAsync(new CreateGroupRequest("Winiarnia"));

        await api.AddLocationAsync(group.Id, new LocationRequest("Rynek", "ul. Długa 1"));

        var details = await api.GetAsync(group.Id);
        Assert.Equal(["Winiarnia", "Rynek"], details.Locations.Select(l => l.Name));
    }

    [Fact]
    public async Task Group_without_name_is_rejected()
    {
        var api = await SignInAsync("noname@example.com");

        var error = await Assert.ThrowsAnyAsync<ApiException>(() => api.CreateAsync(new CreateGroupRequest(" ")));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task Invited_user_joins_with_invited_role()
    {
        var owner = await SignInAsync("inviter@example.com");
        var group = await owner.CreateAsync(new CreateGroupRequest("Dom"));
        var invitation = await owner.InviteAsync(group.Id, new InvitationRequest(GroupRole.Member));

        var guest = await SignInAsync("guest@example.com");
        var joined = await guest.AcceptInvitationAsync(invitation.Code);

        Assert.Equal(GroupRole.Member, joined.MyRole);
        Assert.Equal(2, (await owner.GetAsync(group.Id)).Members.Count);
    }

    [Fact]
    public async Task Invitation_for_an_email_only_works_for_that_account()
    {
        var owner = await SignInAsync("email-owner@example.com");
        var group = await owner.CreateAsync(new CreateGroupRequest("Dom"));
        var invitation = await owner.InviteAsync(group.Id, new InvitationRequest(GroupRole.Admin, "ola@example.com"));

        var wrongPerson = await SignInAsync("jan@example.com");
        var error = await Assert.ThrowsAnyAsync<ApiException>(() => wrongPerson.AcceptInvitationAsync(invitation.Code));
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);

        var ola = await SignInAsync("ola@example.com");
        Assert.Equal(GroupRole.Admin, (await ola.AcceptInvitationAsync(invitation.Code)).MyRole);
    }

    [Fact]
    public async Task Member_cannot_invite()
    {
        var owner = await SignInAsync("owner2@example.com");
        var group = await owner.CreateAsync(new CreateGroupRequest("Dom"));
        var member = await SignInAsync("member2@example.com");
        await member.AcceptInvitationAsync((await owner.InviteAsync(group.Id, new InvitationRequest(GroupRole.Member))).Code);

        var error = await Assert.ThrowsAnyAsync<ApiException>(() => member.InviteAsync(group.Id, new InvitationRequest(GroupRole.Member)));

        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
    }

    [Fact]
    public async Task Other_peoples_groups_are_not_found()
    {
        var owner = await SignInAsync("private@example.com");
        var group = await owner.CreateAsync(new CreateGroupRequest("Prywatna"));
        var stranger = await SignInAsync("stranger@example.com");

        var error = await Assert.ThrowsAnyAsync<ApiException>(() => stranger.GetAsync(group.Id));

        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
    }

    [Fact]
    public async Task Unknown_invitation_code_is_not_found()
    {
        var api = await SignInAsync("unknown-code@example.com");

        var error = await Assert.ThrowsAnyAsync<ApiException>(() => api.AcceptInvitationAsync("NOPE"));

        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
    }
}
