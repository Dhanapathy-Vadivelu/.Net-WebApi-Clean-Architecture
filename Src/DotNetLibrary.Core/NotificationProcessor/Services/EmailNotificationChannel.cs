using System.Net;
using System.Net.Mail;
using Azure;
using Azure.Communication.Email;
using DotNetLibrary.Core.NotificationProcessor.Contracts;
using DotNetLibrary.Core.NotificationProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetLibrary.Core.NotificationProcessor.Services;

public class EmailNotificationChannel : INotificationChannel
{
    private readonly NotificationOptions _notificationOptions;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(IOptions<NotificationOptions> notificationOptions, ILogger<EmailNotificationChannel> logger)
    {
        _notificationOptions = notificationOptions.Value;
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
        var emailClient = new EmailClient(new Uri(options.Endpoint), new AzureKeyCredential(options.AccessKey));

        var emailMessage = new EmailMessage(
            senderAddress: _notificationOptions.Email.SenderEmail,
            content: new EmailContent(message.Subject ?? string.Empty)
            {
                Html = message.Content
            },
            recipients: new EmailRecipients([
                new EmailAddress(message.To)
            ]));

        var response = await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
        if (response.Value.Status != EmailSendStatus.Succeeded)
        {
            _logger.LogError("Azure Email Service failed with operation status {Status}", response.Value.Status);
            throw new InvalidOperationException("Unable to send email via Azure Email Service.");
        }

        _logger.LogInformation("Email notification sent to {Recipient} via Azure Email Service", message.To);
    }
}
