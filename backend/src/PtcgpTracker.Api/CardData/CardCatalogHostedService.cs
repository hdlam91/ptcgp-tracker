using System.Collections.Immutable;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace PtcgpTracker.Api.CardData;

/// <summary>
/// Fetches the pinned collection payload from raw.githubusercontent.com on startup,
/// caches an immutable snapshot, and refreshes it on a timer. Each backend replica
/// holds its own independent copy — this is read-only public data, so there's no
/// need for shared-cache coordination, which keeps the app tier stateless.
/// </summary>
public class CardCatalogHostedService(
    IHttpClientFactory httpClientFactory,
    IOptions<CardDataOptions> options,
    ILogger<CardCatalogHostedService> logger) : BackgroundService, ICardCatalogProvider, ICardCatalogAdmin
{
    public const string HttpClientName = "CardData";

    private ImmutableDictionary<string, CollectionCardRecord> _snapshot =
        ImmutableDictionary<string, CollectionCardRecord>.Empty;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset? _lastRefreshedAt;
    private string? _lastError;

    public bool TryGetCard(string cardId, out CollectionCardRecord card) =>
        _snapshot.TryGetValue(cardId, out card!);

    public IReadOnlyCollection<CollectionCardRecord> GetAll() => _snapshot.Values.ToList();

    public CatalogStatus GetStatus() =>
        new(options.Value.RepoTag, _snapshot.Count, _lastRefreshedAt, _lastError);

    public async Task RefreshNowAsync(CancellationToken cancellationToken)
    {
        // Serialized so a manual refresh can't overlap the timer-driven one.
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _snapshot = await FetchSnapshotAsync(cancellationToken);
            _lastRefreshedAt = DateTimeOffset.UtcNow;
            _lastError = null;
            logger.LogInformation("Card catalog cache populated with {Count} cards", _snapshot.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _lastError = ex.Message;
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.RefreshIntervalHours));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await RefreshNowAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to refresh card catalog cache; keeping previous snapshot ({Count} cards)", _snapshot.Count);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task<ImmutableDictionary<string, CollectionCardRecord>> FetchSnapshotAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        var url = options.Value.BuildCollectionPayloadUrl();

        var cards = await client.GetFromJsonAsync<List<CollectionCardRecord>>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Card catalog payload at {url} deserialized to null.");

        return cards.ToImmutableDictionary(c => c.Id);
    }
}
