using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data;

namespace PtcgpTracker.Api.Services;

/// <summary>
/// Collapses collection rows that predate (or were written before a dataset update revealed) a
/// card's equivalence group into one row per group, per user. Idempotent and safe to re-run: a
/// user with no duplicates is untouched, and if a future dataset update ever changes which id is
/// canonical for an existing group, running this again re-converges storage onto the new one.
/// </summary>
public class CollectionMergeService(ApplicationDbContext db, ICardCatalogProvider catalog)
{
    public async Task MergeDuplicatesAsync()
    {
        var entries = await db.CollectionEntries.ToListAsync();

        var groups = entries.GroupBy(e => (e.UserId, CanonicalId: ResolveCanonicalId(e.CardId)));

        foreach (var group in groups)
        {
            var rows = group.ToList();

            // The same physical card can't have been owned more times than the highest count any
            // one of its duplicate rows already recorded — summing them would double-count it.
            var survivor = rows.OrderByDescending(r => r.OwnedCount).First();
            survivor.CardId = group.Key.CanonicalId;

            foreach (var duplicate in rows.Where(r => r.Id != survivor.Id))
            {
                db.CollectionEntries.Remove(duplicate);
            }
        }

        await db.SaveChangesAsync();
    }

    private string ResolveCanonicalId(string cardId) =>
        catalog.TryGetCard(cardId, out _) ? catalog.ResolveCanonicalId(cardId) : cardId;
}
