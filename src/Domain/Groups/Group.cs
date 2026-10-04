namespace Registry.Domain.Groups;

public class Group
{
    public const int NameMaxLength = 100;

    private readonly List<Membership> _memberships = [];
    private readonly List<Location> _locations = [];
    private readonly List<Invitation> _invitations = [];

    private Group()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<Membership> Memberships => _memberships;

    public IReadOnlyCollection<Location> Locations => _locations;

    public IReadOnlyCollection<Invitation> Invitations => _invitations;

    /// <summary>
    /// Creates a group owned by <paramref name="ownerId"/> with one default location named after the group.
    /// Clients show locations only once a group has more than one.
    /// </summary>
    public static Group Create(string name, Guid ownerId, DateTimeOffset now)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = RequireName(name, "group_name_required", "Podaj nazwę grupy."),
            CreatedAt = now,
        };

        group._memberships.Add(new Membership(group.Id, ownerId, GroupRole.Owner, now));
        group._locations.Add(new Location(group.Id, group.Name, address: null, isDefault: true));
        return group;
    }

    public Membership? MembershipOf(Guid userId) => _memberships.FirstOrDefault(m => m.UserId == userId);

    /// <summary>Whether <paramref name="userId"/> may change the group's setup: name, catalogs, locations.</summary>
    public bool CanManage(Guid userId) => MembershipOf(userId) is { CanManage: true };

    public void Rename(Guid actorId, string name)
    {
        RequireManager(actorId);
        Name = RequireName(name, "group_name_required", "Podaj nazwę grupy.");
    }

    /// <summary>Only an owner may delete the group; the caller removes it with everything in it.</summary>
    public void EnsureCanDelete(Guid actorId)
    {
        if (MembershipOf(actorId)?.Role != GroupRole.Owner)
        {
            throw new DomainException("forbidden", "Tylko właściciel może usunąć grupę.");
        }
    }

    /// <summary>
    /// Owners change anyone's role. Admins only move people between Member and Admin.
    /// The group always keeps at least one owner.
    /// </summary>
    public void ChangeRole(Guid actorId, Guid userId, GroupRole role)
    {
        var actor = RequireManager(actorId);
        var member = MembershipOf(userId) ?? throw new DomainException("member_not_found", "Ta osoba nie należy do grupy.");
        if (actor.Role != GroupRole.Owner && (member.Role == GroupRole.Owner || role == GroupRole.Owner))
        {
            throw new DomainException("forbidden", "Tylko właściciel może nadawać i odbierać rolę właściciela.");
        }

        if (member.Role == GroupRole.Owner && role != GroupRole.Owner)
        {
            RequireAnotherOwner(userId);
        }

        member.ChangeRole(role);
    }

    /// <summary>
    /// Removes <paramref name="userId"/> from the group. Anyone may leave; owners remove anyone,
    /// admins remove members only. The last owner cannot go.
    /// </summary>
    public void RemoveMember(Guid actorId, Guid userId)
    {
        var member = MembershipOf(userId) ?? throw new DomainException("member_not_found", "Ta osoba nie należy do grupy.");
        if (actorId != userId)
        {
            var actor = RequireManager(actorId);
            if (actor.Role != GroupRole.Owner && member.Role != GroupRole.Member)
            {
                throw new DomainException("forbidden", "Administrator może usuwać tylko członków.");
            }
        }

        if (member.Role == GroupRole.Owner)
        {
            RequireAnotherOwner(userId);
        }

        _memberships.Remove(member);
    }

    public Location AddLocation(Guid actorId, string name, string? address)
    {
        RequireManager(actorId);
        var location = new Location(Id, RequireName(name, "location_name_required", "Podaj nazwę lokalizacji."), Trim(address), isDefault: false);
        _locations.Add(location);
        return location;
    }

    public Invitation Invite(Guid actorId, string? email, GroupRole role, DateTimeOffset now)
    {
        var actor = RequireManager(actorId);
        if (role == GroupRole.Owner && actor.Role != GroupRole.Owner)
        {
            throw new DomainException("forbidden", "Tylko właściciel może zaprosić kolejnego właściciela.");
        }

        var invitation = new Invitation(Id, Trim(email), role, actorId, now);
        _invitations.Add(invitation);
        return invitation;
    }

    /// <summary>Invitations that can still be used, newest first.</summary>
    public IEnumerable<Invitation> PendingInvitations(Guid actorId, DateTimeOffset now)
    {
        RequireManager(actorId);
        return _invitations.Where(i => i.AcceptedAt is null && i.ExpiresAt > now).OrderByDescending(i => i.CreatedAt);
    }

    public void RevokeInvitation(Guid actorId, string code)
    {
        RequireManager(actorId);
        var invitation = _invitations.FirstOrDefault(i => i.Code == code)
            ?? throw new DomainException("invitation_not_found", "Nie ma takiego zaproszenia.");
        _invitations.Remove(invitation);
    }

    public Membership Accept(Invitation invitation, Guid userId, string? userEmail, DateTimeOffset now)
    {
        if (invitation.GroupId != Id)
        {
            throw new ArgumentException("Invitation belongs to another group.", nameof(invitation));
        }

        if (invitation.AcceptedAt is not null || invitation.ExpiresAt <= now)
        {
            throw new DomainException("invitation_invalid", "Zaproszenie wygasło albo zostało już użyte.");
        }

        if (invitation.Email is not null && !string.Equals(invitation.Email, userEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("invitation_other_email", "To zaproszenie jest dla innego adresu e-mail.");
        }

        if (MembershipOf(userId) is not null)
        {
            throw new DomainException("already_member", "Już należysz do tej grupy.");
        }

        invitation.MarkAccepted(userId, now);
        var membership = new Membership(Id, userId, invitation.Role, now);
        _memberships.Add(membership);
        return membership;
    }

    private Membership RequireManager(Guid actorId)
    {
        var membership = MembershipOf(actorId);
        if (membership is not { CanManage: true })
        {
            throw new DomainException("forbidden", "Tylko właściciel albo administrator grupy może to zrobić.");
        }

        return membership;
    }

    private void RequireAnotherOwner(Guid userId)
    {
        if (!_memberships.Any(m => m.Role == GroupRole.Owner && m.UserId != userId))
        {
            throw new DomainException("last_owner", "Grupa musi mieć co najmniej jednego właściciela. Najpierw nadaj tę rolę komuś innemu.");
        }
    }

    internal static string RequireName(string? value, string code, string message)
    {
        var trimmed = Trim(value);
        if (trimmed is null)
        {
            throw new DomainException(code, message);
        }

        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException("name_too_long", $"Nazwa może mieć najwyżej {NameMaxLength} znaków.");
        }

        return trimmed;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
