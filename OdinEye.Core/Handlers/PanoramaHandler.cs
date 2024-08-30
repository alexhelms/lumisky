﻿using OdinEye.Core.DomainEvents;
using OdinEye.Core.Services;
using SlimMessageBus;

namespace OdinEye.Core.Handlers;

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
        _imageService.SetLatestPanorama(message.Filename, message.Size);
        return Task.CompletedTask;
    }
}
