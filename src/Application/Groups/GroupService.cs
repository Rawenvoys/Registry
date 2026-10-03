using Microsoft.EntityFrameworkCore;
using Registry.Application.Common;
using Registry.Contracts.Groups;
using Registry.Domain.Groups;
using ContractRole = Registry.Contracts.Groups.GroupRole;
using DomainRole = Registry.Domain.Groups.GroupRole;

namespace Registry.Application.Groups;

/// <summary>
/// Group use cases. Methods return null when the group does not exist or the user is not a member of it,
/// so callers cannot tell other people's groups apart from missing ones.
/// </summary>
public sealed class GroupService(IRegistryDbContext db, IUserDirectory users, TimeProvider clock)
{
	public async Task<GroupDetails> CreateAsync(Guid userId, CreateGroupRequest request, CancellationToken cancellationToken)
	{
		var group = Group.Create(request.Name, userId, clock.GetUtcNow());

		db.Groups.Add(group);
		await db.SaveChangesAsync(cancellationToken);
		return await ToDetailsAsync(group, userId, cancellationToken);
	}

	public async Task<IReadOnlyList<GroupSummary>> ListAsync(Guid userId, CancellationToken cancellationToken)
	{
		var groups = await db.Groups
			.Where(g => g.Memberships.Any(m => m.UserId == userId))
			.Include(g => g.Memberships.Where(m => m.UserId == userId))
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		return [.. groups
			.OrderBy(g => g.Name)
			.Select(g => new GroupSummary(g.Id, g.Name, Map(g.MembershipOf(userId)!.Role)))];
	}

	public async Task<GroupDetails?> GetAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken);
		return group?.MembershipOf(userId) is null ? null : await ToDetailsAsync(group, userId, cancellationToken);
	}

	public async Task<GroupDetails?> UpdateAsync(Guid userId, Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken);
		if (group?.MembershipOf(userId) is null)
		{
			return null;
		}

		group.Rename(userId, request.Name);
		await db.SaveChangesAsync(cancellationToken);
		return await ToDetailsAsync(group, userId, cancellationToken);
	}

	/// <summary>Deletes the group with its memberships, locations, invitations and catalogs.</summary>
	public async Task<bool> DeleteAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken, withInvitations: true);
		if (group?.MembershipOf(userId) is null)
		{
			return false;
		}

		group.EnsureCanDelete(userId);
		db.Groups.Remove(group);
		await db.SaveChangesAsync(cancellationToken);
		return true;
	}

	public async Task<bool> ChangeRoleAsync(Guid userId, Guid groupId, Guid memberId, ChangeRoleRequest request, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken);
		if (group?.MembershipOf(userId) is null)
		{
			return false;
		}

		group.ChangeRole(userId, memberId, Map(request.Role));
		await db.SaveChangesAsync(cancellationToken);
		return true;
	}

	/// <summary>Removes a member; with <paramref name="memberId"/> equal to <paramref name="userId"/> the user leaves the group.</summary>
	public async Task<bool> RemoveMemberAsync(Guid userId, Guid groupId, Guid memberId, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken);
		if (group?.MembershipOf(userId) is null)
		{
			return false;
		}

		group.RemoveMember(userId, memberId);
		await db.SaveChangesAsync(cancellationToken);
		return true;
	}

	public async Task<IReadOnlyList<InvitationResponse>?> ListInvitationsAsync(Guid userId, Guid groupId, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken, withInvitations: true);
		if (group?.MembershipOf(userId) is null)
		{
			return null;
		}

		return [.. group.PendingInvitations(userId, clock.GetUtcNow()).Select(ToResponse)];
	}

	public async Task<bool> RevokeInvitationAsync(Guid userId, Guid groupId, string code, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken, withInvitations: true);
		if (group?.MembershipOf(userId) is null)
		{
			return false;
		}

		group.RevokeInvitation(userId, code);
		await db.SaveChangesAsync(cancellationToken);
		return true;
	}

	public async Task<LocationResponse?> AddLocationAsync(Guid userId, Guid groupId, LocationRequest request, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken);
		if (group?.MembershipOf(userId) is null)
		{
			return null;
		}

		var location = group.AddLocation(userId, request.Name, request.Address);
		await db.SaveChangesAsync(cancellationToken);
		return ToResponse(location);
	}

	public async Task<InvitationResponse?> InviteAsync(Guid userId, Guid groupId, InvitationRequest request, CancellationToken cancellationToken)
	{
		var group = await LoadAsync(groupId, cancellationToken);
		if (group?.MembershipOf(userId) is null)
		{
			return null;
		}

		var invitation = group.Invite(userId, request.Email, Map(request.Role), clock.GetUtcNow());
		await db.SaveChangesAsync(cancellationToken);
		return ToResponse(invitation);
	}

	/// <summary>Returns null when no invitation has this code.</summary>
	public async Task<GroupSummary?> AcceptInvitationAsync(Guid userId, string? userEmail, string code, CancellationToken cancellationToken)
	{
		var invitation = await db.Invitations.SingleOrDefaultAsync(i => i.Code == code, cancellationToken);
		if (invitation is null)
		{
			return null;
		}

		var group = (await LoadAsync(invitation.GroupId, cancellationToken))!;
		var membership = group.Accept(invitation, userId, userEmail, clock.GetUtcNow());
		await db.SaveChangesAsync(cancellationToken);
		return new GroupSummary(group.Id, group.Name, Map(membership.Role));
	}

	private Task<Group?> LoadAsync(Guid groupId, CancellationToken cancellationToken, bool withInvitations = false)
	{
		var groups = db.Groups.Include(g => g.Memberships).Include(g => g.Locations);
		return withInvitations
			? groups.Include(g => g.Invitations).SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken)
			: groups.SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);
	}

	private async Task<GroupDetails> ToDetailsAsync(Group group, Guid userId, CancellationToken cancellationToken)
	{
		var people = await users.GetAsync(group.Memberships.Select(m => m.UserId), cancellationToken);

		var members = group.Memberships
			.OrderBy(m => m.JoinedAt)
			.Select(m =>
			{
				var person = people.GetValueOrDefault(m.UserId);
				return new MemberResponse(m.UserId, person?.Email, person?.DisplayName, Map(m.Role), m.JoinedAt);
			})
			.ToList();

		var locations = group.Locations
			.OrderByDescending(l => l.IsDefault)
			.ThenBy(l => l.Name)
			.Select(ToResponse)
			.ToList();

		return new GroupDetails(group.Id, group.Name, Map(group.MembershipOf(userId)!.Role), members, locations);
	}

	private static InvitationResponse ToResponse(Invitation invitation)
		=> new(invitation.Code, Map(invitation.Role), invitation.Email, invitation.ExpiresAt);

	private static LocationResponse ToResponse(Location location)
		=> new(location.Id, location.Name, location.Address, location.IsDefault);

	private static DomainRole Map(ContractRole role) => Enum.Parse<DomainRole>(role.ToString());

	private static ContractRole Map(DomainRole role) => Enum.Parse<ContractRole>(role.ToString());
}
