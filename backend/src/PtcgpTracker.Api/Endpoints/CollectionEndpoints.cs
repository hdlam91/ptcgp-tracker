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

            // Each stored row is the canonical id of a card's equivalence group; expand it back out
            // to every reprint in that group so a card pulled from a different pack than the one
            // stored still shows as owned wherever it's rendered.
            var expanded = entries
                .SelectMany(e => catalog.GetEquivalentIds(e.CardId)
                    .Select(id => new CollectionEntryResponse(id, e.OwnedCount, e.UpdatedAt)));

            if (setCode is not null)
            {
                expanded = expanded.Where(e => catalog.TryGetCard(e.CardId, out var card) && card.SetCode == setCode);
            }

            return Results.Ok(expanded.ToList());
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

            // Ownership is tracked once per equivalence group (e.g. a reprint across a Deluxe
            // Pack), under whichever id is that group's canonical one — never the id the caller
            // happened to click.
            var canonicalId = catalog.ResolveCanonicalId(cardId);
            var equivalentIds = catalog.GetEquivalentIds(cardId);

            var userId = GetUserId(principal);
            var entry = await db.CollectionEntries
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CardId == canonicalId);

            if (request.OwnedCount == 0)
            {
                if (entry is not null)
                {
                    db.CollectionEntries.Remove(entry);
                    await db.SaveChangesAsync();
                }

                var clearedAt = DateTimeOffset.UtcNow;
                return Results.Ok(equivalentIds.Select(id => new CollectionEntryResponse(id, 0, clearedAt)).ToList());
            }

            if (entry is null)
            {
                entry = new CollectionEntry { UserId = userId, CardId = canonicalId, OwnedCount = request.OwnedCount };
                db.CollectionEntries.Add(entry);
            }
            else
            {
                entry.OwnedCount = request.OwnedCount;
                entry.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync();

            return Results.Ok(equivalentIds
                .Select(id => new CollectionEntryResponse(id, entry.OwnedCount, entry.UpdatedAt))
                .ToList());
        });

        group.MapDelete("/{cardId}", async (
            string cardId,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            ICardCatalogProvider catalog) =>
        {
            var canonicalId = catalog.TryGetCard(cardId, out _) ? catalog.ResolveCanonicalId(cardId) : cardId;

            var userId = GetUserId(principal);
            var entry = await db.CollectionEntries
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CardId == canonicalId);

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
