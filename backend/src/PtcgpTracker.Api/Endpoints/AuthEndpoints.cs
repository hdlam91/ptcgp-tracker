using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PtcgpTracker.Api.Data.Entities;
using PtcgpTracker.Api.Models;

namespace PtcgpTracker.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
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

            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Created($"/api/auth/me", new UserResponse(user.Id, user.Email!, user.DisplayName));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var result = await signInManager.PasswordSignInAsync(
                request.Email, request.Password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            return Results.Ok(new UserResponse(user!.Id, user.Email!, user.DisplayName));
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
                : Results.Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
        }).RequireAuthorization();
    }
}
