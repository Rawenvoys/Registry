using System.Security.Cryptography;

namespace Registry.Domain.Groups;

/// <summary>A code or link that lets someone join a group with a given role.</summary>
public class Invitation
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private Invitation()
    {
    }

    internal Invitation(Guid groupId, string? email, GroupRole role, Guid createdBy, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        GroupId = groupId;
        Email = email;
        Role = role;
        Code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        CreatedBy = createdBy;
        CreatedAt = now;
        ExpiresAt = now + Lifetime;
    }

    public Guid Id { get; private set; }

    public Guid GroupId { get; private set; }

    /// <summary>When set, only the account with this email can accept.</summary>
    public string? Email { get; private set; }

    public GroupRole Role { get; private set; }

    public string Code { get; private set; } = null!;

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public Guid? AcceptedBy { get; private set; }

    internal void MarkAccepted(Guid userId, DateTimeOffset now)
    {
        AcceptedAt = now;
        AcceptedBy = userId;
    }
}
