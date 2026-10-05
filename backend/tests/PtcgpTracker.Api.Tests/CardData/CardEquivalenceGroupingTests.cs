using PtcgpTracker.Api.CardData;

namespace PtcgpTracker.Api.Tests.CardData;

public class CardEquivalenceGroupingTests
{
    private static CollectionCardRecord Card(
        string id,
        string setCode,
        string? releaseDate,
        string rarity,
        string? artStyle,
        bool shiny,
        string artist,
        params (string SetCode, int Id, string? Rarity)[] alternates)
    {
        var alternateVersions = alternates
            .Select(a => new AlternateVersion(a.SetCode, a.SetCode, a.Id, a.Rarity))
            .ToList();

        return new CollectionCardRecord(
            id, "Test Card", setCode, setCode, null, releaseDate, rarity, 35,
            artStyle, artist, null, alternateVersions, false, false, shiny, null, true, true, 0);
    }

    [Fact]
    public void Compute_MergesReprintsWithIdenticalRarityArtStyleShinyAndArtist()
    {
        // Mirrors the real a1-001 / a4b-001 pair: same rarity, no art_style, not shiny, same artist.
        var a1001 = Card("a1-001", "a1", "2024-10-30", "◊", null, false, "Narumi Sato", ("a4b", 1, "◊"));
        var a4b001 = Card("a4b-001", "a4b", "2025-09-30", "◊", null, false, "Narumi Sato", ("a1", 1, "◊"));

        var result = CardEquivalenceGrouping.Compute([a1001, a4b001]);

        Assert.Equal("a1-001", result.CanonicalIds["a1-001"]);
        Assert.Equal("a1-001", result.CanonicalIds["a4b-001"]);
        Assert.Equal(["a1-001", "a4b-001"], result.GroupMembers["a1-001"].ToArray());
    }

    [Fact]
    public void Compute_DoesNotMergeWhenArtStyleDiffers()
    {
        // Mirrors a1-001 vs a4b-002: a4b-002 is a reverse foil of a4b-001, not a1-001 itself.
        var a1001 = Card("a1-001", "a1", "2024-10-30", "◊", null, false, "Narumi Sato", ("a4b", 2, "◊"));
        var a4b002 = Card("a4b-002", "a4b", "2025-09-30", "◊", "Parallel Foil", false, "Narumi Sato", ("a1", 1, "◊"));

        var result = CardEquivalenceGrouping.Compute([a1001, a4b002]);

        Assert.Equal("a1-001", result.CanonicalIds["a1-001"]);
        Assert.Equal("a4b-002", result.CanonicalIds["a4b-002"]);
    }

    [Fact]
    public void Compute_DoesNotMergeWhenRarityDiffers()
    {
        // A common pulled normally vs. the same Pokémon's rarer star pull within another set.
        var common = Card("a1-001", "a1", "2024-10-30", "◊", null, false, "Narumi Sato", ("a1", 227, "☆"));
        var starPull = Card("a1-227", "a1", "2024-10-30", "☆", null, false, "Narumi Sato", ("a1", 1, "◊"));

        var result = CardEquivalenceGrouping.Compute([common, starPull]);

        Assert.Equal("a1-001", result.CanonicalIds["a1-001"]);
        Assert.Equal("a1-227", result.CanonicalIds["a1-227"]);
    }

    [Fact]
    public void Compute_DoesNotMergeWhenShinyDiffers()
    {
        // Shiny and non-shiny prints can carry the same rarity symbol; the shiny flag must
        // still be checked, or they'd incorrectly merge.
        var regular = Card("a2-001", "a2", "2024-12-01", "☆", null, false, "Someone", ("a2", 2, "☆"));
        var shinyVersion = Card("a2-002", "a2", "2024-12-01", "☆", null, true, "Someone", ("a2", 1, "☆"));

        var result = CardEquivalenceGrouping.Compute([regular, shinyVersion]);

        Assert.Equal("a2-001", result.CanonicalIds["a2-001"]);
        Assert.Equal("a2-002", result.CanonicalIds["a2-002"]);
    }

    [Fact]
    public void Compute_DoesNotMergeWhenArtistDiffers()
    {
        // Mirrors pa-009 vs pa-015: same rarity ("Promo"), no art_style, not shiny, but a
        // different illustrator — a genuinely different card, not just a different promo code.
        var pa009 = Card("pa-009", "pa", null, "Promo", null, false, "Atsushi Furusawa", ("pa", 15, "Promo"));
        var pa015 = Card("pa-015", "pa", null, "Promo", null, false, "Kouki Saitou", ("pa", 9, "Promo"));

        var result = CardEquivalenceGrouping.Compute([pa009, pa015]);

        Assert.Equal("pa-009", result.CanonicalIds["pa-009"]);
        Assert.Equal("pa-015", result.CanonicalIds["pa-015"]);
    }

    [Fact]
    public void Compute_MergesSameArtistPromoDrops_EvenWithNoReleaseDate()
    {
        // Mirrors pa-015 / pa-026: two promo codes for the exact same illustrated card, neither
        // with a release_date — canonical id must fall back to the lowest id string.
        var pa015 = Card("pa-015", "pa", null, "Promo", null, false, "Kouki Saitou", ("pa", 26, "Promo"));
        var pa026 = Card("pa-026", "pa", null, "Promo", null, false, "Kouki Saitou", ("pa", 15, "Promo"));

        var result = CardEquivalenceGrouping.Compute([pa026, pa015]);

        Assert.Equal("pa-015", result.CanonicalIds["pa-015"]);
        Assert.Equal("pa-015", result.CanonicalIds["pa-026"]);
    }

    [Fact]
    public void Compute_PrefersEarliestReleaseDate_OverLowestIdString()
    {
        // "a1-001" would sort before "a4b-001" lexicographically, but a1-001's earlier
        // release_date should still win the tie-break, not alphabetical order.
        var later = Card("a4b-001", "a4b", "2025-09-30", "◊", null, false, "Narumi Sato", ("a1", 1, "◊"));
        var earlier = Card("a1-001", "a1", "2024-10-30", "◊", null, false, "Narumi Sato", ("a4b", 1, "◊"));

        var result = CardEquivalenceGrouping.Compute([later, earlier]);

        Assert.Equal("a1-001", result.CanonicalIds["a1-001"]);
        Assert.Equal("a1-001", result.CanonicalIds["a4b-001"]);
    }

    [Fact]
    public void Compute_CardWithNoAlternates_IsItsOwnSingletonGroup()
    {
        var lone = Card("a2-001", "a2", "2024-12-01", "◊", null, false, "Someone");

        var result = CardEquivalenceGrouping.Compute([lone]);

        Assert.Equal("a2-001", result.CanonicalIds["a2-001"]);
        Assert.Equal(["a2-001"], result.GroupMembers["a2-001"].ToArray());
    }
}
