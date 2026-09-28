using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Endpoints;

/// <summary>
/// Self-service account management: change your own password or email, turn on TOTP
/// two-factor authentication, or delete your own account. The admin-only equivalents for
/// managing *other* users live in <see cref="AdminEndpoints"/>.
/// </summary>
public static class AccountEndpoints
{
    private const string Issuer = "PTCGP Tracker";

    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").RequireAuthorization();

        group.MapPost("/password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            // The password change rotates the security stamp; refresh the current session's
            // cookie so this request's own principal doesn't look stale on the next request.
            await signInManager.RefreshSignInAsync(user);
            return Results.NoContent();
        });

        group.MapPost("/email", async (
            ChangeEmailRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["CurrentPassword"] = ["Incorrect password."],
                });
            }

            // UserName is set to the email at registration, and sign-in looks accounts up by
            // UserName — keep the two in sync or the user gets locked out of logging in by email.
            var emailResult = await userManager.SetEmailAsync(user, request.NewEmail);
            if (!emailResult.Succeeded)
            {
                return Results.ValidationProblem(emailResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            var userNameResult = await userManager.SetUserNameAsync(user, request.NewEmail);
            if (!userNameResult.Succeeded)
            {
                return Results.ValidationProblem(userNameResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            return Results.NoContent();
        });

        group.MapDelete("", async (
            [FromBody] DeleteAccountRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AccountDeletionService accountDeletion) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["CurrentPassword"] = ["Incorrect password."],
                });
            }

            var result = await accountDeletion.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Results.Problem(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await signInManager.SignOutAsync();
            return Results.NoContent();
        });

        group.MapGet("/2fa", async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            return Results.Ok(new TwoFactorStatusResponse(await userManager.GetTwoFactorEnabledAsync(user)));
        });

        group.MapGet("/2fa/setup", async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            var key = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                key = await userManager.GetAuthenticatorKeyAsync(user);
            }

            var otpAuthUri = $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(user.Email!)}"
                + $"?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6";

            return Results.Ok(new TwoFactorSetupResponse(key!, otpAuthUri));
        });

        group.MapPost("/2fa/enable", async (
            EnableTwoFactorRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            var isValid = await userManager.VerifyTwoFactorTokenAsync(
                user, TokenOptions.DefaultAuthenticatorProvider, request.Code);
            if (!isValid)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Code"] = ["That code isn't valid. Check the time on your device and try again."],
                });
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
            return Results.Ok(new RecoveryCodesResponse(recoveryCodes!));
        });

        group.MapPost("/2fa/disable", async (
            DisableTwoFactorRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["CurrentPassword"] = ["Incorrect password."],
                });
            }

            await userManager.SetTwoFactorEnabledAsync(user, false);
            // Reset the secret too, so re-enabling later starts from a fresh QR code rather than
            // silently reactivating whatever an authenticator app still has saved from before.
            await userManager.ResetAuthenticatorKeyAsync(user);
            return Results.NoContent();
        });

        group.MapPost("/2fa/recovery-codes", async (
            RegenerateRecoveryCodesRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();

            if (!await userManager.GetTwoFactorEnabledAsync(user))
            {
                return Results.BadRequest(new { error = "Two-factor authentication isn't enabled." });
            }

            if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["CurrentPassword"] = ["Incorrect password."],
                });
            }

            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
            return Results.Ok(new RecoveryCodesResponse(recoveryCodes!));
        });
    }
}
