using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;

namespace PtcgpTracker.Api.Services;

public class CollectionSummaryService(ICardCatalogProvider catalog)
{
    /// <summary>
    /// Groups a user's collection entries by set_code and counts unique cards owned
    /// and total copies owned. Entries whose cardId isn't in the current catalog
    /// snapshot (a version-drift edge case) are skipped rather than thrown away.
    ///
    /// A stored entry is keyed by its equivalence group's canonical id, but owning it counts
    /// toward every set that has a reprint in that group (e.g. owning a card only ever pulled
    /// from a Deluxe Pack still completes the earlier set it was reprinted from) — so each entry
    /// is counted once per set among its equivalent ids, not just the canonical id's own set.
    /// </summary>
    public IReadOnlyList<SetSummaryResponse> Summarize(IEnumerable<CollectionEntry> entries)
    {
        var bySet = new Dictionary<string, (int UniqueCards, int CopiesTotal)>();

        foreach (var entry in entries)
        {
            var setCodes = catalog.GetEquivalentIds(entry.CardId)
                .Select(id => catalog.TryGetCard(id, out var card) ? card.SetCode : null)
                .Where(setCode => setCode is not null)
                .Distinct();

            foreach (var setCode in setCodes)
            {
                var current = bySet.GetValueOrDefault(setCode!);
                bySet[setCode!] = (current.UniqueCards + 1, current.CopiesTotal + entry.OwnedCount);
            }
        }

        return bySet
            .Select(kv => new SetSummaryResponse(kv.Key, kv.Value.UniqueCards, kv.Value.CopiesTotal))
            .ToList();
    }
}
