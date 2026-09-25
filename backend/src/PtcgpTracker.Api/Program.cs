using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Endpoints;
using PtcgpTracker.Api.Images;
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
builder.Services.AddSingleton<ICardCatalogAdmin>(sp => sp.GetRequiredService<CardCatalogHostedService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<CardCatalogHostedService>());
builder.Services.AddScoped<CollectionSummaryService>();
builder.Services.Configure<ImageMirrorOptions>(builder.Configuration.GetSection(ImageMirrorOptions.SectionName));
builder.Services.AddHttpClient(ImageMirrorService.HttpClientName);
builder.Services.AddSingleton<ImageMirrorService>();
builder.Services.AddScoped<AppSettingsService>();
builder.Services.AddScoped<AdminRoleService>();

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

        // The cookie alone would keep a deleted user signed in, and keep a demoted admin's
        // role, for up to 14 days. Re-check the user on every request instead, so deletes
        // and role changes take effect immediately (one small lookup per request is fine here).
        options.Events.OnValidatePrincipal = async context =>
        {
            var services = context.HttpContext.RequestServices;
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            var user = Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? await userManager.FindByIdAsync(userId.ToString())
                : null;

            if (user is null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return;
            }

            var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (isAdmin != context.Principal!.IsInRole(AppRoles.Admin))
            {
                var signInManager = services.GetRequiredService<SignInManager<ApplicationUser>>();
                context.ReplacePrincipal(await signInManager.CreateUserPrincipalAsync(user));
                context.ShouldRenew = true;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppRoles.AdminPolicy, policy => policy.RequireRole(AppRoles.Admin));

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

// Art downloaded from Settings, served from disk (the frontend's nginx maps /card-images and
// /pack-images here). Before authentication: card art is public. Anything missing is a plain 404,
// which the frontend answers by falling back to GitHub.
var imageDirectory = Path.GetFullPath(app.Services.GetRequiredService<IOptions<ImageMirrorOptions>>().Value.Directory);
try
{
    Directory.CreateDirectory(imageDirectory);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    app.Logger.LogWarning(ex, "Image storage {Directory} isn't writable; downloaded art won't be available", imageDirectory);
}

if (Directory.Exists(imageDirectory))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imageDirectory),
        RequestPath = "/local-images",
        OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public, max-age=604800",
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapAuthEndpoints();
app.MapCollectionEndpoints();
app.MapTradeListEndpoints();
app.MapTradeListShareEndpoints();
app.MapConfigEndpoints();
app.MapAdminEndpoints();

// Convenient for solo-dev/compose; a real multi-replica cloud deploy should apply
// migrations as a separate step to avoid concurrent-migration races.
if (builder.Configuration.GetValue("APPLY_MIGRATIONS_ON_STARTUP", false))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AdminRoleService>().PromoteConfiguredAdminsAsync();
}

app.Run();

public partial class Program;
