using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class TradeListShareEndpointsTests(PostgresApiFixture fixture)
{
    [Fact]
    public async Task Status_Unauthenticated_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/trade-list/share");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Status_IsDisabledByDefault()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var status = await client.GetFromJsonAsync<JsonElement>("/api/trade-list/share");

        Assert.False(status.GetProperty("enabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, status.GetProperty("token").ValueKind);
    }

    [Fact]
    public async Task Enable_ThenPublicEndpoint_ReturnsTheUsersLists()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });

        var enableResponse = await client.PostAsync("/api/trade-list/share", null);
        var status = await enableResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = status.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        // The public endpoint must work on a fresh, unauthenticated client.
        var anonymousClient = fixture.Factory.CreateClient();
        var shared = await anonymousClient.GetFromJsonAsync<JsonElement>($"/api/trade-list/shared/{token}");

        Assert.Equal("Test Trainer", shared.GetProperty("displayName").GetString());
        var entries = shared.GetProperty("entries").EnumerateArray().ToList();
        var entry = Assert.Single(entries);
        Assert.Equal("a1-001", entry.GetProperty("cardId").GetString());
    }

    [Fact]
    public async Task PublicEndpoint_UnknownToken_ReturnsNotFound()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/trade-list/shared/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disable_RevokesTheToken()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        var enableResponse = await client.PostAsync("/api/trade-list/share", null);
        var status = await enableResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = status.GetProperty("token").GetString();

        var disableResponse = await client.DeleteAsync("/api/trade-list/share");
        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);

        var anonymousClient = fixture.Factory.CreateClient();
        var afterDisable = await anonymousClient.GetAsync($"/api/trade-list/shared/{token}");
        Assert.Equal(HttpStatusCode.NotFound, afterDisable.StatusCode);
    }

    [Fact]
    public async Task Enable_Twice_GeneratesADifferentToken()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var first = await (await client.PostAsync("/api/trade-list/share", null)).Content.ReadFromJsonAsync<JsonElement>();
        var second = await (await client.PostAsync("/api/trade-list/share", null)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.NotEqual(first.GetProperty("token").GetString(), second.GetProperty("token").GetString());
    }
}
