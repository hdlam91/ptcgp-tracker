using Microsoft.AspNetCore.Identity;
using PtcgpTracker.Api.Data.Entities;

namespace PtcgpTracker.Api.Services;

/// <summary>Builds and sends the "confirm your email" message — shared by registration (sends
/// the first one) and the admin's "resend confirmation" action (sends another).</summary>
public class ConfirmationEmailSender(IEmailSender emailSender)
{
    public async Task SendAsync(HttpContext httpContext, UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        // Same same-origin trick as the password-reset link: this app is only ever deployed
        // same-origin, so the incoming request's own Host is the address to send people back to.
        var confirmUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/confirm-email"
            + $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        await emailSender.SendAsync(
            user.Email!,
            "Confirm your PTCGP Tracker email",
            "Welcome! Confirm your email address to finish setting up your account.\n\n"
                + $"Confirm it here: {confirmUrl}\n\n"
                + "If this wasn't you, you can ignore this email.");
    }
}
