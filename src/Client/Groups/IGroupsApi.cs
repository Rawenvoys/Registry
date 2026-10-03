using Refit;
using Registry.Contracts.Groups;

namespace Registry.Client.Groups;

[Headers("Authorization: Bearer")]
public interface IGroupsApi
{
    /// <summary>Groups the signed-in user belongs to. Empty means the setup wizard should run.</summary>
    [Get("/groups")]
    Task<IReadOnlyList<GroupSummary>> ListAsync(CancellationToken cancellationToken = default);

    [Post("/groups")]
    Task<GroupDetails> CreateAsync([Body] CreateGroupRequest request, CancellationToken cancellationToken = default);

    [Get("/groups/{groupId}")]
    Task<GroupDetails> GetAsync(Guid groupId, CancellationToken cancellationToken = default);

    [Post("/groups/{groupId}/locations")]
    Task<LocationResponse> AddLocationAsync(Guid groupId, [Body] LocationRequest request, CancellationToken cancellationToken = default);

    [Post("/groups/{groupId}/invitations")]
    Task<InvitationResponse> InviteAsync(Guid groupId, [Body] InvitationRequest request, CancellationToken cancellationToken = default);

    [Post("/invitations/{code}/accept")]
    Task<GroupSummary> AcceptInvitationAsync(string code, CancellationToken cancellationToken = default);
}
