using LumiSky.Core.DomainEvents;
using LumiSky.Core.Services;
using SlimMessageBus;

namespace LumiSky.Core.Handlers;

public class PanoramaHandler :
    IConsumer<NewPanoramaEvent>
{
    private readonly ImageService _imageService;

    public PanoramaHandler(ImageService imageService)
    {
        _imageService = imageService;
    }

    public Task OnHandle(NewPanoramaEvent message)
    {
        _imageService.SetLatestPanorama(message.Filename);
        return Task.CompletedTask;
    }
}
