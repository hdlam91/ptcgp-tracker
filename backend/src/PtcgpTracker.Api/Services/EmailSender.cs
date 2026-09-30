using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PtcgpTracker.Api.Services;

/// <summary>General-purpose outbound email — not specific to any one feature (password
/// resets today, but the same interface works for anything sent later).</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string textBody);
}

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string textBody)
    {
        var smtp = options.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            // Not configured yet — the feature stays inert instead of throwing, so a
            // self-hoster who hasn't set up SMTP still has a working app.
            logger.LogWarning("Smtp:Host isn't configured; not sending {Subject} to {ToEmail}", subject, toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = textBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
