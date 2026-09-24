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

    private readonly Dictionary<string, CollectionCardRecord> _cards = new[]
    {
        CardA1_001, CardA1_002, CardA2_001,
    }.ToDictionary(c => c.Id);

    public bool TryGetCard(string cardId, out CollectionCardRecord card) =>
        _cards.TryGetValue(cardId, out card!);

    public IReadOnlyCollection<CollectionCardRecord> GetAll() => _cards.Values.ToList();

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
