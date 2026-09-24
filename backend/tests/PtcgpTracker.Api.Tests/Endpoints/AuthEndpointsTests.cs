using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class AuthEndpointsTests(PostgresApiFixture fixture)
{
    [Fact]
    public async Task Register_ThenMe_ReturnsTheRegisteredUser()
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var email = $"{Guid.NewGuid()}@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1",
            displayName = "Ash",
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var body = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_WithoutSession_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var email = $"{Guid.NewGuid()}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password1", displayName = "Ash" });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Logout_ThenMe_ReturnsUnauthorized()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task MutatingRequest_WithoutCsrfHeader_ReturnsForbidden()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{Guid.NewGuid()}@example.com",
            password = "Password1",
            displayName = "Ash",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
