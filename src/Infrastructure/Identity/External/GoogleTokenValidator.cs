using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Registry.Application.Accounts;

namespace Registry.Infrastructure.Identity.External;

/// <summary>Validates Google ID tokens obtained by the web or mobile client.</summary>
public sealed class GoogleTokenValidator(IOptions<ExternalAuthOptions> options) : IExternalTokenValidator
{
    private static readonly ConfigurationManager<OpenIdConnectConfiguration> Configuration = new(
        "https://accounts.google.com/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever());

    public async Task<ExternalIdentity?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ConfigurationManager = Configuration,
            ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
            ValidAudiences = options.Value.Google.ClientIds,
        });

        if (!result.IsValid || TokenClaims.GetString(result.Claims, "sub") is not { } subject)
        {
            return null;
        }

        var claims = result.Claims;
        return new ExternalIdentity(
            ExternalProviders.Google,
            ProviderKey: subject,
            Email: TokenClaims.GetString(claims, "email"),
            EmailVerified: TokenClaims.IsTrue(TokenClaims.Get(claims, "email_verified")),
            DisplayName: TokenClaims.GetString(claims, "name"));
    }
}
