namespace PtcgpTracker.Api.CardData;

public record CatalogStatus(string RepoTag, int CardCount, DateTimeOffset? LastRefreshedAt, string? LastError);

/// <summary>Admin-facing view of, and control over, the cached card catalog.</summary>
public interface ICardCatalogAdmin
{
    CatalogStatus GetStatus();

    /// <summary>Re-fetches the pinned payload now. Throws on failure; the previous snapshot is kept.</summary>
    Task RefreshNowAsync(CancellationToken cancellationToken);
}
