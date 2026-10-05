using System.Collections.Immutable;

namespace PtcgpTracker.Api.CardData;

/// <summary>
/// Two cards are the same real-world card if they're cross-referenced via alternate_versions and
/// share identical rarity, art_style, shiny, and artist — the fields that distinguish a genuine
/// reprint of the same card (typically across a "Deluxe Pack"-style compilation set) from a
/// different print of it (a rarer pull, a foil variant, or a promo illustrated by someone else).
/// No set/pack code is ever referenced, so this generalizes to any future compilation set without
/// change.
/// </summary>
public static class CardEquivalenceGrouping
{
    public readonly record struct Result(
        IReadOnlyDictionary<string, string> CanonicalIds,
        IReadOnlyDictionary<string, ImmutableArray<string>> GroupMembers);

    public static Result Compute(IEnumerable<CollectionCardRecord> cards)
    {
        var byId = cards.ToDictionary(c => c.Id);
        var parent = byId.Keys.ToDictionary(id => id, id => id);

        string Find(string id)
        {
            while (parent[id] != id)
            {
                id = parent[id];
            }

            return id;
        }

        void Union(string a, string b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA != rootB)
            {
                parent[rootA] = rootB;
            }
        }

        foreach (var card in byId.Values)
        {
            foreach (var alt in card.AlternateVersions)
            {
                var otherId = $"{alt.SetCode}-{alt.Id:D3}";
                if (byId.TryGetValue(otherId, out var other) && IsSameCard(card, other))
                {
                    Union(card.Id, otherId);
                }
            }
        }

        var canonicalIds = new Dictionary<string, string>();
        var groupMembers = new Dictionary<string, ImmutableArray<string>>();

        foreach (var group in byId.Keys.GroupBy(Find))
        {
            var members = group.OrderBy(id => id, StringComparer.Ordinal).ToImmutableArray();

            // Earliest release_date wins; nulls (promo cards, mostly) sort last. Falls back to the
            // lowest id string when every member lacks a date, or on a genuine date tie.
            var canonicalId = members
                .OrderBy(id => byId[id].ReleaseDate is null)
                .ThenBy(id => byId[id].ReleaseDate, StringComparer.Ordinal)
                .ThenBy(id => id, StringComparer.Ordinal)
                .First();

            foreach (var id in members)
            {
                canonicalIds[id] = canonicalId;
            }

            groupMembers[canonicalId] = members;
        }

        return new Result(canonicalIds, groupMembers);
    }

    private static bool IsSameCard(CollectionCardRecord a, CollectionCardRecord b) =>
        a.Rarity == b.Rarity && a.ArtStyle == b.ArtStyle && a.Shiny == b.Shiny && a.Artist == b.Artist;
}
