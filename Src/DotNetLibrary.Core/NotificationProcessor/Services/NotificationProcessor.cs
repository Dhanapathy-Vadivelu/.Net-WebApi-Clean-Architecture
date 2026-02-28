using DotNetLibrary.Core.NotificationProcessor.Contracts;
using DotNetLibrary.Core.NotificationProcessor.Models;

namespace DotNetLibrary.Core.NotificationProcessor.Services;

public class NotificationProcessor : INotificationProcessor
{
    private readonly IReadOnlyDictionary<CommunicationMedium, INotificationChannel> _channels;

    public NotificationProcessor(IEnumerable<INotificationChannel> channels)
    {
        _channels = channels.ToDictionary(channel => channel.Medium);
    }

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(message.Medium, out var channel))
        {
            throw new InvalidOperationException($"No channel is registered for medium '{message.Medium}'.");
        }

        return channel.SendAsync(message, cancellationToken);
    }
}
