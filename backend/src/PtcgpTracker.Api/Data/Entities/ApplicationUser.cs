using Microsoft.AspNetCore.Identity;

namespace PtcgpTracker.Api.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// URL-safe, unique handle for the public, read-only trade-list share link
    /// (/share/{handle}), derived from the display name. Null means sharing is disabled.
    /// </summary>
    public string? TradeListShareHandle { get; set; }
}
