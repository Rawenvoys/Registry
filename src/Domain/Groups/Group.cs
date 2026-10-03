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

    public GroupKind Kind { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<Membership> Memberships => _memberships;

    public IReadOnlyCollection<Location> Locations => _locations;

    public IReadOnlyCollection<Invitation> Invitations => _invitations;

    /// <summary>
    /// Creates a group owned by <paramref name="ownerId"/>. A business needs its first location up front;
    /// personal and household groups get a default location named after the group.
    /// </summary>
    public static Group Create(string name, GroupKind kind, Guid ownerId, string? firstLocationName, string? firstLocationAddress, DateTimeOffset now)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = RequireName(name, "group_name_required", "Podaj nazwę grupy."),
            Kind = kind,
            CreatedAt = now,
        };

        group._memberships.Add(new Membership(group.Id, ownerId, GroupRole.Owner, now));

        if (kind == GroupKind.Business)
        {
            var locationName = RequireName(firstLocationName, "location_name_required", "Firma potrzebuje pierwszej lokalizacji.");
            group._locations.Add(new Location(group.Id, locationName, Trim(firstLocationAddress), isDefault: true));
        }
        else
        {
            group._locations.Add(new Location(group.Id, group.Name, address: null, isDefault: true));
        }

        return group;
    }

    public Membership? MembershipOf(Guid userId) => _memberships.FirstOrDefault(m => m.UserId == userId);

    public Location AddLocation(Guid actorId, string name, string? address)
    {
        RequireManager(actorId);
        if (Kind != GroupKind.Business)
        {
            throw new DomainException("locations_business_only", "Kolejne lokalizacje może mieć tylko firma.");
        }

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

    private static string RequireName(string? value, string code, string message)
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
