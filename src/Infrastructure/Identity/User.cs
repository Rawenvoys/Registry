using Microsoft.AspNetCore.Identity;

namespace Registry.Infrastructure.Identity;

public class User : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
