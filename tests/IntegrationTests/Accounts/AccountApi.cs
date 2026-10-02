using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Registry.Contracts.Accounts;

namespace Registry.IntegrationTests.Accounts;

internal sealed record TokenResponse(string AccessToken, string RefreshToken);

internal static class AccountApi
{
    public const string Password = "Correct-horse-1";

    public static async Task RegisterAndConfirmAsync(this HttpClient client, RegistryApiFactory factory, string email)
    {
        await client.RegisterAsync(email);
        await client.ConfirmEmailAsync(factory, email);
    }

    public static async Task RegisterAsync(this HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static async Task ConfirmEmailAsync(this HttpClient client, RegistryApiFactory factory, string email)
    {
        // Identity passes the link HTML-encoded, ready to drop into an email body.
        var link = new Uri(WebUtility.HtmlDecode(factory.Emails.ConfirmationLinks[email]));
        var response = await client.GetAsync(link.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/auth/login", new { email, password });

    public static Task<HttpResponseMessage> ExternalLoginAsync(this HttpClient client, string provider, string token) =>
        client.PostAsJsonAsync($"/auth/external/{provider}", new { token });

    public static async Task<AccountResponse> GetMeAsync(this HttpClient client, HttpResponseMessage loginResponse)
    {
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/account/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }
}
