using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;

namespace PtcgpTracker.Api.Services;

public class AppSettingsService(ApplicationDbContext db)
{
    private const string RegistrationOpenKey = "RegistrationOpen";
    private const string EmailConfirmationRequiredKey = "EmailConfirmationRequired";

    /// <summary>Registration is open unless an admin has explicitly closed it.</summary>
    public async Task<bool> IsRegistrationOpenAsync()
    {
        var setting = await db.AppSettings.FindAsync(RegistrationOpenKey);
        return setting is null || setting.Value != bool.FalseString;
    }

    public async Task SetRegistrationOpenAsync(bool open)
    {
        var value = open.ToString();
        var setting = await db.AppSettings.FindAsync(RegistrationOpenKey);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = RegistrationOpenKey, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Off unless an admin has explicitly turned it on.</summary>
    public async Task<bool> IsEmailConfirmationRequiredAsync()
    {
        var setting = await db.AppSettings.FindAsync(EmailConfirmationRequiredKey);
        return setting is not null && setting.Value == bool.TrueString;
    }

    public async Task SetEmailConfirmationRequiredAsync(bool required)
    {
        var wasRequired = await IsEmailConfirmationRequiredAsync();
        var value = required.ToString();
        var setting = await db.AppSettings.FindAsync(EmailConfirmationRequiredKey);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = EmailConfirmationRequiredKey, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync();

        // Turning this on the first time can't retroactively require confirmation from everyone
        // who already registered under the old rules — that would lock out the entire existing
        // user base, not just new signups. Grandfather them in, once, right here.
        if (required && !wasRequired)
        {
            await GrandfatherExistingUsersAsync();
        }
    }

    /// <summary>
    /// Called once at startup. Every account created before this feature existed has
    /// <c>EmailConfirmed = false</c> from Identity's default — nobody ever asked them to confirm
    /// anything, so while the setting is off they shouldn't show as "unconfirmed" in the admin
    /// Users list. A no-op once everyone's already marked, and skipped entirely while the setting
    /// is on (new signups are correctly pending, and <see cref="SetEmailConfirmationRequiredAsync"/>
    /// already backfills the moment it's turned on).
    /// </summary>
    public async Task EnsureNoStaleUnconfirmedUsersAsync()
    {
        if (!await IsEmailConfirmationRequiredAsync())
        {
            await GrandfatherExistingUsersAsync();
        }
    }

    private async Task GrandfatherExistingUsersAsync() =>
        await db.Users.Where(u => !u.EmailConfirmed).ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailConfirmed, true));
}
