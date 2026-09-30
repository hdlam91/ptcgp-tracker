namespace PtcgpTracker.Api.Services;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>Empty means email isn't configured yet — <see cref="SmtpEmailSender"/> logs and no-ops.</summary>
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public string FromAddress { get; set; } = "noreply@ptcgp-tracker.local";

    public string FromName { get; set; } = "PTCGP Tracker";
}
