using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Tests.Infrastructure;

internal static class AuthTestHelper
{
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this ApiWebApplicationFactory factory, string? email = null, string displayName = "Test Trainer")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = email ?? $"{Guid.NewGuid()}@example.com",
            password = "Password1",
            displayName,
        });
        response.EnsureSuccessStatusCode();

        return client;
    }

    /// <summary>
    /// Registers a normal user, then promotes them straight in the database — the
    /// already-issued session cookie has no admin claim yet, so this also exercises
    /// the server-side role refresh on the next request.
    /// </summary>
    public static async Task<HttpClient> CreateAdminClientAsync(this ApiWebApplicationFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync(displayName: "Admin");
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        await factory.SetAdminAsync(me.GetProperty("id").GetGuid(), true);
        return client;
    }

    public static async Task SetAdminAsync(this ApiWebApplicationFactory factory, Guid userId, bool isAdmin)
    {
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByIdAsync(userId.ToString());
        await scope.ServiceProvider.GetRequiredService<AdminRoleService>().SetAdminAsync(user!, isAdmin);
    }

    public static async Task<Guid> GetUserIdAsync(this HttpClient client)
    {
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        return me.GetProperty("id").GetGuid();
    }
}
