namespace DotNetLibrary.Core.NotificationProcessor.Models;

public class NotificationMessage
{
    public CommunicationMedium Medium { get; set; }
    public string To { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Content { get; set; } = string.Empty;
}
