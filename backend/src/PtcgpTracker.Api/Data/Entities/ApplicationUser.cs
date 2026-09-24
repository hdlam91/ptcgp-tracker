using Microsoft.AspNetCore.Identity;

namespace PtcgpTracker.Api.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Unguessable token for the public, read-only trade-list share link.
    /// Null means sharing is disabled; regenerating replaces (and invalidates) it.
    /// </summary>
    public string? TradeListShareToken { get; set; }
}
