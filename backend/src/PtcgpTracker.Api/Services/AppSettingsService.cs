using PtcgpTracker.Api.Data;
using PtcgpTracker.Api.Data.Entities;

namespace PtcgpTracker.Api.Services;

public class AppSettingsService(ApplicationDbContext db)
{
    private const string RegistrationOpenKey = "RegistrationOpen";

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
}
