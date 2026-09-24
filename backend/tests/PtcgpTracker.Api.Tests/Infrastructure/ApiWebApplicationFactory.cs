using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PtcgpTracker.Api.CardData;

namespace PtcgpTracker.Api.Tests.Infrastructure;

internal class ApiWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            // Swap the real, network-backed catalog for a fixed fake one, and drop the
            // hosted service entirely so tests never make a real GitHub fetch.
            services.RemoveAll<IHostedService>();
            services.RemoveAll<CardCatalogHostedService>();
            services.RemoveAll<ICardCatalogProvider>();
            services.RemoveAll<ICardCatalogAdmin>();
            services.AddSingleton<FakeCardCatalogProvider>();
            services.AddSingleton<ICardCatalogProvider>(sp => sp.GetRequiredService<FakeCardCatalogProvider>());
            services.AddSingleton<ICardCatalogAdmin>(sp => sp.GetRequiredService<FakeCardCatalogProvider>());
        });
    }
}
