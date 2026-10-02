namespace Registry.Application.Common;

public sealed record UserInfo(Guid Id, string? Email, string? DisplayName);

/// <summary>Reads account details owned by Identity, so group features can show who is who.</summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, UserInfo>> GetAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken);
}
