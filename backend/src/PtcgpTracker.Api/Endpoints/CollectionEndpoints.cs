using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Endpoints;

public static class CollectionEndpoints
{
    public static void MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collection").RequireAuthorization();

        group.MapGet("", async (
            string? setCode,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            ICardCatalogProvider catalog) =>
        {
            var userId = GetUserId(principal);
            var entries = await db.CollectionEntries.Where(e => e.UserId == userId).ToListAsync();

            if (setCode is not null)
            {
                entries = entries
                    .Where(e => catalog.TryGetCard(e.CardId, out var card) && card.SetCode == setCode)
                    .ToList();
            }

            return Results.Ok(entries
                .Select(e => new CollectionEntryResponse(e.CardId, e.OwnedCount, e.UpdatedAt))
                .ToList());
        });

        group.MapGet("/summary", async (
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            CollectionSummaryService summaryService) =>
        {
            var userId = GetUserId(principal);
            var entries = await db.CollectionEntries.Where(e => e.UserId == userId).ToListAsync();
            return Results.Ok(summaryService.Summarize(entries));
        });

        group.MapPut("/{cardId}", async (
            string cardId,
            UpsertCollectionEntryRequest request,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            ICardCatalogProvider catalog) =>
        {
            if (!catalog.TryGetCard(cardId, out _))
            {
                return Results.BadRequest(new { error = $"Unknown cardId '{cardId}'." });
            }

            if (request.OwnedCount < 0)
            {
                return Results.BadRequest(new { error = "ownedCount must be >= 0." });
            }

            var userId = GetUserId(principal);
            var entry = await db.CollectionEntries
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CardId == cardId);

            if (request.OwnedCount == 0)
            {
                if (entry is not null)
                {
                    db.CollectionEntries.Remove(entry);
                    await db.SaveChangesAsync();
                }
                return Results.Ok(new CollectionEntryResponse(cardId, 0, DateTimeOffset.UtcNow));
            }

            if (entry is null)
            {
                entry = new CollectionEntry { UserId = userId, CardId = cardId, OwnedCount = request.OwnedCount };
                db.CollectionEntries.Add(entry);
            }
            else
            {
                entry.OwnedCount = request.OwnedCount;
                entry.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new CollectionEntryResponse(entry.CardId, entry.OwnedCount, entry.UpdatedAt));
        });

        group.MapDelete("/{cardId}", async (
            string cardId,
            ClaimsPrincipal principal,
            ApplicationDbContext db) =>
        {
            var userId = GetUserId(principal);
            var entry = await db.CollectionEntries
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CardId == cardId);

            if (entry is not null)
            {
                db.CollectionEntries.Remove(entry);
                await db.SaveChangesAsync();
            }

            return Results.NoContent();
        });
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
