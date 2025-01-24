using LumiSky.Core.Services;
using SlimMessageBus;

namespace LumiSky.Core.Handlers;

// Helper handler to send notifications from the message bus.

public class NotificationHandler(NotificationService notificationService) :
    IConsumer<NotificationMessage>
{
    public async Task OnHandle(NotificationMessage message)
    {
        await notificationService.SendNotification(message);
    }
}
