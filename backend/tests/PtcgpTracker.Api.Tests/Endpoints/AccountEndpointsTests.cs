using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OtpNet;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class AccountEndpointsTests(PostgresApiFixture fixture)
{
    private const string Password = "Password1";

    private static string GenerateCode(string sharedKey) => new Totp(Base32Encoding.ToBytes(sharedKey)).ComputeTotp();

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_IsRejected()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/account/password", new
        {
            currentPassword = "WrongPassword1",
            newPassword = "NewPassword1",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ThenLoginWithOldPassword_Fails_NewPassword_Works()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(email: email);

        var change = await client.PostAsJsonAsync("/api/account/password", new
        {
            currentPassword = Password,
            newPassword = "NewPassword1",
        });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var anonymousClient = fixture.Factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        var oldPasswordLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new { email, password = "NewPassword1" });
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_ThenLoginWithNewEmail_Works()
    {
        var oldEmail = $"{Guid.NewGuid()}@example.com";
        var newEmail = $"{Guid.NewGuid()}@example.com";
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(email: oldEmail);

        var change = await client.PostAsJsonAsync("/api/account/email", new { newEmail, currentPassword = Password });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var anonymousClient = fixture.Factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var login = await anonymousClient.PostAsJsonAsync("/api/auth/login", new { email = newEmail, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_ToAnExistingUsersEmail_IsRejected()
    {
        var takenEmail = $"{Guid.NewGuid()}@example.com";
        await fixture.Factory.CreateAuthenticatedClientAsync(email: takenEmail);
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/account/email", new { newEmail = takenEmail, currentPassword = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithWrongPassword_IsRejected()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/account")
        {
            Content = JsonContent.Create(new { currentPassword = "WrongPassword1" }),
        };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_RemovesTheUserAndSignsOut()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(email: email);
        await client.PutAsJsonAsync("/api/collection/a1-001", new { ownedCount = 2 });

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/account")
        {
            Content = JsonContent.Create(new { currentPassword = Password }),
        };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        var anonymousClient = fixture.Factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var login = await anonymousClient.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task TwoFactor_FullLifecycle_SetupEnableLoginDisable()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var client = await fixture.Factory.CreateAuthenticatedClientAsync(email: email);

        var statusBefore = await client.GetFromJsonAsync<JsonElement>("/api/account/2fa");
        Assert.False(statusBefore.GetProperty("enabled").GetBoolean());

        var setup = await (await client.GetAsync("/api/account/2fa/setup")).Content.ReadFromJsonAsync<JsonElement>();
        var sharedKey = setup.GetProperty("sharedKey").GetString()!;
        Assert.Contains("otpauth://totp/", setup.GetProperty("otpAuthUri").GetString());

        var enableWithBadCode = await client.PostAsJsonAsync("/api/account/2fa/enable", new { code = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, enableWithBadCode.StatusCode);

        var enable = await client.PostAsJsonAsync("/api/account/2fa/enable", new { code = GenerateCode(sharedKey) });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        var recoveryCodes = (await enable.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.NotEmpty(recoveryCodes);

        var statusAfter = await client.GetFromJsonAsync<JsonElement>("/api/account/2fa");
        Assert.True(statusAfter.GetProperty("enabled").GetBoolean());

        // A fresh, unauthenticated client now needs the second step to finish logging in.
        var anonymousClient = fixture.Factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var login = await anonymousClient.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(loginBody.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.Equal(JsonValueKind.Null, loginBody.GetProperty("user").ValueKind);

        var badCodeLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login/2fa", new { code = "000000", isRecoveryCode = false, rememberDevice = false });
        Assert.Equal(HttpStatusCode.Unauthorized, badCodeLogin.StatusCode);

        var codeLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login/2fa", new { code = GenerateCode(sharedKey), isRecoveryCode = false, rememberDevice = false });
        Assert.Equal(HttpStatusCode.OK, codeLogin.StatusCode);

        var meAfterTwoFactorLogin = await anonymousClient.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal(email, meAfterTwoFactorLogin.GetProperty("email").GetString());

        // A recovery code also completes a fresh login, and each one works only once.
        var recoveryClient = fixture.Factory.CreateClient();
        recoveryClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        await recoveryClient.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var recoveryLogin = await recoveryClient.PostAsJsonAsync("/api/auth/login/2fa", new { code = recoveryCodes[0], isRecoveryCode = true, rememberDevice = false });
        Assert.Equal(HttpStatusCode.OK, recoveryLogin.StatusCode);

        var disable = await client.PostAsJsonAsync("/api/account/2fa/disable", new { currentPassword = Password });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);

        var statusAfterDisable = await client.GetFromJsonAsync<JsonElement>("/api/account/2fa");
        Assert.False(statusAfterDisable.GetProperty("enabled").GetBoolean());

        // Logging in again now needs no second step.
        var plainLoginClient = fixture.Factory.CreateClient();
        plainLoginClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var plainLogin = await plainLoginClient.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var plainLoginBody = await plainLogin.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(plainLoginBody.GetProperty("requiresTwoFactor").GetBoolean());
    }
}
