namespace PtcgpTracker.Api.Models;

public record TradeListShareStatusResponse(bool Enabled, string? Handle);

public record SharedTradeListResponse(string DisplayName, IReadOnlyList<TradeListEntryResponse> Entries);
