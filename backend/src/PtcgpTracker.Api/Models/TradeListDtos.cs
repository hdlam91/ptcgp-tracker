using PtcgpTracker.Api.Data.Entities;

namespace PtcgpTracker.Api.Models;

public record TradeListEntryResponse(string CardId, TradeDirection Direction, DateTimeOffset CreatedAt);

public record CreateTradeListEntryRequest(string CardId, TradeDirection Direction);
