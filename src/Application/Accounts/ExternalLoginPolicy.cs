namespace Registry.Application.Accounts;

public enum ExternalLoginDecision
{
    /// <summary>The external login is already linked to an account.</summary>
    SignInLinkedAccount,

    /// <summary>No account uses this email yet.</summary>
    CreateAccount,

    /// <summary>An account with a confirmed email exists and the provider verified the same email.</summary>
    LinkToExistingAccount,

    /// <summary>
    /// An account exists but its email was never confirmed. Whoever set its password may not own the email,
    /// so the password is removed and existing sessions are invalidated before linking.
    /// </summary>
    LinkAndResetCredentials,

    /// <summary>The provider returned no email, so there is nothing to create or link the account by.</summary>
    EmailRequired,

    /// <summary>An account exists, but the provider did not verify the email, so linking could hand the account to someone else.</summary>
    EmailNotVerified,
}

public static class ExternalLoginPolicy
{
    public static ExternalLoginDecision Decide(
        bool loginAlreadyLinked,
        ExternalIdentity identity,
        bool accountWithEmailExists,
        bool existingAccountEmailConfirmed)
    {
        if (loginAlreadyLinked)
        {
            return ExternalLoginDecision.SignInLinkedAccount;
        }

        if (string.IsNullOrWhiteSpace(identity.Email))
        {
            return ExternalLoginDecision.EmailRequired;
        }

        if (!accountWithEmailExists)
        {
            return ExternalLoginDecision.CreateAccount;
        }

        if (!identity.EmailVerified)
        {
            return ExternalLoginDecision.EmailNotVerified;
        }

        return existingAccountEmailConfirmed
            ? ExternalLoginDecision.LinkToExistingAccount
            : ExternalLoginDecision.LinkAndResetCredentials;
    }
}
