using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task Login_WithoutTwoFactor_ReturnsTheUserDirectly()
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var email = $"{Guid.NewGuid()}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password1", displayName = "Ash" });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.Equal(email, body.GetProperty("user").GetProperty("email").GetString());
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
    public async Task ForgotPassword_ForAnUnknownEmail_StillReturnsNoContent()
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var unknownEmail = $"{Guid.NewGuid()}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = unknownEmail });

        // Never reveals whether the email is registered — same response, and nothing sent.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(fixture.Factory.Services.GetRequiredService<FakeEmailSender>().Sent, e => e.ToEmail == unknownEmail);
    }

    [Fact]
    public async Task ForgotPassword_ThenResetPassword_ChangesThePasswordAndInvalidatesTheOldOne()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password1", displayName = "Ash" });

        var forgot = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);

        var sender = fixture.Factory.Services.GetRequiredService<FakeEmailSender>();
        var sentEmail = sender.Sent.Single(e => e.ToEmail == email);
        var resetUrl = new Uri(System.Text.RegularExpressions.Regex.Match(sentEmail.TextBody, @"https?://\S+").Value);
        // StringValues serializes as a JSON array, not a plain string — .ToString() gives the single value.
        var token = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(resetUrl.Query)["token"].ToString();

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password", new { email, token, newPassword = "NewPassword1" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var oldPasswordLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "NewPassword1" });
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithAGarbageToken_IsRejected()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password1", displayName = "Ash" });

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password", new { email, token = "not-a-real-token", newPassword = "NewPassword1" });

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
    }

    [Fact]
    public async Task Register_WithConfirmationRequired_BlocksLoginUntilTheLinkIsClicked()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var email = $"{Guid.NewGuid()}@example.com";

        try
        {
            Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(
                "/api/admin/settings", new { registrationOpen = true, requireEmailConfirmation = true })).StatusCode);

            var anonymous = fixture.Factory.CreateClient();
            anonymous.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            var register = await anonymous.PostAsJsonAsync("/api/auth/register", new { email, password = "Password1", displayName = "Pending" });
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
            var registerBody = await register.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(registerBody.GetProperty("requiresEmailConfirmation").GetBoolean());
            Assert.Equal(JsonValueKind.Null, registerBody.GetProperty("user").ValueKind);

            // Not actually signed in yet.
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);

            // Correct password, but still blocked.
            var blockedLogin = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
            Assert.Equal(HttpStatusCode.OK, blockedLogin.StatusCode);
            var blockedBody = await blockedLogin.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(blockedBody.GetProperty("requiresEmailConfirmation").GetBoolean());
            Assert.Equal(JsonValueKind.Null, blockedBody.GetProperty("user").ValueKind);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);

            // Pull the confirmation link out of the fake mailer.
            var sender = fixture.Factory.Services.GetRequiredService<FakeEmailSender>();
            var sentEmail = sender.Sent.Last(e => e.ToEmail == email);
            var confirmUrl = new Uri(System.Text.RegularExpressions.Regex.Match(sentEmail.TextBody, @"https?://\S+").Value);
            var token = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(confirmUrl.Query)["token"].ToString();

            var confirm = await anonymous.PostAsJsonAsync("/api/auth/confirm-email", new { email, token });
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

            // Confirming signs you in immediately.
            Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/auth/me")).StatusCode);

            // A fresh login now succeeds normally too.
            await anonymous.PostAsync("/api/auth/logout", null);
            var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
            var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(loginBody.GetProperty("requiresEmailConfirmation").GetBoolean());
        }
        finally
        {
            await admin.PutAsJsonAsync("/api/admin/settings", new { registrationOpen = true, requireEmailConfirmation = false });
        }
    }

    [Fact]
    public async Task Register_WithConfirmationRequired_ConfiguredAdminEmailSkipsIt()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var adminEmail = $"{Guid.NewGuid()}@example.com";
        using var factory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Emails"] = adminEmail })));

        try
        {
            await admin.PutAsJsonAsync("/api/admin/settings", new { registrationOpen = true, requireEmailConfirmation = true });

            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            var register = await client.PostAsJsonAsync("/api/auth/register", new { email = adminEmail, password = "Password1", displayName = "Boss" });
            var body = await register.Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.GetProperty("requiresEmailConfirmation").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        }
        finally
        {
            await admin.PutAsJsonAsync("/api/admin/settings", new { registrationOpen = true, requireEmailConfirmation = false });
        }
    }

    [Fact]
    public async Task EnablingConfirmation_GrandfathersUsersWhoRegisteredBeforeIt()
    {
        var admin = await fixture.Factory.CreateAdminClientAsync();
        var email = $"{Guid.NewGuid()}@example.com";
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(email);

        try
        {
            Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(
                "/api/admin/settings", new { registrationOpen = true, requireEmailConfirmation = true })).StatusCode);

            // Already-logged-in session is untouched...
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

            // ...and a brand new login for that same pre-existing account isn't blocked either.
            var anonymous = fixture.Factory.CreateClient();
            anonymous.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var body = await login.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("requiresEmailConfirmation").GetBoolean());
        }
        finally
        {
            await admin.PutAsJsonAsync("/api/admin/settings", new { registrationOpen = true, requireEmailConfirmation = false });
        }
    }

    [Fact]
    public async Task ResendConfirmation_NeverRevealsWhetherAnEmailExistsOrIsAlreadyConfirmed()
    {
        var confirmedEmail = $"{Guid.NewGuid()}@example.com";
        await fixture.Factory.CreateAuthenticatedClientAsync(confirmedEmail);
        var unknownEmail = $"{Guid.NewGuid()}@example.com";

        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/resend-confirmation", new { email = confirmedEmail })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/resend-confirmation", new { email = unknownEmail })).StatusCode);

        var sender = fixture.Factory.Services.GetRequiredService<FakeEmailSender>();
        Assert.DoesNotContain(sender.Sent, e => e.ToEmail == confirmedEmail || e.ToEmail == unknownEmail);
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
