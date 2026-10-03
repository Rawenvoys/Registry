using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Groups;

namespace Registry.Infrastructure.Persistence.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> group)
    {
        // Ids are created in the domain, so EF must insert children added to a loaded group instead of updating them.
        group.Property(g => g.Id).ValueGeneratedNever();
        group.Property(g => g.Name).HasMaxLength(Group.NameMaxLength);

        group.HasMany(g => g.Memberships).WithOne().HasForeignKey(m => m.GroupId);
        group.HasMany(g => g.Locations).WithOne().HasForeignKey(l => l.GroupId);
        group.HasMany(g => g.Invitations).WithOne().HasForeignKey(i => i.GroupId);
    }
}

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> membership)
    {
        membership.ToTable("Memberships");
        membership.HasKey(m => new { m.GroupId, m.UserId });
        membership.HasIndex(m => m.UserId);
        membership.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
    }
}

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> location)
    {
        location.ToTable("Locations");
        location.Property(l => l.Id).ValueGeneratedNever();
        location.Property(l => l.Name).HasMaxLength(Group.NameMaxLength);
        location.Property(l => l.Address).HasMaxLength(300);
    }
}

internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> invitation)
    {
        invitation.Property(i => i.Id).ValueGeneratedNever();
        invitation.HasIndex(i => i.Code).IsUnique();
        invitation.Property(i => i.Code).HasMaxLength(64);
        invitation.Property(i => i.Email).HasMaxLength(256);
        invitation.Property(i => i.Role).HasConversion<string>().HasMaxLength(20);
    }
}
