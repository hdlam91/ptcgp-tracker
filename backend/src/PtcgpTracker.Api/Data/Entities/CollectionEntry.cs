namespace PtcgpTracker.Api.Data.Entities;

public class CollectionEntry
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    /// <summary>Stable card ID from the pokemon-tcg-pocket-cards dataset, e.g. "a1-001".</summary>
    public required string CardId { get; set; }

    public required int OwnedCount { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
