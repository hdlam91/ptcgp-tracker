using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class CollectionEndpointsTests(PostgresApiFixture fixture)
{
    [Fact]
    public async Task Get_Unauthenticated_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/collection");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_InitiallyEmpty_ForNewUser()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var entries = await client.GetFromJsonAsync<JsonElement>("/api/collection");

        Assert.Equal(0, entries.GetArrayLength());
    }

    [Fact]
    public async Task Put_UnknownCardId_ReturnsBadRequest()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/collection/not-a-real-card", new { ownedCount = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_ValidCardId_UpsertsAndIsReflectedInGet()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var put = await client.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 3 });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var entries = await client.GetFromJsonAsync<List<JsonElement>>("/api/collection");
        var entry = Assert.Single(entries!);
        Assert.Equal("a1-001", entry.GetProperty("cardId").GetString());
        Assert.Equal(3, entry.GetProperty("ownedCount").GetInt32());
    }

    [Fact]
    public async Task Put_OwnedCountZero_DeletesTheEntry()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        await client.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 2 });

        var put = await client.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 0 });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var entries = await client.GetFromJsonAsync<List<JsonElement>>("/api/collection");
        Assert.Empty(entries!);
    }

    [Fact]
    public async Task Summary_GroupsOwnedCardsBySetCode()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        await client.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 2 });
        await client.PutAsJsonAsync("/api/collection/a1-002", new { ownedCount = 1 });
        await client.PutAsJsonAsync("/api/collection/a2-001", new { ownedCount = 5 });

        var summary = await client.GetFromJsonAsync<List<JsonElement>>("/api/collection/summary") ?? [];

        var a1 = summary.Single(s => s.GetProperty("setCode").GetString() == "a1");
        Assert.Equal(2, a1.GetProperty("ownedUniqueCards").GetInt32());
        Assert.Equal(3, a1.GetProperty("ownedCopiesTotal").GetInt32());

        var a2 = summary.Single(s => s.GetProperty("setCode").GetString() == "a2");
        Assert.Equal(1, a2.GetProperty("ownedUniqueCards").GetInt32());
        Assert.Equal(5, a2.GetProperty("ownedCopiesTotal").GetInt32());
    }

    [Fact]
    public async Task Delete_RemovesTheEntry()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        await client.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 1 });

        var delete = await client.DeleteAsync("/api/collection/a1-001");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var entries = await client.GetFromJsonAsync<List<JsonElement>>("/api/collection");
        Assert.Empty(entries!);
    }
}
