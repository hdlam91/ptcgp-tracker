using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PtcgpTracker.Api.Images;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class ImageMirrorEndpointsTests(PostgresApiFixture fixture) : IDisposable
{
    private readonly ImageMirrorStubHandler _github = new();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ptcgp-test-images-{Guid.NewGuid():N}");

    // A fresh app instance per test: its own image folder and its own downloader state, so runs can't leak into each other.
    private WebApplicationFactory<Program> CreateFactory() =>
        fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Images:Directory"] = _directory }));
            builder.ConfigureServices(services =>
                services.AddHttpClient(ImageMirrorService.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => _github));
        });

    private static async Task<JsonElement> RunToCompletionAsync(WebApplicationFactory<Program> factory, HttpClient admin)
    {
        var start = await admin.PostAsync("/api/admin/images/download", null);
        Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
        await factory.Services.GetRequiredService<ImageMirrorService>().CurrentJob!.WaitAsync(TimeSpan.FromSeconds(30));
        return await admin.GetFromJsonAsync<JsonElement>("/api/admin/images");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task Endpoints_RequireAnAdmin()
    {
        using var factory = CreateFactory();
        var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var trainer = await factory.CreateAuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/admin/images")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/admin/images/download", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trainer.GetAsync("/api/admin/images")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trainer.PostAsync("/api/admin/images/download", null)).StatusCode);
        Assert.Empty(_github.Requests);
    }

    [Fact]
    public async Task Status_BeforeAnyDownload_IsIdleAndEmpty()
    {
        using var factory = CreateFactory();
        var admin = await factory.CreateAdminClientAsync();

        var status = await admin.GetFromJsonAsync<JsonElement>("/api/admin/images");

        Assert.Equal("Idle", status.GetProperty("state").GetString());
        Assert.Equal(0, status.GetProperty("storedCards").GetInt32());
        Assert.Equal(0, status.GetProperty("storedPacks").GetInt32());
    }

    [Fact]
    public async Task Download_StoresEveryCardAndPackImage_FromThePinnedRelease_AndServesThemPublicly()
    {
        using var factory = CreateFactory();
        var admin = await factory.CreateAdminClientAsync();

        var status = await RunToCompletionAsync(factory, admin);

        // 3 cards in the fake catalog + the one pack that has art (the promo pack has none).
        Assert.Equal("Completed", status.GetProperty("state").GetString());
        Assert.Equal(4, status.GetProperty("total").GetInt32());
        Assert.Equal(4, status.GetProperty("completed").GetInt32());
        Assert.Equal(0, status.GetProperty("failed").GetInt32());
        Assert.Equal(3, status.GetProperty("storedCards").GetInt32());
        Assert.Equal(1, status.GetProperty("storedPacks").GetInt32());
        Assert.Equal(4 * ImageMirrorStubHandler.ImageBytes.Length, status.GetProperty("storedBytes").GetInt64());

        var requested = _github.Requests.ToList();
        Assert.Contains(requested, url => url.EndsWith("/v5.3.1/images/webp/cards/a1/001.webp"));
        Assert.Contains(requested, url => url.EndsWith("/v5.3.1/images/webp/cards/a2/001.webp"));
        Assert.Contains(requested, url => url.EndsWith("/v5.3.1/images/webp/packs/a1-charizard.webp"));
        Assert.DoesNotContain(requested, url => url.Contains("pa-promov1"));

        // Served to anyone (no login), as an image, cacheable.
        var visitor = factory.CreateClient();
        var card = await visitor.GetAsync("/local-images/cards/a1-001.webp");
        Assert.Equal(HttpStatusCode.OK, card.StatusCode);
        Assert.Equal("image/webp", card.Content.Headers.ContentType?.MediaType);
        Assert.Contains("public", card.Headers.CacheControl?.ToString());
        Assert.Equal(ImageMirrorStubHandler.ImageBytes, await card.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync("/local-images/packs/a1-charizard.webp")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/local-images/cards/zz-999.webp")).StatusCode);
    }

    [Fact]
    public async Task Download_CountsFailures_AndARerunOnlyFetchesWhatIsMissing()
    {
        using var factory = CreateFactory();
        var admin = await factory.CreateAdminClientAsync();
        _github.Missing.Add("/cards/a2/001.webp");

        var first = await RunToCompletionAsync(factory, admin);
        Assert.Equal("Completed", first.GetProperty("state").GetString());
        Assert.Equal(1, first.GetProperty("failed").GetInt32());
        Assert.Equal(3, first.GetProperty("completed").GetInt32());
        Assert.Equal(2, first.GetProperty("storedCards").GetInt32());

        _github.Missing.Clear();
        var second = await RunToCompletionAsync(factory, admin);
        Assert.Equal(0, second.GetProperty("failed").GetInt32());
        Assert.Equal(4, second.GetProperty("completed").GetInt32());
        Assert.Equal(3, second.GetProperty("storedCards").GetInt32());

        // a1-001 was stored the first time, so the second run must not fetch it again.
        Assert.Single(_github.Requests, url => url.EndsWith("/cards/a1/001.webp"));
        // The one that failed was asked for both times.
        Assert.Equal(2, _github.Requests.Count(url => url.EndsWith("/cards/a2/001.webp")));
    }

    [Fact]
    public async Task Download_WhileOneIsRunning_ReturnsConflict()
    {
        using var factory = CreateFactory();
        var admin = await factory.CreateAdminClientAsync();
        _github.Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Assert.Equal(HttpStatusCode.Accepted, (await admin.PostAsync("/api/admin/images/download", null)).StatusCode);
        var running = await admin.GetFromJsonAsync<JsonElement>("/api/admin/images");
        Assert.Equal("Running", running.GetProperty("state").GetString());

        var second = await admin.PostAsync("/api/admin/images/download", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        _github.Gate.SetResult();
        await factory.Services.GetRequiredService<ImageMirrorService>().CurrentJob!.WaitAsync(TimeSpan.FromSeconds(30));
        var done = await admin.GetFromJsonAsync<JsonElement>("/api/admin/images");
        Assert.Equal("Completed", done.GetProperty("state").GetString());
    }

    [Fact]
    public async Task Download_WhenThePackListCantBeRead_FailsWithAMessage_ThenSucceedsOnRetry()
    {
        using var factory = CreateFactory();
        var admin = await factory.CreateAdminClientAsync();
        _github.FailPackList = true;

        var failed = await RunToCompletionAsync(factory, admin);
        Assert.Equal("Failed", failed.GetProperty("state").GetString());
        Assert.False(string.IsNullOrWhiteSpace(failed.GetProperty("error").GetString()));

        _github.FailPackList = false;
        var retried = await RunToCompletionAsync(factory, admin);
        Assert.Equal("Completed", retried.GetProperty("state").GetString());
        Assert.Equal(JsonValueKind.Null, retried.GetProperty("error").ValueKind);
    }
}
