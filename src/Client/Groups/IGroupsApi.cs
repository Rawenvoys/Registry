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

    [Put("/groups/{groupId}")]
    Task<GroupDetails> UpdateAsync(Guid groupId, [Body] UpdateGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>Owner only. Deletes the group with its catalogs.</summary>
    [Delete("/groups/{groupId}")]
    Task DeleteAsync(Guid groupId, CancellationToken cancellationToken = default);

    [Put("/groups/{groupId}/members/{memberId}")]
    Task ChangeRoleAsync(Guid groupId, Guid memberId, [Body] ChangeRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a member; pass your own id to leave the group.</summary>
    [Delete("/groups/{groupId}/members/{memberId}")]
    Task RemoveMemberAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken = default);

    [Post("/groups/{groupId}/locations")]
    Task<LocationResponse> AddLocationAsync(Guid groupId, [Body] LocationRequest request, CancellationToken cancellationToken = default);

    [Post("/groups/{groupId}/invitations")]
    Task<InvitationResponse> InviteAsync(Guid groupId, [Body] InvitationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Invitations that are neither used nor expired.</summary>
    [Get("/groups/{groupId}/invitations")]
    Task<IReadOnlyList<InvitationResponse>> ListInvitationsAsync(Guid groupId, CancellationToken cancellationToken = default);

    [Delete("/groups/{groupId}/invitations/{code}")]
    Task RevokeInvitationAsync(Guid groupId, string code, CancellationToken cancellationToken = default);

    [Post("/invitations/{code}/accept")]
    Task<GroupSummary> AcceptInvitationAsync(string code, CancellationToken cancellationToken = default);
}
