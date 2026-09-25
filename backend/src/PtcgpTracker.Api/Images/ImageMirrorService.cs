using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PtcgpTracker.Api.CardData;

namespace PtcgpTracker.Api.Images;

public enum ImageMirrorState { Idle, Running, Completed, Failed }

public record ImageMirrorStatus(
    ImageMirrorState State,
    int Total,
    int Completed,
    int Failed,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int StoredCards,
    int StoredPacks,
    long StoredBytes,
    string? Error);

/// <summary>
/// Copies every card and pack image from the pinned pokemon-tcg-pocket-cards release onto this
/// server, so the site can serve its own art instead of every visitor's browser loading it from
/// GitHub. The list of images comes from the backend's own card catalog and the release's
/// expansions.json, never from the browser. Safe to re-run: files already on disk are skipped, so
/// a second run only fetches what is missing or failed.
/// </summary>
public partial class ImageMirrorService(
    IHttpClientFactory httpClientFactory,
    IOptions<ImageMirrorOptions> options,
    IOptions<CardDataOptions> cardDataOptions,
    ICardCatalogProvider catalog,
    IHostApplicationLifetime lifetime,
    ILogger<ImageMirrorService> logger)
{
    public const string HttpClientName = "ImageMirror";

    private readonly object _gate = new();
    private ImageMirrorState _state = ImageMirrorState.Idle;
    private int _total;
    private int _completed;
    private int _failed;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _finishedAt;
    private string? _error;
    private Task? _job;

    private string CardsDirectory => Path.Combine(options.Value.Directory, "cards");

    private string PacksDirectory => Path.Combine(options.Value.Directory, "packs");

    /// <summary>The running (or last) job, so tests can wait for it deterministically.</summary>
    public Task? CurrentJob
    {
        get { lock (_gate) return _job; }
    }

    public ImageMirrorStatus GetStatus()
    {
        var (storedCards, storedPacks, storedBytes) = ScanDisk();
        lock (_gate)
        {
            return new ImageMirrorStatus(
                _state, _total, Volatile.Read(ref _completed), Volatile.Read(ref _failed),
                _startedAt, _finishedAt, storedCards, storedPacks, storedBytes, _error);
        }
    }

    /// <summary>Starts a download in the background. Returns false if one is already running.</summary>
    public bool TryStart()
    {
        lock (_gate)
        {
            if (_state == ImageMirrorState.Running)
            {
                return false;
            }

            _state = ImageMirrorState.Running;
            _total = 0;
            Volatile.Write(ref _completed, 0);
            Volatile.Write(ref _failed, 0);
            _startedAt = DateTimeOffset.UtcNow;
            _finishedAt = null;
            _error = null;
            _job = Task.Run(() => RunAsync(lifetime.ApplicationStopping));
            return true;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(CardsDirectory);
            Directory.CreateDirectory(PacksDirectory);

            var work = await BuildWorkListAsync(cancellationToken);
            lock (_gate)
            {
                _total = work.Count + _failed;
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            await Parallel.ForEachAsync(
                work,
                new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.Value.Concurrency), CancellationToken = cancellationToken },
                async (item, token) =>
                {
                    if (await DownloadAsync(client, item, token))
                    {
                        Interlocked.Increment(ref _completed);
                    }
                    else
                    {
                        Interlocked.Increment(ref _failed);
                    }
                });

            Finish(ImageMirrorState.Completed, null);
            logger.LogInformation("Image download finished: {Completed} stored, {Failed} failed", _completed, _failed);
        }
        catch (OperationCanceledException)
        {
            Finish(ImageMirrorState.Failed, "Stopped before it finished. Start it again to continue.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image download failed");
            Finish(ImageMirrorState.Failed, ex.Message);
        }
    }

    private void Finish(ImageMirrorState state, string? error)
    {
        lock (_gate)
        {
            _state = state;
            _error = error;
            _finishedAt = DateTimeOffset.UtcNow;
        }
    }

    private record WorkItem(string Url, string Path);

    private record ExpansionRecord(string Id, List<PackRecord>? Packs);

    private record PackRecord(string Id, string? Image);

    private async Task<List<WorkItem>> BuildWorkListAsync(CancellationToken cancellationToken)
    {
        var cards = catalog.GetAll();
        if (cards.Count == 0)
        {
            throw new InvalidOperationException("The card data hasn't loaded yet. Refresh it under Card data, then try again.");
        }

        var settings = cardDataOptions.Value;
        var work = new List<WorkItem>(cards.Count + 64);

        foreach (var card in cards)
        {
            // Ids come from the trusted catalog, but they end up in a file path: only ever accept plain names.
            var prefix = card.SetCode + "-";
            if (!SafeName().IsMatch(card.Id) || !card.Id.StartsWith(prefix, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _failed);
                logger.LogWarning("Skipping card with unexpected id {CardId}", card.Id);
                continue;
            }

            work.Add(new WorkItem(
                settings.BuildCardImageUrl(card.SetCode, card.Id[prefix.Length..]),
                Path.Combine(CardsDirectory, card.Id + ".webp")));
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var expansions = await client.GetFromJsonAsync<List<ExpansionRecord>>(settings.BuildExpansionsUrl(), cancellationToken)
            ?? throw new InvalidOperationException("The list of packs could not be read.");

        // Only packs that have art; promo "packs" have none, and keep loading theirs from GitHub.
        foreach (var pack in expansions.SelectMany(expansion => expansion.Packs ?? []).Where(pack => !string.IsNullOrEmpty(pack.Image)))
        {
            if (!SafeName().IsMatch(pack.Id))
            {
                Interlocked.Increment(ref _failed);
                continue;
            }

            work.Add(new WorkItem(settings.BuildPackImageUrl(pack.Id), Path.Combine(PacksDirectory, pack.Id + ".webp")));
        }

        return work;
    }

    private async Task<bool> DownloadAsync(HttpClient client, WorkItem item, CancellationToken cancellationToken)
    {
        if (File.Exists(item.Path) && new FileInfo(item.Path).Length > 0)
        {
            return true;
        }

        var temporaryPath = item.Path + ".tmp";
        try
        {
            using var response = await client.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Could not download {Url}: HTTP {Status}", item.Url, (int)response.StatusCode);
                return false;
            }

            // Written to a temporary name first, so an interrupted download never leaves a half file
            // that a later run would mistake for a finished one.
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var destination = File.Create(temporaryPath))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            File.Move(temporaryPath, item.Path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not download {Url}", item.Url);
            TryDelete(temporaryPath);
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    private (int Cards, int Packs, long Bytes) ScanDisk()
    {
        long bytes = 0;

        int Count(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            var count = 0;
            foreach (var file in new DirectoryInfo(directory).EnumerateFiles("*.webp"))
            {
                count++;
                bytes += file.Length;
            }

            return count;
        }

        var cards = Count(CardsDirectory);
        var packs = Count(PacksDirectory);
        return (cards, packs, bytes);
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex SafeName();
}
