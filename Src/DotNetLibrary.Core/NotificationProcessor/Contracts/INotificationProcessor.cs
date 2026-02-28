using DotNetLibrary.Core.NotificationProcessor.Models;

namespace DotNetLibrary.Core.NotificationProcessor.Contracts;

public interface INotificationProcessor
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
