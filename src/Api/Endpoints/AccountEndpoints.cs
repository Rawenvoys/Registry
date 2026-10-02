using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Registry.Application.Accounts;
using Registry.Contracts.Accounts;
using Registry.Infrastructure.Identity;
using Registry.Infrastructure.Identity.External;

namespace Registry.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        // register, login, refresh, confirmEmail, forgotPassword, resetPassword, manage/*
        auth.MapIdentityApi<User>();
        auth.MapPost("/external/{provider}", ExternalLoginAsync);

        app.MapGet("/account/me", GetAccountAsync)
            .WithTags("Account")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ExternalLoginAsync(
        string provider,
        ExternalLoginRequest request,
        IServiceProvider services,
        ExternalLoginService externalLogins,
        SignInManager<User> signInManager,
        CancellationToken cancellationToken)
    {
        var validator = services.GetKeyedService<IExternalTokenValidator>(provider.ToLowerInvariant());
        if (validator is null || !IsConfigured(services, provider))
        {
            return TypedResults.Problem($"Logowanie przez '{provider}' nie jest dostępne.", statusCode: StatusCodes.Status404NotFound);
        }

        var identity = await validator.ValidateAsync(request.Token, cancellationToken);
        if (identity is null)
        {
            return TypedResults.Problem("Nieprawidłowy token dostawcy.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await externalLogins.SignInAsync(identity);
        if (!result.Succeeded)
        {
            return result.Decision switch
            {
                ExternalLoginDecision.EmailRequired => TypedResults.Problem(
                    "Dostawca nie udostępnił adresu e-mail.", statusCode: StatusCodes.Status400BadRequest, title: "email_required"),
                ExternalLoginDecision.EmailNotVerified => TypedResults.Problem(
                    "Konto z tym adresem już istnieje, a dostawca nie potwierdził adresu. Zaloguj się hasłem.",
                    statusCode: StatusCodes.Status409Conflict, title: "email_not_verified"),
                _ => TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })),
            };
        }

        if (!result.User!.EmailConfirmed)
        {
            // The provider did not verify the email of this new account; the client sends it through /auth/resendConfirmationEmail.
            return TypedResults.Problem(
                "Potwierdź adres e-mail, aby się zalogować.", statusCode: StatusCodes.Status403Forbidden, title: "email_not_confirmed");
        }

        // Same bearer tokens as /auth/login, written to the response by the sign-in handler.
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signInManager.SignInAsync(result.User, isPersistent: false);
        return TypedResults.Empty;
    }

    private static bool IsConfigured(IServiceProvider services, string provider)
    {
        var options = services.GetRequiredService<IOptions<ExternalAuthOptions>>().Value;
        return provider.ToLowerInvariant() switch
        {
            ExternalProviders.Google => options.Google.IsConfigured,
            ExternalProviders.Microsoft => options.Microsoft.IsConfigured,
            ExternalProviders.Facebook => options.Facebook.IsConfigured,
            _ => false,
        };
    }

    private static async Task<IResult> GetAccountAsync(ClaimsPrincipal principal, UserManager<User> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var logins = await userManager.GetLoginsAsync(user);
        return TypedResults.Ok(new AccountResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            await userManager.HasPasswordAsync(user),
            logins.Select(l => l.LoginProvider).ToArray()));
    }
}
