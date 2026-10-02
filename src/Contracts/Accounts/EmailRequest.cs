namespace Registry.Contracts.Accounts;

/// <summary>Body of /auth/resendConfirmationEmail and /auth/forgotPassword.</summary>
public sealed record EmailRequest(string Email);
