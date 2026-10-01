namespace PtcgpTracker.Api.Models;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ChangeEmailRequest(string NewEmail, string CurrentPassword);

public record DeleteAccountRequest(string CurrentPassword);

public record UpdateLocaleRequest(string Locale);

public record TwoFactorStatusResponse(bool Enabled);

public record TwoFactorSetupResponse(string SharedKey, string OtpAuthUri);

public record EnableTwoFactorRequest(string Code);

public record RecoveryCodesResponse(IEnumerable<string> RecoveryCodes);

public record DisableTwoFactorRequest(string CurrentPassword);

public record RegenerateRecoveryCodesRequest(string CurrentPassword);
