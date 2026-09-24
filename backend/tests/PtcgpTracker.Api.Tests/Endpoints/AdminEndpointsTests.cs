using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class AdminEndpointsTests(PostgresApiFixture fixture)
{
    private static JsonElement FindUser(JsonElement users, Guid id) =>
        users.EnumerateArray().Single(u => u.GetProperty("id").GetGuid() == id);

    [Fact]
    public async Task Users_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await fixture.Factory.CreateClient().GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Users_NonAdmin_ReturnsForbidden()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReportsWhetherTheUserIsAnAdmin()
    {
        var user = await fixture.Factory.CreateAuthenticatedClientAsync();
        var admin = await fixture.Factory.CreateAdminClientAsync();

        Assert.False((await user.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("isAdmin").GetBoolean());
        Assert.True((await admin.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("isAdmin").GetBoolean());
    }

    [Fact]
    public async Task Users_Admin_ListsEveryoneWithTheirStats()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var trainer = await fixture.Factory.CreateAuthenticatedClientAsync(displayName: "Stats Trainer");
        await trainer.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 2 });
        await trainer.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-002", direction = "Want" });
        await trainer.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-001", direction = "Offer" });
        await trainer.PostAsync("/api/trade-list/share", null);

        var users = await admin.GetFromJsonAsync<JsonElement>("/api/admin/users");

        var entry = FindUser(users, await trainer.GetUserIdAsync());
        Assert.Equal("Stats Trainer", entry.GetProperty("displayName").GetString());
        Assert.False(entry.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(1, entry.GetProperty("ownedUniqueCards").GetInt32());
        Assert.Equal(1, entry.GetProperty("wantCount").GetInt32());
        Assert.Equal(1, entry.GetProperty("offerCount").GetInt32());
        Assert.StartsWith("stats-trainer", entry.GetProperty("shareHandle").GetString());

        var adminEntry = FindUser(users, await admin.GetUserIdAsync());
        Assert.True(adminEntry.GetProperty("isAdmin").GetBoolean());
    }

    [Fact]
    public async Task SetAdmin_PromoteThenDemote_TakesEffectOnTheNextRequest()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var trainer = await fixture.Factory.CreateAuthenticatedClientAsync();
        var trainerId = await trainer.GetUserIdAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await trainer.GetAsync("/api/admin/users")).StatusCode);

        var promote = await admin.PutAsJsonAsync($"/api/admin/users/{trainerId}/admin", new { isAdmin = true });
        Assert.Equal(HttpStatusCode.NoContent, promote.StatusCode);

        // Same session cookie as before the promotion — no re-login needed.
        Assert.Equal(HttpStatusCode.OK, (await trainer.GetAsync("/api/admin/users")).StatusCode);

        var demote = await admin.PutAsJsonAsync($"/api/admin/users/{trainerId}/admin", new { isAdmin = false });
        Assert.Equal(HttpStatusCode.NoContent, demote.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trainer.GetAsync("/api/admin/users")).StatusCode);
    }

    [Fact]
    public async Task SetAdmin_OnSelf_ReturnsBadRequest()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{await admin.GetUserIdAsync()}/admin", new { isAdmin = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetAdmin_UnknownUser_ReturnsNotFound()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/admin", new { isAdmin = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_RemovesTheUserTheirDataAndTheirSession()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var email = $"{Guid.NewGuid()}@example.com";
        var trainer = await fixture.Factory.CreateAuthenticatedClientAsync(email);
        var trainerId = await trainer.GetUserIdAsync();
        await trainer.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 1 });
        await trainer.PostAsJsonAsync("/api/trade-list", new { cardId = "a1-002", direction = "Want" });

        var response = await admin.DeleteAsync($"/api/admin/users/{trainerId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Their still-valid cookie must stop working straight away.
        Assert.Equal(HttpStatusCode.Unauthorized, (await trainer.GetAsync("/api/auth/me")).StatusCode);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.Id == trainerId));
        Assert.False(await db.CollectionEntries.AnyAsync(e => e.UserId == trainerId));
        Assert.False(await db.TradeListEntries.AnyAsync(e => e.UserId == trainerId));

        var loginClient = fixture.Factory.CreateClient();
        loginClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var login = await loginClient.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_OnlyRemovesTheTargetsData()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var doomed = await fixture.Factory.CreateAuthenticatedClientAsync();
        var bystander = await fixture.Factory.CreateAuthenticatedClientAsync();
        await doomed.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 1 });
        await bystander.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 3 });

        await admin.DeleteAsync($"/api/admin/users/{await doomed.GetUserIdAsync()}");

        var remaining = await bystander.GetFromJsonAsync<JsonElement>("/api/collection");
        Assert.Equal(3, Assert.Single(remaining.EnumerateArray()).GetProperty("ownedCount").GetInt32());
    }

    [Fact]
    public async Task DeleteUser_Self_ReturnsBadRequest()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();

        var response = await admin.DeleteAsync($"/api/admin/users/{await admin.GetUserIdAsync()}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_NonAdmin_ReturnsForbidden()
    {
        var trainer = await fixture.Factory.CreateAuthenticatedClientAsync();
        var victim = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await trainer.DeleteAsync($"/api/admin/users/{await victim.GetUserIdAsync()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DisableShare_ClearsTheUsersPublicLink()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var trainer = await fixture.Factory.CreateAuthenticatedClientAsync();
        var status = await (await trainer.PostAsync("/api/trade-list/share", null)).Content.ReadFromJsonAsync<JsonElement>();
        var handle = status.GetProperty("handle").GetString();

        var response = await admin.DeleteAsync($"/api/admin/users/{await trainer.GetUserIdAsync()}/share");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var publicResponse = await fixture.Factory.CreateClient().GetAsync($"/api/trade-list/shared/{handle}");
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);
        var afterStatus = await trainer.GetFromJsonAsync<JsonElement>("/api/trade-list/share");
        Assert.False(afterStatus.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task Registration_ClosingBlocksSignupsButNotExistingUsers_AndReopeningRestoresIt()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var existingEmail = $"{Guid.NewGuid()}@example.com";
        await fixture.Factory.CreateAuthenticatedClientAsync(existingEmail);
        var anonymous = fixture.Factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        Assert.True((await anonymous.GetFromJsonAsync<JsonElement>("/api/config")).GetProperty("registrationOpen").GetBoolean());

        try
        {
            var close = await admin.PutAsJsonAsync("/api/admin/settings", new { registrationOpen = false });
            Assert.Equal(HttpStatusCode.OK, close.StatusCode);
            Assert.False((await anonymous.GetFromJsonAsync<JsonElement>("/api/config")).GetProperty("registrationOpen").GetBoolean());
            Assert.False((await admin.GetFromJsonAsync<JsonElement>("/api/admin/settings")).GetProperty("registrationOpen").GetBoolean());

            var blocked = await anonymous.PostAsJsonAsync("/api/auth/register",
                new { email = $"{Guid.NewGuid()}@example.com", password = "Password1", displayName = "Late" });
            Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
            var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("registration_closed", body.GetProperty("error").GetString());

            var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = existingEmail, password = "Password1" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }
        finally
        {
            await admin.PutAsJsonAsync("/api/admin/settings", new { registrationOpen = true });
        }

        var allowed = await anonymous.PostAsJsonAsync("/api/auth/register",
            new { email = $"{Guid.NewGuid()}@example.com", password = "Password1", displayName = "On Time" });
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
    }

    [Fact]
    public async Task Settings_NonAdmin_ReturnsForbidden()
    {
        var trainer = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await trainer.PutAsJsonAsync("/api/admin/settings", new { registrationOpen = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_StatusAndRefresh_ReflectTheCatalog()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();

        var status = await admin.GetFromJsonAsync<JsonElement>("/api/admin/catalog");
        Assert.Equal("v-test", status.GetProperty("repoTag").GetString());
        Assert.Equal(3, status.GetProperty("cardCount").GetInt32());

        var refresh = await admin.PostAsync("/api/admin/catalog/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var after = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, after.GetProperty("lastRefreshedAt").ValueKind);
    }

    [Fact]
    public async Task Catalog_RefreshFailure_ReturnsBadGateway()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var catalog = fixture.Factory.Services.GetRequiredService<FakeCardCatalogProvider>();
        catalog.RefreshFailure = new HttpRequestException("network down");

        try
        {
            var response = await admin.PostAsync("/api/admin/catalog/refresh", null);

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.Contains("network down", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            catalog.RefreshFailure = null;
        }
    }

    [Fact]
    public async Task Catalog_NonAdmin_ReturnsForbidden()
    {
        var trainer = await fixture.Factory.CreateAuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await trainer.GetAsync("/api/admin/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trainer.PostAsync("/api/admin/catalog/refresh", null)).StatusCode);
    }
}
