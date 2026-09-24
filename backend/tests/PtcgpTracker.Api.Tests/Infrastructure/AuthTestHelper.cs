using System.Net.Http.Json;

namespace PtcgpTracker.Api.Tests.Infrastructure;

internal static class AuthTestHelper
{
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this ApiWebApplicationFactory factory, string? email = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = email ?? $"{Guid.NewGuid()}@example.com",
            password = "Password1",
            displayName = "Test Trainer",
        });
        response.EnsureSuccessStatusCode();

        return client;
    }
}
