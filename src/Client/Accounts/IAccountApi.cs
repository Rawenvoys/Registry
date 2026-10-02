using Refit;
using Registry.Contracts.Accounts;

namespace Registry.Client.Accounts;

[Headers("Authorization: Bearer")]
public interface IAccountApi
{
    [Get("/account/me")]
    Task<AccountResponse> GetMeAsync(CancellationToken cancellationToken = default);
}
