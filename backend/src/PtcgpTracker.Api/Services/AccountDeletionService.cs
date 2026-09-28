using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;

namespace PtcgpTracker.Api.Services;

/// <summary>
/// Deletes a user and every row that references them. Shared by the admin's "delete another
/// user" endpoint and the self-service "delete my own account" endpoint, so the two can't drift
/// apart as more per-user tables get added later.
/// </summary>
public class AccountDeletionService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
{
    public async Task<IdentityResult> DeleteAsync(ApplicationUser user)
    {
        // Collection and trade-list rows hold a plain UserId (no FK), so they don't
        // cascade — delete them explicitly, atomically with the user.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.CollectionEntries.Where(e => e.UserId == user.Id).ExecuteDeleteAsync();
        await db.TradeListEntries.Where(e => e.UserId == user.Id).ExecuteDeleteAsync();

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await transaction.CommitAsync();
        }

        return result;
    }
}
