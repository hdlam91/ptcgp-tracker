namespace PtcgpTracker.Api.Models;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string Email, string DisplayName, bool IsAdmin);
