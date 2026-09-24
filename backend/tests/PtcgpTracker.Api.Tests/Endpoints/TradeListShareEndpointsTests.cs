using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class TradeListShareEndpointsTests(PostgresApiFixture fixture)
{
    private static string UniqueName() => $"Trainer {Guid.NewGuid():N}";

    private static async Task<string> EnableAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/trade-list/share", null);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>();
        return status.GetProperty("handle").GetString()!;
    }

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
        Assert.Equal(JsonValueKind.Null, status.GetProperty("handle").ValueKind);
    }

    [Fact]
    public async Task Enable_ThenPublicEndpoint_ReturnsTheUsersLists()
    {
        var displayName = UniqueName();
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: displayName);
        await client.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Want" });

        var handle = await EnableAsync(client);
        Assert.False(string.IsNullOrEmpty(handle));

        // The public endpoint must work on a fresh, unauthenticated client.
        var anonymousClient = fixture.Factory.CreateClient();
        var shared = await anonymousClient.GetFromJsonAsync<JsonElement>($"/api/trade-list/shared/{handle}");

        Assert.Equal(displayName, shared.GetProperty("displayName").GetString());
        var entries = shared.GetProperty("entries").EnumerateArray().ToList();
        var entry = Assert.Single(entries);
        Assert.Equal("a1-001", entry.GetProperty("cardId").GetString());
    }

    [Fact]
    public async Task Enable_DerivesTheHandleFromTheDisplayName()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: $"HD {suffix}");

        var handle = await EnableAsync(client);

        Assert.Equal($"hd-{suffix}", handle);
    }

    [Fact]
    public async Task Enable_TwoUsersWithTheSameDisplayName_GetDistinctHandles()
    {
        var displayName = UniqueName();
        var first = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: displayName);
        var second = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: displayName);

        var firstHandle = await EnableAsync(first);
        var secondHandle = await EnableAsync(second);

        Assert.NotEqual(firstHandle, secondHandle);
        Assert.Equal($"{firstHandle}-2", secondHandle);
    }

    [Fact]
    public async Task PublicEndpoint_HandleLookupIsCaseInsensitive()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: UniqueName());
        var handle = await EnableAsync(client);

        var anonymousClient = fixture.Factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/trade-list/shared/{handle.ToUpperInvariant()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoint_UnknownHandle_ReturnsNotFound()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/trade-list/shared/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disable_RevokesTheLink()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: UniqueName());
        var handle = await EnableAsync(client);

        var disableResponse = await client.DeleteAsync("/api/trade-list/share");
        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);

        var anonymousClient = fixture.Factory.CreateClient();
        var afterDisable = await anonymousClient.GetAsync($"/api/trade-list/shared/{handle}");
        Assert.Equal(HttpStatusCode.NotFound, afterDisable.StatusCode);
    }

    [Fact]
    public async Task Enable_Twice_KeepsTheSameHandle()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: UniqueName());

        var first = await EnableAsync(client);
        var second = await EnableAsync(client);

        Assert.Equal(first, second);
    }
}
