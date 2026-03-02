using DotNetLibrary.Core.NotificationProcessor.Contracts;
using DotNetLibrary.Core.NotificationProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetLibrary.Core.NotificationProcessor.Services;

public class SmsNotificationChannel : INotificationChannel
{
    private readonly NotificationOptions _options;
    private readonly ILogger<SmsNotificationChannel> _logger;

    public SmsNotificationChannel(IOptions<NotificationOptions> options, ILogger<SmsNotificationChannel> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public CommunicationMedium Medium => CommunicationMedium.Sms;

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Sms.Enabled)
        {
            _logger.LogWarning("SMS is disabled. Message to {Recipient} was not sent.", message.To);
            return Task.CompletedTask;
        }

        _logger.LogInformation("SMS notification queued for {Recipient}: {Content}", message.To, message.Content);
        return Task.CompletedTask;
    }
}
