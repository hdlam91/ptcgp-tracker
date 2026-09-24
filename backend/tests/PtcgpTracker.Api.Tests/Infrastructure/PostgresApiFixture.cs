using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PtcgpTracker.Api.Data;
using Testcontainers.PostgreSql;

namespace PtcgpTracker.Api.Tests.Infrastructure;

/// <summary>
/// One Postgres container and one WebApplicationFactory shared across the endpoint
/// test classes in <see cref="ApiTestCollection"/>, since Docker is already a hard
/// dependency of this stack — no reason to test against non-Postgres semantics
/// (e.g. EF Core's InMemory provider) or to pay container-startup cost per class.
/// </summary>
public class PostgresApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ptcgp_tracker_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    internal ApiWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new ApiWebApplicationFactory(_container.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiTestCollection : ICollectionFixture<PostgresApiFixture>
{
    public const string Name = "Api integration tests";
}
