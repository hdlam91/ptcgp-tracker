namespace PtcgpTracker.Api.Models;

public record CollectionEntryResponse(string CardId, int OwnedCount, DateTimeOffset UpdatedAt);

public record SetSummaryResponse(string SetCode, int OwnedUniqueCards, int OwnedCopiesTotal);

public record UpsertCollectionEntryRequest(int OwnedCount);
