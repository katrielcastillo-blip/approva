using System.Net.Http.Headers;
using System.Net.Http.Json;
using Approva.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Approva.Infrastructure.Notifications;

/// <summary>Sends transactional email via the Resend API (https://resend.com).
/// Only registered when RESEND_API_KEY is present — see DependencyInjection.cs.</summary>
public class ResendNotificationSender : INotificationSender
{
    private readonly HttpClient _httpClient;
    private readonly string _fromAddress;
    private readonly ILogger<ResendNotificationSender> _logger;

    public ResendNotificationSender(HttpClient httpClient, string apiKey, string fromAddress, ILogger<ResendNotificationSender> logger)
    {
        _httpClient = httpClient;
        _fromAddress = fromAddress;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("emails", new
        {
            from = _fromAddress,
            to = new[] { toEmail },
            subject,
            html = body
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Resend devolvió {StatusCode} al enviar a {ToEmail}: {Error}", response.StatusCode, toEmail, error);
        }
    }
}
