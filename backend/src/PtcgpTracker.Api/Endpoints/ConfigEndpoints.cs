using PtcgpTracker.Api.Models;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        // Public: the login and register pages need to know whether sign-up is open
        // before anyone is authenticated.
        app.MapGet("/api/config", async (AppSettingsService settings) =>
            Results.Ok(new PublicConfigResponse(await settings.IsRegistrationOpenAsync())));
    }
}
