namespace PtcgpTracker.Api.CardData;

public class CardDataOptions
{
    public const string SectionName = "CardData";

    /// <summary>Pinned tag of PocketDecks/pokemon-tcg-pocket-cards, e.g. "v5.3.1". Never "main".</summary>
    public required string RepoTag { get; set; }

    public string BaseUrl { get; set; } = "https://raw.githubusercontent.com/PocketDecks/pokemon-tcg-pocket-cards";

    public int RefreshIntervalHours { get; set; } = 24;

    public string BuildCollectionPayloadUrl() => $"{BaseUrl}/{RepoTag}/data/v5/cards.collection.no-image.json";
}
