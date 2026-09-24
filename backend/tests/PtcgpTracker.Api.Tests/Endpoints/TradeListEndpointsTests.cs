using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class TradeListEndpointsTests(PostgresApiFixture fixture)
{
    [Fact]
    public async Task Post_UnknownCardId_ReturnsBadRequest()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/trade-list", new { cardId = "not-a-real-card", direction = "Want" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidCardId_CreatesEntry_ReflectedInGet()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var post = await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var entries = await client.GetFromJsonAsync<List<JsonElement>>("/api/trade-list");
        var entry = Assert.Single(entries!);
        Assert.Equal("a1-001", entry.GetProperty("cardId").GetString());
        Assert.Equal("Want", entry.GetProperty("direction").GetString());
    }

    [Fact]
    public async Task Post_SameCardBothDirections_BothAllowed()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var want = await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });
        var offer = await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Offer" });

        Assert.Equal(HttpStatusCode.Created, want.StatusCode);
        Assert.Equal(HttpStatusCode.Created, offer.StatusCode);

        var entries = await client.GetFromJsonAsync<List<JsonElement>>("/api/trade-list");
        Assert.Equal(2, entries!.Count);
    }

    [Fact]
    public async Task Post_Duplicate_ReturnsConflict()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });

        var duplicate = await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Get_FiltersByDirection()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });
        await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-002", direction = "Offer" });

        var wanted = await client.GetFromJsonAsync<List<JsonElement>>("/api/trade-list?direction=Want");

        var entry = Assert.Single(wanted!);
        Assert.Equal("a1-001", entry.GetProperty("cardId").GetString());
    }

    [Fact]
    public async Task Delete_RemovesTheEntry()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });

        var delete = await client.DeleteAsync("/api/trade-list/a1-001/Want");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var entries = await client.GetFromJsonAsync<List<JsonElement>>("/api/trade-list");
        Assert.Empty(entries!);
    }
}
