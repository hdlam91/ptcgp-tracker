using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Endpoints;
using PtcgpTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<CardDataOptions>(builder.Configuration.GetSection(CardDataOptions.SectionName));
builder.Services.AddHttpClient(CardCatalogHostedService.HttpClientName);
builder.Services.AddSingleton<CardCatalogHostedService>();
builder.Services.AddSingleton<ICardCatalogProvider>(sp => sp.GetRequiredService<CardCatalogHostedService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<CardCatalogHostedService>());
builder.Services.AddScoped<CollectionSummaryService>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var app = builder.Build();

// Lightweight CSRF defense: the frontend and backend are always served same-origin
// (Vite dev proxy locally, nginx reverse proxy in prod), so no cross-site caller ever
// has a CORS grant to set this header on a credentialed request. This is cheaper than
// full antiforgery tokens and is the standard defense for SPA + cookie-auth BFFs.
app.Use(async (context, next) =>
{
    var isMutating = HttpMethods.IsPost(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method)
        || HttpMethods.IsDelete(context.Request.Method)
        || HttpMethods.IsPatch(context.Request.Method);

    if (isMutating
        && context.Request.Path.StartsWithSegments("/api")
        && context.Request.Headers["X-Requested-With"] != "XMLHttpRequest")
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapAuthEndpoints();
app.MapCollectionEndpoints();
app.MapTradeListEndpoints();

// Convenient for solo-dev/compose; a real multi-replica cloud deploy should apply
// migrations as a separate step to avoid concurrent-migration races.
if (builder.Configuration.GetValue("APPLY_MIGRATIONS_ON_STARTUP", false))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

app.Run();

public partial class Program;
