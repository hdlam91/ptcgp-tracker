using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;

namespace PtcgpTracker.Api.Endpoints;

public static class TradeListEndpoints
{
    public static void MapTradeListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trade-list").RequireAuthorization();

        group.MapGet("", async (
            TradeDirection? direction,
            ClaimsPrincipal principal,
            ApplicationDbContext db) =>
        {
            var userId = GetUserId(principal);
            var query = db.TradeListEntries.Where(e => e.UserId == userId);

            if (direction is not null)
            {
                query = query.Where(e => e.Direction == direction);
            }

            var entries = await query.ToListAsync();
            return Results.Ok(entries
                .Select(e => new TradeListEntryResponse(e.CardId, e.Direction, e.CreatedAt))
                .ToList());
        });

        group.MapPost("", async (
            CreateTradeListEntryRequest request,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            ICardCatalogProvider catalog) =>
        {
            if (!catalog.TryGetCard(request.CardId, out _))
            {
                return Results.BadRequest(new { error = $"Unknown cardId '{request.CardId}'." });
            }

            var userId = GetUserId(principal);
            var exists = await db.TradeListEntries.AnyAsync(e =>
                e.UserId == userId && e.CardId == request.CardId && e.Direction == request.Direction);

            if (exists)
            {
                return Results.Conflict(new { error = "This card is already on that list." });
            }

            var entry = new TradeListEntry
            {
                UserId = userId,
                CardId = request.CardId,
                Direction = request.Direction,
            };
            db.TradeListEntries.Add(entry);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/trade-list/{entry.CardId}/{entry.Direction}",
                new TradeListEntryResponse(entry.CardId, entry.Direction, entry.CreatedAt));
        });

        group.MapDelete("/{cardId}/{direction}", async (
            string cardId,
            TradeDirection direction,
            ClaimsPrincipal principal,
            ApplicationDbContext db) =>
        {
            var userId = GetUserId(principal);
            var entry = await db.TradeListEntries.FirstOrDefaultAsync(e =>
                e.UserId == userId && e.CardId == cardId && e.Direction == direction);

            if (entry is not null)
            {
                db.TradeListEntries.Remove(entry);
                await db.SaveChangesAsync();
            }

            return Results.NoContent();
        });
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
