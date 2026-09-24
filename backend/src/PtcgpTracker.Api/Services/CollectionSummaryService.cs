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
    /// </summary>
    public IReadOnlyList<SetSummaryResponse> Summarize(IEnumerable<CollectionEntry> entries)
    {
        var bySet = new Dictionary<string, (int UniqueCards, int CopiesTotal)>();

        foreach (var entry in entries)
        {
            if (!catalog.TryGetCard(entry.CardId, out var card))
            {
                continue;
            }

            var current = bySet.GetValueOrDefault(card.SetCode);
            bySet[card.SetCode] = (current.UniqueCards + 1, current.CopiesTotal + entry.OwnedCount);
        }

        return bySet
            .Select(kv => new SetSummaryResponse(kv.Key, kv.Value.UniqueCards, kv.Value.CopiesTotal))
            .ToList();
    }
}
