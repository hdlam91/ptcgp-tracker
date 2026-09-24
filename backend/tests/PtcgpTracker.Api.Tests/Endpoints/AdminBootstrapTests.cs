using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Services;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Endpoints;

[Collection(ApiTestCollection.Name)]
public class AdminBootstrapTests(PostgresApiFixture fixture)
{
    private static IConfiguration ConfigWith(string? adminEmails) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Emails"] = adminEmails })
            .Build();

    private AdminRoleService ServiceFor(IServiceScope scope, string? adminEmails) =>
        new(
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            ConfigWith(adminEmails));

    [Fact]
    public async Task PromoteConfiguredAdmins_PromotesMatchingUsersCaseInsensitively_AndLeavesOthersAlone()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await fixture.Factory.CreateAuthenticatedClientAsync(email);
        var otherEmail = $"{Guid.NewGuid()}@example.com";
        await fixture.Factory.CreateAuthenticatedClientAsync(otherEmail);

        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = ServiceFor(scope, $" {email.ToUpperInvariant()} ,nobody@nowhere.example");

        await service.PromoteConfiguredAdminsAsync();

        Assert.True(await userManager.IsInRoleAsync((await userManager.FindByEmailAsync(email))!, AppRoles.Admin));
        Assert.False(await userManager.IsInRoleAsync((await userManager.FindByEmailAsync(otherEmail))!, AppRoles.Admin));
    }

    [Fact]
    public void IsConfiguredAdmin_HandlesEmptyAndMissingConfig()
    {
        using var scope = fixture.Factory.Services.CreateScope();

        Assert.False(ServiceFor(scope, null).IsConfiguredAdmin("a@example.com"));
        Assert.False(ServiceFor(scope, "").IsConfiguredAdmin("a@example.com"));
        Assert.False(ServiceFor(scope, "b@example.com").IsConfiguredAdmin(null));
        Assert.True(ServiceFor(scope, "a@example.com, b@example.com").IsConfiguredAdmin("B@Example.com"));
    }

    [Fact]
    public async Task Register_WithAConfiguredAdminEmail_IsAdminFromTheFirstRequest()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        using var factory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Emails"] = email })));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password1", displayName = "Boss" });
        var created = await register.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(created.GetProperty("isAdmin").GetBoolean());
        Assert.True((await client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("isAdmin").GetBoolean());
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/api/admin/users")).StatusCode);
    }
}
