using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.CardData;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Images;
using PtcgpTracker.Api.Models;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization(AppRoles.AdminPolicy);

        group.MapGet("/users", async (
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            AdminRoleService adminRoles) =>
        {
            await adminRoles.EnsureRoleAsync();

            var users = await db.Users.OrderBy(u => u.CreatedAt).ToListAsync();
            var adminIds = (await userManager.GetUsersInRoleAsync(AppRoles.Admin)).Select(u => u.Id).ToHashSet();

            var ownedByUser = await db.CollectionEntries
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var tradesByUser = await db.TradeListEntries
                .GroupBy(e => new { e.UserId, e.Direction })
                .Select(g => new { g.Key.UserId, g.Key.Direction, Count = g.Count() })
                .ToListAsync();

            int TradeCount(Guid userId, TradeDirection direction) =>
                tradesByUser.FirstOrDefault(t => t.UserId == userId && t.Direction == direction)?.Count ?? 0;

            return Results.Ok(users.Select(u => new AdminUserResponse(
                u.Id,
                u.Email!,
                u.DisplayName,
                u.CreatedAt,
                adminIds.Contains(u.Id),
                ownedByUser.GetValueOrDefault(u.Id),
                TradeCount(u.Id, TradeDirection.Want),
                TradeCount(u.Id, TradeDirection.Offer),
                u.TradeListShareHandle)).ToList());
        });

        // Admins can't change their own status or delete themselves — an admin can't lock
        // the app out of having one by accident, and someone else has to make that call.
        group.MapPut("/users/{id:guid}/admin", async (
            Guid id,
            SetAdminRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AdminRoleService adminRoles) =>
        {
            if (id == GetUserId(principal))
            {
                return Results.BadRequest(new { error = "You can't change your own admin status." });
            }

            var user = await userManager.FindByIdAsync(id.ToString());
            if (user is null)
            {
                return Results.NotFound();
            }

            await adminRoles.SetAdminAsync(user, request.IsAdmin);
            return Results.NoContent();
        });

        group.MapDelete("/users/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager) =>
        {
            if (id == GetUserId(principal))
            {
                return Results.BadRequest(new { error = "You can't delete your own account." });
            }

            var user = await userManager.FindByIdAsync(id.ToString());
            if (user is null)
            {
                return Results.NotFound();
            }

            // Collection and trade-list rows hold a plain UserId (no FK), so they don't
            // cascade — delete them explicitly, atomically with the user.
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.CollectionEntries.Where(e => e.UserId == id).ExecuteDeleteAsync();
            await db.TradeListEntries.Where(e => e.UserId == id).ExecuteDeleteAsync();

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Results.Problem(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await transaction.CommitAsync();
            return Results.NoContent();
        });

        group.MapDelete("/users/{id:guid}/share", async (Guid id, ApplicationDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.TradeListShareHandle = null;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/settings", async (AppSettingsService settings) =>
            Results.Ok(new AdminSettingsResponse(await settings.IsRegistrationOpenAsync())));

        group.MapPut("/settings", async (UpdateAdminSettingsRequest request, AppSettingsService settings) =>
        {
            await settings.SetRegistrationOpenAsync(request.RegistrationOpen);
            return Results.Ok(new AdminSettingsResponse(request.RegistrationOpen));
        });

        group.MapGet("/catalog", (ICardCatalogAdmin catalog) => Results.Ok(ToResponse(catalog.GetStatus())));

        group.MapGet("/images", (ImageMirrorService images) => Results.Ok(ToResponse(images.GetStatus())));

        // Starts the download and returns straight away; the page polls GET /images for progress.
        group.MapPost("/images/download", (ImageMirrorService images) =>
            images.TryStart()
                ? Results.Accepted("/api/admin/images", ToResponse(images.GetStatus()))
                : Results.Conflict(new { error = "A download is already running." }));

        group.MapPost("/catalog/refresh", async (ICardCatalogAdmin catalog, CancellationToken cancellationToken) =>
        {
            try
            {
                await catalog.RefreshNowAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Results.Problem($"Could not fetch the card data: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
            }

            return Results.Ok(ToResponse(catalog.GetStatus()));
        });
    }

    private static ImageMirrorStatusResponse ToResponse(ImageMirrorStatus status) =>
        new(status.State, status.Total, status.Completed, status.Failed, status.StartedAt, status.FinishedAt,
            status.StoredCards, status.StoredPacks, status.StoredBytes, status.Error);

    private static CatalogStatusResponse ToResponse(CatalogStatus status) =>
        new(status.RepoTag, status.CardCount, status.LastRefreshedAt, status.LastError);

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
