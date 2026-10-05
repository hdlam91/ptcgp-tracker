namespace PtcgpTracker.Api.CardData;

/// <summary>
/// Read-only access to the cached card catalog. The BFF uses this only to validate
/// cardId values on writes and to compute per-set counts — it never returns catalog
/// fields (name, image, rarity, ...) to the frontend, which gets those from the
/// pokemon-tcg-pocket-cards npm package directly.
/// </summary>
public interface ICardCatalogProvider
{
    bool TryGetCard(string cardId, out CollectionCardRecord card);

    IReadOnlyCollection<CollectionCardRecord> GetAll();

    /// <summary>
    /// The id that collection ownership is actually stored under for this card — itself, unless
    /// it's a reprint of another card (see <see cref="CardEquivalenceGrouping"/>), in which case
    /// every reprint in the group shares one canonical id. Callers validate with
    /// <see cref="TryGetCard"/> first; this assumes the id is already known.
    /// </summary>
    string ResolveCanonicalId(string cardId);

    /// <summary>Every id that shares the same canonical id as this one, including itself.</summary>
    IReadOnlyCollection<string> GetEquivalentIds(string cardId);
}
