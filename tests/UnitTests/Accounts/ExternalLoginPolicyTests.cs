using Registry.Application.Accounts;

namespace Registry.UnitTests.Accounts;

public class ExternalLoginPolicyTests
{
    private static ExternalIdentity Identity(string? email = "jan@example.com", bool verified = true) =>
        new("google", "sub-1", email, verified, "Jan");

    [Fact]
    public void Already_linked_login_signs_in_even_without_email()
    {
        var decision = ExternalLoginPolicy.Decide(true, Identity(email: null), false, false);

        Assert.Equal(ExternalLoginDecision.SignInLinkedAccount, decision);
    }

    [Fact]
    public void Missing_email_requires_email()
    {
        var decision = ExternalLoginPolicy.Decide(false, Identity(email: null), false, false);

        Assert.Equal(ExternalLoginDecision.EmailRequired, decision);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Unknown_email_creates_account(bool verified)
    {
        var decision = ExternalLoginPolicy.Decide(false, Identity(verified: verified), false, false);

        Assert.Equal(ExternalLoginDecision.CreateAccount, decision);
    }

    [Fact]
    public void Verified_email_links_to_confirmed_account()
    {
        var decision = ExternalLoginPolicy.Decide(false, Identity(), true, true);

        Assert.Equal(ExternalLoginDecision.LinkToExistingAccount, decision);
    }

    [Fact]
    public void Verified_email_resets_credentials_of_unconfirmed_account()
    {
        var decision = ExternalLoginPolicy.Decide(false, Identity(), true, false);

        Assert.Equal(ExternalLoginDecision.LinkAndResetCredentials, decision);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Unverified_email_never_links_to_existing_account(bool existingConfirmed)
    {
        var decision = ExternalLoginPolicy.Decide(false, Identity(verified: false), true, existingConfirmed);

        Assert.Equal(ExternalLoginDecision.EmailNotVerified, decision);
    }
}
