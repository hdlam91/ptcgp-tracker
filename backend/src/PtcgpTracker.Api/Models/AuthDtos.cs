namespace PtcgpTracker.Api.Models;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record TwoFactorLoginRequest(string Code, bool IsRecoveryCode, bool RememberDevice);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record ConfirmEmailRequest(string Email, string Token);

public record ResendConfirmationRequest(string Email);

public record UserResponse(Guid Id, string Email, string DisplayName, bool IsAdmin);

/// <summary>
/// <see cref="User"/> is null exactly when <see cref="RequiresTwoFactor"/> or
/// <see cref="RequiresEmailConfirmation"/> is true — the password was right, but the login isn't
/// complete until <c>/api/auth/login/2fa</c> succeeds, or the account's email is confirmed.
/// </summary>
public record LoginResponse(bool RequiresTwoFactor, bool RequiresEmailConfirmation, UserResponse? User);

/// <summary>
/// <see cref="User"/> is null exactly when <see cref="RequiresEmailConfirmation"/> is true — the
/// account was created, but isn't signed in until its email is confirmed.
/// </summary>
public record RegisterResponse(bool RequiresEmailConfirmation, UserResponse? User);
