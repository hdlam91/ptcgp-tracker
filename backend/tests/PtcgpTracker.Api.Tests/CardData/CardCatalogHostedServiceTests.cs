using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PtcgpTracker.Api.CardData;

namespace PtcgpTracker.Api.Tests.CardData;

public class CardCatalogHostedServiceTests
{
    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "CardData", "Fixtures", "collection.sample.json");

    private static CardCatalogHostedService CreateService(HttpStatusCode statusCode, string content)
    {
        var options = Options.Create(new CardDataOptions { RepoTag = "v5.3.1" });
        var factory = new StubHttpClientFactory(new StubHttpMessageHandler(statusCode, content));
        return new CardCatalogHostedService(factory, options, NullLogger<CardCatalogHostedService>.Instance);
    }

    [Fact]
    public async Task FetchSnapshotAsync_ParsesAllFixtureCards()
    {
        var json = await File.ReadAllTextAsync(FixturePath);
        var service = CreateService(HttpStatusCode.OK, json);

        var snapshot = await service.FetchSnapshotAsync(CancellationToken.None);

        Assert.Equal(4, snapshot.Count);
        Assert.True(snapshot.ContainsKey("a1-001"));
        Assert.True(snapshot.ContainsKey("pa-001"));
    }

    [Fact]
    public async Task FetchSnapshotAsync_PreservesSparseAndTypedFields()
    {
        var json = await File.ReadAllTextAsync(FixturePath);
        var service = CreateService(HttpStatusCode.OK, json);
        var snapshot = await service.FetchSnapshotAsync(CancellationToken.None);

        var bulbasaur = snapshot["a1-001"];
        Assert.Equal("Bulbasaur", bulbasaur.Name);
        Assert.Equal("a1", bulbasaur.SetCode);
        Assert.Null(bulbasaur.ArtStyle);
        Assert.Single(bulbasaur.AlternateVersions);
        Assert.Equal(227, bulbasaur.AlternateVersions[0].Id); // alt-version id is a bare int, not "a1-227"

        var charizard = snapshot["a1-280"];
        Assert.False(charizard.Tradable);
        Assert.Null(charizard.TradeCost);
        Assert.Equal("Immersive Art", charizard.ArtStyle);

        var necrozma = snapshot["a3-088"];
        Assert.NotNull(necrozma.SpecialTags);
        Assert.Contains("ultra_beasts", necrozma.SpecialTags!);

        var promo = snapshot["pa-001"];
        Assert.Null(promo.ReleaseDate);
        Assert.Null(promo.PackPoints);
        Assert.Null(promo.TradeCost);
    }

    [Fact]
    public async Task FetchSnapshotAsync_ThrowsOnServerError()
    {
        var service = CreateService(HttpStatusCode.NotFound, string.Empty);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.FetchSnapshotAsync(CancellationToken.None));
    }

    [Fact]
    public async Task TryGetCard_ReturnsFalse_BeforeAnyFetch()
    {
        var service = CreateService(HttpStatusCode.OK, "[]");

        var found = service.TryGetCard("a1-001", out _);

        Assert.False(found);
    }
}
