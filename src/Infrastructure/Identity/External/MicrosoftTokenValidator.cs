using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Registry.Application.Accounts;

namespace Registry.Infrastructure.Identity.External;

/// <summary>
/// Validates Microsoft identity platform ID tokens from both personal and work or school accounts.
/// </summary>
public sealed class MicrosoftTokenValidator(IOptions<ExternalAuthOptions> options) : IExternalTokenValidator
{
    private const string Authority = "https://login.microsoftonline.com/common/v2.0";

    // Tenant that every personal Microsoft account (outlook.com, hotmail.com, live.com) belongs to.
    private const string PersonalAccountsTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

    private static readonly ConfigurationManager<OpenIdConnectConfiguration> Configuration = new(
        $"{Authority}/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever());

    public async Task<ExternalIdentity?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ConfigurationManager = Configuration,
            IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(Authority).Validate,
            ValidAudiences = options.Value.Microsoft.ClientIds,
        });

        if (!result.IsValid)
        {
            return null;
        }

        var claims = result.Claims;
        // tid and oid are only present when the client requested the profile scope.
        if (TokenClaims.GetString(claims, "tid") is not { } tenantId || TokenClaims.GetString(claims, "oid") is not { } objectId)
        {
            return null;
        }

        // Work or school tenants can set any email on their users without proving ownership,
        // so the email only counts as verified for personal accounts or when Microsoft says the domain is verified.
        var emailVerified = tenantId == PersonalAccountsTenantId || TokenClaims.IsTrue(TokenClaims.Get(claims, "xms_edov"));

        return new ExternalIdentity(
            ExternalProviders.Microsoft,
            // oid is stable across the web and mobile app registrations, unlike sub.
            ProviderKey: $"{tenantId}:{objectId}",
            Email: TokenClaims.GetString(claims, "email") ?? TokenClaims.GetString(claims, "preferred_username"),
            EmailVerified: emailVerified,
            DisplayName: TokenClaims.GetString(claims, "name"));
    }
}
