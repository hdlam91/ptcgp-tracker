using System.Text.Json.Serialization;

namespace PtcgpTracker.Api.CardData;

/// <summary>
/// Mirrors one record of the pokemon-tcg-pocket-cards "collection" payload
/// (data/v5/cards.collection.no-image.json at the pinned tag). Records are sparse:
/// a field is simply absent from the JSON when it doesn't apply to that card
/// (e.g. promo cards have no release_date or pack_points).
/// </summary>
public record CollectionCardRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("set_code")] string SetCode,
    [property: JsonPropertyName("set_name")] string SetName,
    [property: JsonPropertyName("pack")] string? Pack,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("rarity")] string? Rarity,
    [property: JsonPropertyName("pack_points")] int? PackPoints,
    [property: JsonPropertyName("art_style")] string? ArtStyle,
    [property: JsonPropertyName("artist")] string? Artist,
    [property: JsonPropertyName("flavour_text")] string? FlavourText,
    [property: JsonPropertyName("alternate_versions")] IReadOnlyList<AlternateVersion> AlternateVersions,
    [property: JsonPropertyName("ex")] bool Ex,
    [property: JsonPropertyName("mega")] bool Mega,
    [property: JsonPropertyName("shiny")] bool Shiny,
    [property: JsonPropertyName("special_tags")] IReadOnlyList<string>? SpecialTags,
    [property: JsonPropertyName("tradable")] bool Tradable,
    [property: JsonPropertyName("sharable")] bool Sharable,
    [property: JsonPropertyName("trade_cost")] int? TradeCost);

/// <summary>
/// Note: unlike the top-level record's "id" (a string like "a1-001"), the alternate
/// version's "id" is the bare integer card number within that set.
/// </summary>
public record AlternateVersion(
    [property: JsonPropertyName("set_code")] string SetCode,
    [property: JsonPropertyName("set_name")] string SetName,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("rarity")] string? Rarity);
