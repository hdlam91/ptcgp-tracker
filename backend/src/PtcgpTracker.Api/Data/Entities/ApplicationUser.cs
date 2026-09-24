using Microsoft.AspNetCore.Identity;

namespace PtcgpTracker.Api.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
