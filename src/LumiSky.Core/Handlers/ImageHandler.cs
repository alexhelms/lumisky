using LumiSky.Core.DomainEvents;
using LumiSky.Core.Services;
using SlimMessageBus;

namespace LumiSky.Core.Handlers;

public class ImageHandler :
    IConsumer<NewImageEvent>
{
    private readonly ImageService _imageService;

    public ImageHandler(ImageService imageService)
    {
        _imageService = imageService;
    }

    public Task OnHandle(NewImageEvent message)
    {
        _imageService.SetLatestImage(message.Filename, message.Size);
        return Task.CompletedTask;
    }
}
