using Registry.Domain;
using Registry.Domain.Groups;

namespace Registry.UnitTests.Groups;

public class GroupTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Owner = Guid.NewGuid();

    private static Group NewGroup() => Group.Create("Sklepy", Owner, Now);

    [Fact]
    public void Creator_becomes_owner()
    {
        Assert.Equal(GroupRole.Owner, NewGroup().MembershipOf(Owner)!.Role);
    }

    [Fact]
    public void Group_starts_with_one_default_location_named_after_it()
    {
        var group = Group.Create("  Dom  ", Owner, Now);

        var location = Assert.Single(group.Locations);
        Assert.Equal("Dom", location.Name);
        Assert.True(location.IsDefault);
    }

    [Fact]
    public void Group_requires_a_name()
    {
        var error = Assert.Throws<DomainException>(() => Group.Create(" ", Owner, Now));

        Assert.Equal("group_name_required", error.Code);
    }

    [Fact]
    public void Owner_adds_more_locations()
    {
        var group = NewGroup();

        group.AddLocation(Owner, "Rynek", "ul. Długa 1");

        Assert.Equal(["Sklepy", "Rynek"], group.Locations.Select(l => l.Name));
        Assert.Equal("ul. Długa 1", group.Locations.Last().Address);
        Assert.False(group.Locations.Last().IsDefault);
    }

    [Fact]
    public void Member_cannot_add_locations_or_invite()
    {
        var group = NewGroup();
        var member = Guid.NewGuid();
        group.Accept(group.Invite(Owner, null, GroupRole.Member, Now), member, null, Now);

        Assert.Equal("forbidden", Assert.Throws<DomainException>(() => group.AddLocation(member, "Rynek", null)).Code);
        Assert.Equal("forbidden", Assert.Throws<DomainException>(() => group.Invite(member, null, GroupRole.Member, Now)).Code);
    }

    [Fact]
    public void Admin_cannot_invite_owner()
    {
        var group = NewGroup();
        var admin = Guid.NewGuid();
        group.Accept(group.Invite(Owner, null, GroupRole.Admin, Now), admin, null, Now);

        Assert.Equal("forbidden", Assert.Throws<DomainException>(() => group.Invite(admin, null, GroupRole.Owner, Now)).Code);
        Assert.Equal(GroupRole.Member, group.Invite(admin, null, GroupRole.Member, Now).Role);
    }

    [Fact]
    public void Invitation_can_be_used_once()
    {
        var group = NewGroup();
        var invitation = group.Invite(Owner, null, GroupRole.Member, Now);
        group.Accept(invitation, Guid.NewGuid(), null, Now);

        var error = Assert.Throws<DomainException>(() => group.Accept(invitation, Guid.NewGuid(), null, Now));

        Assert.Equal("invitation_invalid", error.Code);
    }

    [Fact]
    public void Expired_invitation_is_rejected()
    {
        var group = NewGroup();
        var invitation = group.Invite(Owner, null, GroupRole.Member, Now);

        var error = Assert.Throws<DomainException>(() => group.Accept(invitation, Guid.NewGuid(), null, Now + Invitation.Lifetime));

        Assert.Equal("invitation_invalid", error.Code);
    }

    [Fact]
    public void Invitation_for_an_email_only_works_for_that_email()
    {
        var group = NewGroup();
        var invitation = group.Invite(Owner, "ola@example.com", GroupRole.Member, Now);

        Assert.Equal("invitation_other_email",
            Assert.Throws<DomainException>(() => group.Accept(invitation, Guid.NewGuid(), "jan@example.com", Now)).Code);

        var membership = group.Accept(invitation, Guid.NewGuid(), "OLA@example.com", Now);
        Assert.Equal(GroupRole.Member, membership.Role);
    }

    [Fact]
    public void Existing_member_cannot_join_again()
    {
        var group = NewGroup();

        var error = Assert.Throws<DomainException>(() => group.Accept(group.Invite(Owner, null, GroupRole.Member, Now), Owner, null, Now));

        Assert.Equal("already_member", error.Code);
    }
}
