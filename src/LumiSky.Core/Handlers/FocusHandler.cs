using LumiSky.Core.DomainEvents;
using LumiSky.Core.Services;
using SlimMessageBus;

namespace LumiSky.Core.Handlers;

public class FocusHandler :
    IConsumer<NewFocusEvent>
{
    private readonly ImageService _imageService;

    public FocusHandler(ImageService imageService)
    {
        _imageService = imageService;
    }

    public Task OnHandle(NewFocusEvent message)
    {
        _imageService.SetLatestFocus(message.Filename);
        return Task.CompletedTask;
    }
}
