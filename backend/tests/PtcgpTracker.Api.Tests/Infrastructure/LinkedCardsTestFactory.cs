using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PtcgpTracker.Api.CardData;

namespace PtcgpTracker.Api.Tests.Infrastructure;

/// <summary>
/// A factory override, layered on the shared <see cref="PostgresApiFixture"/>, whose catalog
/// includes a1-001 / a4b-001 (the same real-world card per <see cref="CardEquivalenceGrouping"/>)
/// and a4b-002 (a4b-001's own reverse foil, which must stay separate) — scoped to its own DI
/// container (same underlying Postgres) so these extra cards don't change the card count every
/// other test in the collection already asserts against.
/// </summary>
internal static class LinkedCardsTestFactory
{
    public static WebApplicationFactory<Program> Create(PostgresApiFixture fixture) =>
        fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICardCatalogProvider>();
            services.RemoveAll<ICardCatalogAdmin>();
            var catalog = new FakeCardCatalogProvider([
                FakeCardCatalogProvider.CardA1_001,
                FakeCardCatalogProvider.CardA4b_001,
                FakeCardCatalogProvider.CardA4b_002,
            ]);
            services.AddSingleton<ICardCatalogProvider>(catalog);
            services.AddSingleton<ICardCatalogAdmin>(catalog);
        }));
}
