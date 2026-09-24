namespace PtcgpTracker.Api.Models;

public record TradeListShareStatusResponse(bool Enabled, string? Token);

public record SharedTradeListResponse(string DisplayName, IReadOnlyList<TradeListEntryResponse> Entries);
