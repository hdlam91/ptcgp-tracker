using PtcgpTracker.Api.CardData;

namespace PtcgpTracker.Api.Tests.Infrastructure;

/// <summary>
/// Small, fixed catalog used by endpoint integration tests instead of the real
/// network-backed catalog, so tests don't depend on GitHub availability.
/// </summary>
internal class FakeCardCatalogProvider : ICardCatalogProvider, ICardCatalogAdmin
{
    public static readonly CollectionCardRecord CardA1_001 = new(
        "a1-001", "Bulbasaur", "a1", "Genetic Apex", "Mewtwo", "2024-10-30", "◊", 35,
        null, "Narumi Sato", null, [], false, false, false, null, true, true, 0);

    public static readonly CollectionCardRecord CardA1_002 = new(
        "a1-002", "Ivysaur", "a1", "Genetic Apex", "Mewtwo", "2024-10-30", "◊◊", 70,
        null, "Kurata So", null, [], false, false, false, null, true, true, 0);

    public static readonly CollectionCardRecord CardA2_001 = new(
        "a2-001", "Oddish", "a2", "Space-Time Smackdown", "Palkia", "2024-12-01", "◊", 35,
        null, "Someone", null, [], false, false, false, null, true, true, 0);

    // A genuine reprint of CardA1_001 (same rarity/art_style/shiny/artist) — should merge with it.
    // Not part of the default fixture (would change the card count every other test in the
    // collection already asserts on) — pass these explicitly via the constructor where needed,
    // e.g. CollectionEndpointsTests' own WithWebHostBuilder override.
    public static readonly CollectionCardRecord CardA4b_001 = new(
        "a4b-001", "Bulbasaur", "a4b", "Deluxe Pack: ex", "Deluxe Pack: ex", "2025-09-30", "◊", 35,
        null, "Narumi Sato", null, [new AlternateVersion("a1", "Genetic Apex", 1, "◊")],
        false, false, false, null, true, true, 0);

    // The same pack's own reverse-foil pull of CardA4b_001 — same rarity and artist, but a
    // different art_style, so it must NOT merge with CardA1_001 despite being listed as its alternate.
    public static readonly CollectionCardRecord CardA4b_002 = new(
        "a4b-002", "Bulbasaur", "a4b", "Deluxe Pack: ex", "Deluxe Pack: ex", "2025-09-30", "◊", 35,
        "Parallel Foil", "Narumi Sato", null, [new AlternateVersion("a1", "Genetic Apex", 1, "◊")],
        false, false, false, null, true, true, 0);

    public static readonly CollectionCardRecord[] DefaultCards = [CardA1_001, CardA1_002, CardA2_001];

    private readonly Dictionary<string, CollectionCardRecord> _cards;
    private readonly CardEquivalenceGrouping.Result _equivalence;

    // A required (not optional/nullable) parameter deliberately: the DI container treats
    // IEnumerable<T> specially and always resolves it (to an empty list, since CollectionCardRecord
    // isn't itself a registered service) rather than ever falling back to a C# default value — so
    // every registration must supply cards explicitly via a factory delegate, never bare
    // AddSingleton<FakeCardCatalogProvider>(), or it silently ends up with zero cards.
    public FakeCardCatalogProvider(IEnumerable<CollectionCardRecord> cards)
    {
        _cards = cards.ToDictionary(c => c.Id);
        _equivalence = CardEquivalenceGrouping.Compute(_cards.Values);
    }

    public bool TryGetCard(string cardId, out CollectionCardRecord card) =>
        _cards.TryGetValue(cardId, out card!);

    public IReadOnlyCollection<CollectionCardRecord> GetAll() => _cards.Values.ToList();

    public string ResolveCanonicalId(string cardId) =>
        _equivalence.CanonicalIds.GetValueOrDefault(cardId, cardId);

    public IReadOnlyCollection<string> GetEquivalentIds(string cardId)
    {
        var canonicalId = ResolveCanonicalId(cardId);
        return _equivalence.GroupMembers.TryGetValue(canonicalId, out var members) ? members : [cardId];
    }

    /// <summary>Set to make the next refreshes fail, like an unreachable GitHub would.</summary>
    public Exception? RefreshFailure { get; set; }

    public int RefreshCount { get; private set; }

    public DateTimeOffset? LastRefreshedAt { get; private set; }

    public CatalogStatus GetStatus() => new("v-test", _cards.Count, LastRefreshedAt, RefreshFailure?.Message);

    public Task RefreshNowAsync(CancellationToken cancellationToken)
    {
        if (RefreshFailure is not null)
        {
            throw RefreshFailure;
        }

        RefreshCount++;
        LastRefreshedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }
}
