namespace Registry.Domain.Groups;

public class Membership
{
    private Membership()
    {
    }

    internal Membership(Guid groupId, Guid userId, GroupRole role, DateTimeOffset joinedAt)
    {
        GroupId = groupId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public Guid GroupId { get; private set; }

    public Guid UserId { get; private set; }

    public GroupRole Role { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public bool CanManage => Role is GroupRole.Owner or GroupRole.Admin;

    internal void ChangeRole(GroupRole role) => Role = role;
}
