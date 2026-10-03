using System.Net;
using Refit;
using Registry.Client.Accounts;
using Registry.Contracts.Accounts;
using Registry.PresentationKit.Auth;

namespace Registry.UnitTests.PresentationKit;

public class AuthSessionTests
{
    private readonly FakeAuthApi _authApi = new();
    private readonly MemoryStorage _storage = new();
    private readonly ManualTime _time = new();

    private AuthSession CreateSession() => new(_authApi, _storage, _time);

    [Fact]
    public async Task Signed_out_session_has_no_token()
    {
        Assert.Null(await CreateSession().GetAccessTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Sign_in_survives_an_app_restart()
    {
        await CreateSession().SignInAsync(Tokens("access-1"));

        Assert.Equal("access-1", await CreateSession().GetAccessTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Expiring_token_is_refreshed()
    {
        var session = CreateSession();
        await session.SignInAsync(Tokens("access-1", refreshToken: "refresh-1"));
        _authApi.NextRefresh = Tokens("access-2");

        _time.Advance(TimeSpan.FromMinutes(59.5));

        Assert.Equal("access-2", await session.GetAccessTokenAsync(CancellationToken.None));
        Assert.Equal("refresh-1", _authApi.LastRefreshToken);
    }

    [Fact]
    public async Task Failed_refresh_ends_the_session()
    {
        var session = CreateSession();
        await session.SignInAsync(Tokens("access-1"));
        var changed = false;
        session.Changed += () => changed = true;

        _time.Advance(TimeSpan.FromHours(2));

        Assert.Null(await session.GetAccessTokenAsync(CancellationToken.None));
        Assert.True(changed);
        Assert.Null(await CreateSession().GetAccessTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Sign_out_forgets_the_tokens()
    {
        var session = CreateSession();
        await session.SignInAsync(Tokens("access-1"));

        await session.SignOutAsync();

        Assert.Null(await session.GetAccessTokenAsync(CancellationToken.None));
        Assert.Null(await CreateSession().GetAccessTokenAsync(CancellationToken.None));
    }

    private static TokenResponse Tokens(string accessToken, string refreshToken = "refresh") =>
        new("Bearer", accessToken, ExpiresIn: 3600, refreshToken);

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class MemoryStorage : ISessionStorage
    {
        private readonly Dictionary<string, string> _values = [];

        public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(_values.GetValueOrDefault(key));

        public ValueTask SetAsync(string key, string? value)
        {
            if (value is null)
            {
                _values.Remove(key);
            }
            else
            {
                _values[key] = value;
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeAuthApi : IAuthApi
    {
        public TokenResponse? NextRefresh { get; set; }

        public string? LastRefreshToken { get; private set; }

        public Task<IApiResponse<TokenResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
        {
            LastRefreshToken = request.RefreshToken;
            var status = NextRefresh is null ? HttpStatusCode.Unauthorized : HttpStatusCode.OK;
            return Task.FromResult<IApiResponse<TokenResponse>>(
                new ApiResponse<TokenResponse>(
                    new HttpResponseMessage(status) { RequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.test/auth/refresh") },
                    NextRefresh,
                    new RefitSettings()));
        }

        public Task<IApiResponse> RegisterAsync(PasswordCredentials credentials, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IApiResponse<TokenResponse>> LoginAsync(PasswordCredentials credentials, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IApiResponse<TokenResponse>> ExternalLoginAsync(string provider, ExternalLoginRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IApiResponse> ResendConfirmationEmailAsync(EmailRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IApiResponse> ForgotPasswordAsync(EmailRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
