using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;

namespace PtcgpTracker.Api.Endpoints;

public static class TradeListShareEndpoints
{
    public static void MapTradeListShareEndpoints(this IEndpointRouteBuilder app)
    {
        var owner = app.MapGroup("/api/trade-list/share").RequireAuthorization();

        owner.MapGet("", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var user = await GetUserAsync(principal, db);
            return Results.Ok(new TradeListShareStatusResponse(user.TradeListShareToken is not null, user.TradeListShareToken));
        });

        owner.MapPost("", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var user = await GetUserAsync(principal, db);
            user.TradeListShareToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            await db.SaveChangesAsync();
            return Results.Ok(new TradeListShareStatusResponse(true, user.TradeListShareToken));
        });

        owner.MapDelete("", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var user = await GetUserAsync(principal, db);
            user.TradeListShareToken = null;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Public: no auth. Anyone with the token can view the two lists (never edit them).
        app.MapGet("/api/trade-list/shared/{token}", async (string token, ApplicationDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.TradeListShareToken == token);
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

    private static async Task<ApplicationUser> GetUserAsync(ClaimsPrincipal principal, ApplicationDbContext db)
    {
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await db.Users.FirstAsync(u => u.Id == userId);
    }
}
