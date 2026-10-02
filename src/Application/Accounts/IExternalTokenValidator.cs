namespace Registry.Application.Accounts;

public interface IExternalTokenValidator
{
    /// <summary>Returns the identity carried by the token, or null when the token is invalid.</summary>
    Task<ExternalIdentity?> ValidateAsync(string token, CancellationToken cancellationToken);
}
