using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Services;
using PtcgpTracker.Api.Tests.Infrastructure;

namespace PtcgpTracker.Api.Tests.Services;

[Collection(ApiTestCollection.Name)]
public class CollectionMergeServiceTests(PostgresApiFixture fixture)
{
    [Fact]
    public async Task MergeDuplicatesAsync_CollapsesPreExistingRowsIntoOneCanonicalRow_KeepingTheHigherCount()
    {
        using var factory = LinkedCardsTestFactory.Create(fixture);
        var client = await factory.CreateAuthenticatedClientAsync();
        var userId = await client.GetUserIdAsync();

        // Simulate data written before this feature existed: two separate rows for what's
        // actually the same card (a1-001 is a4b-001's canonical id — earlier release_date).
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.CollectionEntries.AddRange(
                new CollectionEntry { UserId = userId, CardId = "a1-001", OwnedCount = 1 },
                new CollectionEntry { UserId = userId, CardId = "a4b-001", OwnedCount = 3 });
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<CollectionMergeService>().MergeDuplicatesAsync();
        }

        var entries = await client.GetFromJsonAsync<List<JsonElement>>("/api/collection") ?? [];
        Assert.Equal(2, entries.Count); // expanded back out to both ids in the group
        Assert.All(entries, e => Assert.Equal(3, e.GetProperty("ownedCount").GetInt32())); // kept the higher count, not summed

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await verifyDb.CollectionEntries.Where(e => e.UserId == userId).ToListAsync();
        var storedEntry = Assert.Single(stored);
        Assert.Equal("a1-001", storedEntry.CardId);
        Assert.Equal(3, storedEntry.OwnedCount);
    }

    [Fact]
    public async Task MergeDuplicatesAsync_RenamesASingleStaleRow_ToItsCanonicalId()
    {
        using var factory = LinkedCardsTestFactory.Create(fixture);
        var client = await factory.CreateAuthenticatedClientAsync();
        var userId = await client.GetUserIdAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.CollectionEntries.Add(new CollectionEntry { UserId = userId, CardId = "a4b-001", OwnedCount = 5 });
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<CollectionMergeService>().MergeDuplicatesAsync();
        }

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await verifyDb.CollectionEntries.Where(e => e.UserId == userId).ToListAsync();
        var storedEntry = Assert.Single(stored);
        Assert.Equal("a1-001", storedEntry.CardId); // renamed from a4b-001 to the group's canonical id
        Assert.Equal(5, storedEntry.OwnedCount);
    }

    [Fact]
    public async Task MergeDuplicatesAsync_IsIdempotent()
    {
        using var factory = LinkedCardsTestFactory.Create(fixture);
        var client = await factory.CreateAuthenticatedClientAsync();
        var userId = await client.GetUserIdAsync();
        await client.PutAsJsonAsync("/api/collection/a4b-001", new { ownedCount = 2 });

        using var scope = factory.Services.CreateScope();
        var mergeService = scope.ServiceProvider.GetRequiredService<CollectionMergeService>();
        await mergeService.MergeDuplicatesAsync();
        await mergeService.MergeDuplicatesAsync();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.CollectionEntries.Where(e => e.UserId == userId).ToListAsync();
        var storedEntry = Assert.Single(stored);
        Assert.Equal("a1-001", storedEntry.CardId);
        Assert.Equal(2, storedEntry.OwnedCount);
    }
}
