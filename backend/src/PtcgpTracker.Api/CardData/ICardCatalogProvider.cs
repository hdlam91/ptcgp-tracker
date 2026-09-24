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
}
