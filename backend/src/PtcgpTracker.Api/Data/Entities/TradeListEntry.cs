namespace PtcgpTracker.Api.Data.Entities;

public class TradeListEntry
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    /// <summary>Stable card ID from the pokemon-tcg-pocket-cards dataset, e.g. "a1-001".</summary>
    public required string CardId { get; set; }

    public required TradeDirection Direction { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
