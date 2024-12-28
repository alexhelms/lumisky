namespace LumiSky.Core.Services;

public class NotificationService
{
    public event EventHandler<NotificationMessage>? Message;

    public async Task SendNotification(NotificationMessage message)
    {
        await Task.Run(() => Message?.Invoke(this, message));
    }
}

public record NotificationMessage
{
    public required NotificationType Type { get; set; }
    public required string Summary { get; init; }
    public required string Detail { get; init; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
}