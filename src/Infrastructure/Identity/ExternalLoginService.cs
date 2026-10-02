using Microsoft.AspNetCore.Identity;
using Registry.Application.Accounts;

namespace Registry.Infrastructure.Identity;

public sealed record ExternalLoginResult(User? User, ExternalLoginDecision Decision, IEnumerable<IdentityError> Errors)
{
    public bool Succeeded => User is not null;
}

/// <summary>
/// Finds, creates or links the account for an identity confirmed by an external provider,
/// following <see cref="ExternalLoginPolicy"/>.
/// </summary>
public sealed class ExternalLoginService(UserManager<User> userManager)
{
    public async Task<ExternalLoginResult> SignInAsync(ExternalIdentity identity)
    {
        var linkedUser = await userManager.FindByLoginAsync(identity.Provider, identity.ProviderKey);
        var userWithEmail = linkedUser is null && !string.IsNullOrWhiteSpace(identity.Email)
            ? await userManager.FindByEmailAsync(identity.Email)
            : null;

        var decision = ExternalLoginPolicy.Decide(
            loginAlreadyLinked: linkedUser is not null,
            identity,
            accountWithEmailExists: userWithEmail is not null,
            existingAccountEmailConfirmed: userWithEmail?.EmailConfirmed ?? false);

        switch (decision)
        {
            case ExternalLoginDecision.SignInLinkedAccount:
                return Success(linkedUser!, decision);

            case ExternalLoginDecision.CreateAccount:
                var user = new User
                {
                    UserName = identity.Email,
                    Email = identity.Email,
                    EmailConfirmed = identity.EmailVerified,
                    DisplayName = identity.DisplayName,
                };
                var created = await userManager.CreateAsync(user);
                if (!created.Succeeded)
                {
                    return Failure(decision, created.Errors);
                }
                return await LinkAsync(user, identity, decision);

            case ExternalLoginDecision.LinkToExistingAccount:
                return await LinkAsync(userWithEmail!, identity, decision);

            case ExternalLoginDecision.LinkAndResetCredentials:
                var existing = userWithEmail!;
                if (await userManager.HasPasswordAsync(existing))
                {
                    var removed = await userManager.RemovePasswordAsync(existing);
                    if (!removed.Succeeded)
                    {
                        return Failure(decision, removed.Errors);
                    }
                }
                existing.EmailConfirmed = true;
                existing.DisplayName ??= identity.DisplayName;
                await userManager.UpdateAsync(existing);
                // Invalidates refresh tokens issued to whoever knew the removed password.
                await userManager.UpdateSecurityStampAsync(existing);
                return await LinkAsync(existing, identity, decision);

            default:
                return Failure(decision, []);
        }
    }

    private async Task<ExternalLoginResult> LinkAsync(User user, ExternalIdentity identity, ExternalLoginDecision decision)
    {
        var added = await userManager.AddLoginAsync(
            user,
            new UserLoginInfo(identity.Provider, identity.ProviderKey, identity.Provider));

        return added.Succeeded ? Success(user, decision) : Failure(decision, added.Errors);
    }

    private static ExternalLoginResult Success(User user, ExternalLoginDecision decision) => new(user, decision, []);

    private static ExternalLoginResult Failure(ExternalLoginDecision decision, IEnumerable<IdentityError> errors) =>
        new(null, decision, errors);
}
