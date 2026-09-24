using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Endpoints;

public static class TradeListShareEndpoints
{
    public static void MapTradeListShareEndpoints(this IEndpointRouteBuilder app)
    {
        var owner = app.MapGroup("/api/trade-list/share").RequireAuthorization();

        owner.MapGet("", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var user = await GetUserAsync(principal, db);
            return Results.Ok(new TradeListShareStatusResponse(user.TradeListShareHandle is not null, user.TradeListShareHandle));
        });

        owner.MapPost("", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var user = await GetUserAsync(principal, db);

            // Idempotent: the handle isn't a secret, so re-enabling keeps the same link.
            if (user.TradeListShareHandle is null)
            {
                user.TradeListShareHandle = await ClaimUniqueHandleAsync(user, db);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new TradeListShareStatusResponse(true, user.TradeListShareHandle));
        });

        owner.MapDelete("", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var user = await GetUserAsync(principal, db);
            user.TradeListShareHandle = null;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Public: no auth. Anyone who knows a user's handle can view their two lists while
        // sharing is enabled (never edit them). Handles are stored lowercase.
        app.MapGet("/api/trade-list/shared/{handle}", async (string handle, ApplicationDbContext db) =>
        {
            var normalized = handle.ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.TradeListShareHandle == normalized);
            if (user is null)
            {
                return Results.NotFound();
            }

            var entries = await db.TradeListEntries.Where(e => e.UserId == user.Id).ToListAsync();
            return Results.Ok(new SharedTradeListResponse(
                user.DisplayName,
                entries.Select(e => new TradeListEntryResponse(e.CardId, e.Direction, e.CreatedAt)).ToList()));
        });
    }

    private static async Task<string> ClaimUniqueHandleAsync(ApplicationUser user, ApplicationDbContext db)
    {
        var baseHandle = ShareHandles.Slugify(user.DisplayName);
        var candidate = baseHandle;
        for (var suffix = 2; await db.Users.AnyAsync(u => u.TradeListShareHandle == candidate); suffix++)
        {
            candidate = $"{baseHandle}-{suffix}";
        }

        return candidate;
    }

    private static async Task<ApplicationUser> GetUserAsync(ClaimsPrincipal principal, ApplicationDbContext db)
    {
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await db.Users.FirstAsync(u => u.Id == userId);
    }
}
