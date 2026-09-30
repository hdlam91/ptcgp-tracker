using System.Collections.Concurrent;
using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Tests.Infrastructure;

public record SentEmail(string ToEmail, string Subject, string TextBody);

/// <summary>Records every "sent" email instead of touching real SMTP, so tests can pull a
/// password-reset link straight out of the captured body.</summary>
internal class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentBag<SentEmail> sent = [];

    public IReadOnlyCollection<SentEmail> Sent => sent;

    public Task SendAsync(string toEmail, string subject, string textBody)
    {
        sent.Add(new SentEmail(toEmail, subject, textBody));
        return Task.CompletedTask;
    }
}
