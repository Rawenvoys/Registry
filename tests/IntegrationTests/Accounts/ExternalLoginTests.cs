using System.Net;

namespace Registry.IntegrationTests.Accounts;

public class ExternalLoginTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task New_email_creates_account_without_password()
    {
        var token = factory.Google.Issue("google-new", "new@example.com");

        var me = await _client.GetMeAsync(await _client.ExternalLoginAsync("google", token));

        Assert.Equal("new@example.com", me.Email);
        Assert.Equal("Test User", me.DisplayName);
        Assert.False(me.HasPassword);
        Assert.Equal(["google"], me.ExternalLogins);
    }

    [Fact]
    public async Task Second_sign_in_uses_the_same_account()
    {
        var first = await _client.GetMeAsync(
            await _client.ExternalLoginAsync("google", factory.Google.Issue("google-repeat", "repeat@example.com")));

        var second = await _client.GetMeAsync(
            await _client.ExternalLoginAsync("google", factory.Google.Issue("google-repeat", "repeat@example.com")));

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Verified_email_links_to_existing_confirmed_account_and_keeps_password()
    {
        await _client.RegisterAndConfirmAsync(factory, "linked@example.com");
        var passwordAccount = await _client.GetMeAsync(await _client.LoginAsync("linked@example.com"));

        var googleAccount = await _client.GetMeAsync(
            await _client.ExternalLoginAsync("google", factory.Google.Issue("google-linked", "linked@example.com")));

        Assert.Equal(passwordAccount.Id, googleAccount.Id);
        Assert.True(googleAccount.HasPassword);
        Assert.Equal(["google"], googleAccount.ExternalLogins);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync("linked@example.com")).StatusCode);
    }

    [Fact]
    public async Task Linking_unconfirmed_account_removes_its_password()
    {
        // Someone registered with this email but never proved they own it.
        await _client.RegisterAsync("squatted@example.com");

        var me = await _client.GetMeAsync(
            await _client.ExternalLoginAsync("google", factory.Google.Issue("google-owner", "squatted@example.com")));

        Assert.False(me.HasPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync("squatted@example.com")).StatusCode);
    }

    [Fact]
    public async Task Unverified_email_does_not_link_to_existing_account()
    {
        await _client.RegisterAndConfirmAsync(factory, "victim@example.com");

        var response = await _client.ExternalLoginAsync(
            "google", factory.Google.Issue("google-attacker", "victim@example.com", emailVerified: false));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unverified_email_creates_account_that_waits_for_confirmation()
    {
        var token = factory.Google.Issue("google-unverified", "pending@example.com", emailVerified: false);

        var response = await _client.ExternalLoginAsync("google", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_email_is_rejected()
    {
        var response = await _client.ExternalLoginAsync("google", factory.Google.Issue("google-no-email", email: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_token_is_rejected()
    {
        var response = await _client.ExternalLoginAsync("google", "not-a-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("facebook")]
    [InlineData("myspace")]
    public async Task Unconfigured_or_unknown_provider_is_not_found(string provider)
    {
        var response = await _client.ExternalLoginAsync(provider, "token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
