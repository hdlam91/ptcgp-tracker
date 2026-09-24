using Microsoft.AspNetCore.Identity;
using PtcgpTracker.Api.Data.Entities;

namespace PtcgpTracker.Api.Services;

/// <summary>
/// Admin role management, plus the config-driven bootstrap that makes the first
/// admin possible: emails listed in <c>Admin:Emails</c> (env <c>Admin__Emails</c>,
/// comma-separated) are promoted on startup and when they register. Config only
/// ever promotes — demotion happens through the admin settings page.
/// </summary>
public class AdminRoleService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IConfiguration configuration)
{
    public async Task EnsureRoleAsync()
    {
        if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Admin));
        }
    }

    public async Task SetAdminAsync(ApplicationUser user, bool isAdmin)
    {
        await EnsureRoleAsync();

        var alreadyAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (isAdmin && !alreadyAdmin)
        {
            await userManager.AddToRoleAsync(user, AppRoles.Admin);
        }
        else if (!isAdmin && alreadyAdmin)
        {
            await userManager.RemoveFromRoleAsync(user, AppRoles.Admin);
        }
    }

    public bool IsConfiguredAdmin(string? email) =>
        email is not null && ConfiguredEmails().Contains(email, StringComparer.OrdinalIgnoreCase);

    public async Task PromoteConfiguredAdminsAsync()
    {
        await EnsureRoleAsync();

        foreach (var email in ConfiguredEmails())
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                await SetAdminAsync(user, true);
            }
        }
    }

    private string[] ConfiguredEmails() =>
        (configuration["Admin:Emails"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
