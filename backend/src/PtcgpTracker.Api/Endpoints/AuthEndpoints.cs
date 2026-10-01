using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppSettingsService settings,
            AdminRoleService adminRoles,
            ConfirmationEmailSender confirmationEmail,
            HttpContext httpContext) =>
        {
            if (!await settings.IsRegistrationOpenAsync())
            {
                return Results.Json(new { error = "registration_closed" }, statusCode: StatusCodes.Status403Forbidden);
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                DisplayName = request.DisplayName,
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.ValidationProblem(
                    result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            // Before sign-in, so the session's role claims are right from the first request.
            var isAdmin = adminRoles.IsConfiguredAdmin(user.Email);
            if (isAdmin)
            {
                await adminRoles.SetAdminAsync(user, true);
            }

            // Configured admins skip this entirely — they're already trusted via server config,
            // and requiring confirmation for them risks locking out the only admin if SMTP isn't
            // set up yet.
            if (!isAdmin && await settings.IsEmailConfirmationRequiredAsync())
            {
                await confirmationEmail.SendAsync(httpContext, userManager, user);
                return Results.Created($"/api/auth/me", new RegisterResponse(true, null));
            }

            // Nothing pending for this account (confirmation is off, or this email is exempt) —
            // EmailConfirmed means "no outstanding confirmation", not literally "clicked a link",
            // so the admin Users list only ever flags accounts that actually have one pending.
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);

            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Created($"/api/auth/me", new RegisterResponse(false, new UserResponse(user.Id, user.Email!, user.DisplayName, isAdmin)));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AppSettingsService settings,
            AdminRoleService adminRoles) =>
        {
            var result = await signInManager.PasswordSignInAsync(
                request.Email, request.Password, isPersistent: true, lockoutOnFailure: false);

            if (result.RequiresTwoFactor)
            {
                // The password was right, but SignInManager hasn't completed the sign-in — it's
                // stashed who's mid-login in its own short-lived cookie for /login/2fa to finish.
                return Results.Ok(new LoginResponse(true, false, null));
            }

            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (await IsBlockedByUnconfirmedEmailAsync(user!, settings, adminRoles))
            {
                await signInManager.SignOutAsync();
                return Results.Ok(new LoginResponse(false, true, null));
            }

            var isAdmin = await userManager.IsInRoleAsync(user!, AppRoles.Admin);
            return Results.Ok(new LoginResponse(false, false, new UserResponse(user!.Id, user.Email!, user.DisplayName, isAdmin)));
        });

        group.MapPost("/login/2fa", async (
            TwoFactorLoginRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AppSettingsService settings,
            AdminRoleService adminRoles) =>
        {
            // Fetched before the sign-in call below, which clears the intermediate "who's
            // mid-login" cookie this reads from once it succeeds.
            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = request.IsRecoveryCode
                ? await signInManager.TwoFactorRecoveryCodeSignInAsync(request.Code)
                : await signInManager.TwoFactorAuthenticatorSignInAsync(request.Code, isPersistent: true, rememberClient: request.RememberDevice);

            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }

            if (await IsBlockedByUnconfirmedEmailAsync(user, settings, adminRoles))
            {
                await signInManager.SignOutAsync();
                return Results.Ok(new LoginResponse(false, true, null));
            }

            var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
            return Results.Ok(new LoginResponse(false, false, new UserResponse(user.Id, user.Email!, user.DisplayName, isAdmin)));
        });

        group.MapPost("/confirm-email", async (
            ConfirmEmailRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Token"] = ["That confirmation link is invalid or has expired."],
                });
            }

            var result = await userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            // One less step: confirming signs you in immediately.
            await signInManager.SignInAsync(user, isPersistent: true);
            var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
            return Results.Ok(new UserResponse(user.Id, user.Email!, user.DisplayName, isAdmin));
        });

        group.MapPost("/resend-confirmation", async (
            ResendConfirmationRequest request,
            UserManager<ApplicationUser> userManager,
            ConfirmationEmailSender confirmationEmail,
            HttpContext httpContext) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is not null && !user.EmailConfirmed)
            {
                await confirmationEmail.SendAsync(httpContext, userManager, user);
            }

            // Same response either way — never reveal whether an email is registered or confirmed.
            return Results.NoContent();
        }).RequireRateLimiting("resend-confirmation");

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is not null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                // Always the same origin the request itself came in on — this app is only ever
                // deployed same-origin (Vite's dev proxy locally, nginx in prod), so there's no
                // separate "public URL" setting to keep in sync with reality.
                var resetUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/reset-password"
                    + $"?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

                await emailSender.SendAsync(
                    request.Email,
                    "Reset your PTCGP Tracker password",
                    $"Someone asked to reset the password for this account.\n\n"
                        + $"Reset it here: {resetUrl}\n\n"
                        + "If this wasn't you, you can ignore this email.");
            }

            // Same response either way — never reveal whether an email is registered.
            return Results.NoContent();
        }).RequireRateLimiting("forgot-password");

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                // Same shape as a bad token, so this can't be used to probe which emails exist.
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Token"] = ["That reset link is invalid or has expired."],
                });
            }

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            return Results.NoContent();
        });

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new UserResponse(user.Id, user.Email!, user.DisplayName, principal.IsInRole(AppRoles.Admin)));
        }).RequireAuthorization();
    }

    private static async Task<bool> IsBlockedByUnconfirmedEmailAsync(
        ApplicationUser user, AppSettingsService settings, AdminRoleService adminRoles) =>
        !user.EmailConfirmed
        && !adminRoles.IsConfiguredAdmin(user.Email)
        && await settings.IsEmailConfirmationRequiredAsync();
}
