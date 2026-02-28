using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using DotNetLibrary.Core.NotificationProcessor.Contracts;
using DotNetLibrary.Core.NotificationProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetLibrary.Core.NotificationProcessor.Services;

public class EmailNotificationChannel : INotificationChannel
{
    private readonly NotificationOptions _notificationOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(IOptions<NotificationOptions> notificationOptions, IHttpClientFactory httpClientFactory, ILogger<EmailNotificationChannel> logger)
    {
        _notificationOptions = notificationOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public CommunicationMedium Medium => CommunicationMedium.Email;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var provider = _notificationOptions.Email.Provider.Trim();

        if (provider.Equals("AzureEmailService", StringComparison.OrdinalIgnoreCase))
        {
            await SendUsingAzureEmailServiceAsync(message, cancellationToken);
            return;
        }

        await SendUsingGmailSmtpAsync(message, cancellationToken);
    }

    private async Task SendUsingGmailSmtpAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        var smtp = _notificationOptions.Email.GmailSmtp;
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            Credentials = new NetworkCredential(smtp.Username, smtp.Password),
            EnableSsl = smtp.EnableSsl
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_notificationOptions.Email.SenderEmail, _notificationOptions.Email.SenderDisplayName),
            Subject = message.Subject ?? string.Empty,
            Body = message.Content,
            IsBodyHtml = true
        };

        mailMessage.To.Add(message.To);
        await client.SendMailAsync(mailMessage, cancellationToken);
        _logger.LogInformation("Email notification sent to {Recipient}", message.To);
    }

    private async Task SendUsingAzureEmailServiceAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        var options = _notificationOptions.Email.AzureEmailService;
        var endpoint = options.Endpoint.TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/emails:send?api-version=2023-03-31");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessKey);

        var payload = new
        {
            senderAddress = _notificationOptions.Email.SenderEmail,
            content = new
            {
                subject = message.Subject ?? string.Empty,
                html = message.Content
            },
            recipients = new
            {
                to = new[]
                {
                    new { address = message.To }
                }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient(nameof(EmailNotificationChannel));
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Azure Email Service failed with status {StatusCode}: {Content}", response.StatusCode, content);
            throw new InvalidOperationException("Unable to send email via Azure Email Service.");
        }

        _logger.LogInformation("Email notification sent to {Recipient} via Azure Email Service", message.To);
    }
}
