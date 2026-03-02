using DotNetLibrary.Core.NotificationProcessor.Models;

namespace DotNetLibrary.Core.NotificationProcessor.Contracts;

public interface INotificationChannel
{
    CommunicationMedium Medium { get; }
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
