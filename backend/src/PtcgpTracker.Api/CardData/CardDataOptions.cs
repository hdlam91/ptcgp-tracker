namespace PtcgpTracker.Api.CardData;

public class CardDataOptions
{
    public const string SectionName = "CardData";

    /// <summary>Pinned tag of PocketDecks/pokemon-tcg-pocket-cards, e.g. "v5.3.1". Never "main".</summary>
    public required string RepoTag { get; set; }

    public string BaseUrl { get; set; } = "https://raw.githubusercontent.com/PocketDecks/pokemon-tcg-pocket-cards";

    public int RefreshIntervalHours { get; set; } = 24;

    public string BuildCollectionPayloadUrl() => $"{BaseUrl}/{RepoTag}/data/v5/cards.collection.no-image.json";

    public string BuildExpansionsUrl() => $"{BaseUrl}/{RepoTag}/data/v5/expansions.json";

    /// <summary>Card art at the pinned release, e.g. set "a1" number "001".</summary>
    public string BuildCardImageUrl(string setCode, string number) => $"{BaseUrl}/{RepoTag}/images/webp/cards/{setCode}/{number}.webp";

    public string BuildPackImageUrl(string packId) => $"{BaseUrl}/{RepoTag}/images/webp/packs/{packId}.webp";
}
