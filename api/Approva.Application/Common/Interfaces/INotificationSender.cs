namespace Approva.Application.Common.Interfaces;

public interface INotificationSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
