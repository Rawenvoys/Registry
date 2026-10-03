using Registry.Domain;
using Registry.Domain.Catalogs;
using Registry.Domain.Groups;

namespace Registry.UnitTests.Groups;

public class MembershipManagementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Admin = Guid.NewGuid();
    private static readonly Guid Member = Guid.NewGuid();

    /// <summary>A group with one owner, one admin and one member.</summary>
    private static Group Team()
    {
        var group = Group.Create("Firma", Owner, Now);
        group.Accept(group.Invite(Owner, null, GroupRole.Admin, Now), Admin, null, Now);
        group.Accept(group.Invite(Owner, null, GroupRole.Member, Now), Member, null, Now);
        return group;
    }

    private static string ErrorCode(Action action) => Assert.Throws<DomainException>(action).Code;

    [Fact]
    public void Admin_renames_member_cannot()
    {
        var group = Team();

        group.Rename(Admin, "  Nowa nazwa ");

        Assert.Equal("Nowa nazwa", group.Name);
        Assert.Equal("forbidden", ErrorCode(() => group.Rename(Member, "X")));
    }

    [Fact]
    public void Only_owner_can_delete()
    {
        var group = Team();

        group.EnsureCanDelete(Owner);
        Assert.Equal("forbidden", ErrorCode(() => group.EnsureCanDelete(Admin)));
    }

    [Fact]
    public void Owner_promotes_member_to_owner()
    {
        var group = Team();

        group.ChangeRole(Owner, Member, GroupRole.Owner);

        Assert.Equal(GroupRole.Owner, group.MembershipOf(Member)!.Role);
    }

    [Fact]
    public void Admin_moves_people_between_member_and_admin_only()
    {
        var group = Team();

        group.ChangeRole(Admin, Member, GroupRole.Admin);
        Assert.Equal(GroupRole.Admin, group.MembershipOf(Member)!.Role);

        Assert.Equal("forbidden", ErrorCode(() => group.ChangeRole(Admin, Member, GroupRole.Owner)));
        Assert.Equal("forbidden", ErrorCode(() => group.ChangeRole(Admin, Owner, GroupRole.Member)));
    }

    [Fact]
    public void Member_cannot_change_roles()
    {
        Assert.Equal("forbidden", ErrorCode(() => Team().ChangeRole(Member, Admin, GroupRole.Member)));
    }

    [Fact]
    public void Last_owner_cannot_step_down_or_leave()
    {
        var group = Team();

        Assert.Equal("last_owner", ErrorCode(() => group.ChangeRole(Owner, Owner, GroupRole.Admin)));
        Assert.Equal("last_owner", ErrorCode(() => group.RemoveMember(Owner, Owner)));

        group.ChangeRole(Owner, Admin, GroupRole.Owner);
        group.RemoveMember(Owner, Owner);
        Assert.Null(group.MembershipOf(Owner));
    }

    [Fact]
    public void Anyone_can_leave()
    {
        var group = Team();

        group.RemoveMember(Member, Member);

        Assert.Null(group.MembershipOf(Member));
    }

    [Fact]
    public void Admin_removes_members_but_not_admins_or_owners()
    {
        var group = Team();
        var otherAdmin = Guid.NewGuid();
        group.Accept(group.Invite(Owner, null, GroupRole.Admin, Now), otherAdmin, null, Now);

        Assert.Equal("forbidden", ErrorCode(() => group.RemoveMember(Admin, otherAdmin)));
        Assert.Equal("forbidden", ErrorCode(() => group.RemoveMember(Admin, Owner)));
        group.RemoveMember(Admin, Member);

        Assert.Null(group.MembershipOf(Member));
    }

    [Fact]
    public void Member_cannot_remove_others()
    {
        var group = Team();

        Assert.Equal("forbidden", ErrorCode(() => group.RemoveMember(Member, Admin)));
    }

    [Fact]
    public void Pending_invitations_skip_used_and_expired_ones()
    {
        var group = Group.Create("Dom", Owner, Now);
        var used = group.Invite(Owner, null, GroupRole.Member, Now);
        group.Accept(used, Member, null, Now);
        group.Invite(Owner, null, GroupRole.Member, Now - Invitation.Lifetime);
        var open = group.Invite(Owner, null, GroupRole.Member, Now);

        Assert.Equal([open.Code], group.PendingInvitations(Owner, Now).Select(i => i.Code));
        Assert.Equal("forbidden", ErrorCode(() => group.PendingInvitations(Member, Now)));
    }

    [Fact]
    public void Revoked_invitation_is_gone()
    {
        var group = Group.Create("Dom", Owner, Now);
        var invitation = group.Invite(Owner, null, GroupRole.Member, Now);

        group.RevokeInvitation(Owner, invitation.Code);

        Assert.Empty(group.PendingInvitations(Owner, Now));
        Assert.Equal("invitation_not_found", ErrorCode(() => group.RevokeInvitation(Owner, invitation.Code)));
    }

    [Fact]
    public void Catalog_name_is_required_and_trimmed()
    {
        var catalog = Catalog.Create(Guid.NewGuid(), "  Wina ", Now);

        Assert.Equal("Wina", catalog.Name);
        Assert.Equal("catalog_name_required", ErrorCode(() => catalog.Rename(" ")));
    }
}
