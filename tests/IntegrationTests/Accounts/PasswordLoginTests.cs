using System.Net;

namespace Registry.IntegrationTests.Accounts;

public class PasswordLoginTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_requires_confirmed_email()
    {
        await _client.RegisterAsync("unconfirmed@example.com");

        var response = await _client.LoginAsync("unconfirmed@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirmed_account_signs_in_and_reads_its_profile()
    {
        await _client.RegisterAndConfirmAsync(factory, "anna@example.com");

        var me = await _client.GetMeAsync(await _client.LoginAsync("anna@example.com"));

        Assert.Equal("anna@example.com", me.Email);
        Assert.True(me.HasPassword);
        Assert.Empty(me.ExternalLogins);
    }

    [Fact]
    public async Task Profile_requires_authentication()
    {
        var response = await _client.GetAsync("/account/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
