using System.Net;
using Refit;
using Registry.Client.Accounts;
using Registry.Contracts.Accounts;

namespace Registry.IntegrationTests.Accounts;

/// <summary>Checks that the Refit client used by WebApp and MobileApp matches the API's routes and payloads.</summary>
public class ClientContractTests(RegistryApiFactory factory) : IClassFixture<RegistryApiFactory>
{
    [Fact]
    public async Task Client_registers_signs_in_refreshes_and_reads_profile()
    {
        var http = factory.CreateClient();
        var auth = RestService.For<IAuthApi>(http);
        var credentials = new PasswordCredentials("client@example.com", AccountApi.Password);

        var registered = await auth.RegisterAsync(credentials);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        await http.ConfirmEmailAsync(factory, credentials.Email);

        var login = await auth.LoginAsync(credentials);
        Assert.True(login.IsSuccessStatusCode);
        Assert.Equal("Bearer", login.Content!.TokenType);

        var refreshed = await auth.RefreshAsync(new RefreshRequest(login.Content.RefreshToken));
        Assert.True(refreshed.IsSuccessStatusCode);

        var account = RestService.For<IAccountApi>(http, new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => ValueTask.FromResult(refreshed.Content!.AccessToken),
        });
        var me = await account.GetMeAsync();
        Assert.Equal(credentials.Email, me.Email);
    }

    [Fact]
    public async Task Client_external_login_returns_tokens()
    {
        var auth = RestService.For<IAuthApi>(factory.CreateClient());
        var token = factory.Google.Issue("google-client", "client-google@example.com");

        var response = await auth.ExternalLoginAsync("google", new ExternalLoginRequest(token));

        Assert.True(response.IsSuccessStatusCode);
        Assert.False(string.IsNullOrEmpty(response.Content!.AccessToken));
    }
}
