using Refit;
using Registry.Contracts.Accounts;

namespace Registry.Client.Accounts;

public interface IAuthApi
{
    [Post("/auth/register")]
    Task<IApiResponse> RegisterAsync([Body] PasswordCredentials credentials, CancellationToken cancellationToken = default);

    [Post("/auth/login")]
    Task<IApiResponse<TokenResponse>> LoginAsync([Body] PasswordCredentials credentials, CancellationToken cancellationToken = default);

    [Post("/auth/refresh")]
    Task<IApiResponse<TokenResponse>> RefreshAsync([Body] RefreshRequest request, CancellationToken cancellationToken = default);

    /// <param name="provider">google, microsoft or facebook.</param>
    [Post("/auth/external/{provider}")]
    Task<IApiResponse<TokenResponse>> ExternalLoginAsync(string provider, [Body] ExternalLoginRequest request, CancellationToken cancellationToken = default);

    [Post("/auth/resendConfirmationEmail")]
    Task<IApiResponse> ResendConfirmationEmailAsync([Body] EmailRequest request, CancellationToken cancellationToken = default);

    [Post("/auth/forgotPassword")]
    Task<IApiResponse> ForgotPasswordAsync([Body] EmailRequest request, CancellationToken cancellationToken = default);
}
