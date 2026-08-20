using Approva.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Approva.Infrastructure.Notifications;

/// <summary>Fallback used when no email provider API key is configured (RESEND_API_KEY).
/// Logs the notification instead of sending it, so the approval flow keeps working end
/// to end in local/demo environments without requiring a paid email account.</summary>
public class LogNotificationSender : INotificationSender
{
    private readonly ILogger<LogNotificationSender> _logger;

    public LogNotificationSender(ILogger<LogNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[email fallback — sin RESEND_API_KEY] To: {ToEmail} | Subject: {Subject} | Body: {Body}",
            toEmail, subject, body);
        return Task.CompletedTask;
    }
}
