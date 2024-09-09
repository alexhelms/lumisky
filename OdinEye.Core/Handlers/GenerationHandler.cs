using OdinEye.Core.DomainEvents;
using OdinEye.Core.Services;
using SlimMessageBus;

namespace OdinEye.Core.Handlers;

public class GenerationHandler :
    IConsumer<GenerationQueued>,
    IConsumer<GenerationStarting>,
    IConsumer<GenerationProgress>,
    IConsumer<GenerationComplete>
{
    private readonly GenerationService _generationService;

    public GenerationHandler(GenerationService generationService)
    {
        _generationService = generationService;
    }

    public Task OnHandle(GenerationQueued message)
    {
        _generationService.OnQueued(message);
        return Task.CompletedTask;
    }

    public Task OnHandle(GenerationStarting message)
    {
        _generationService.OnStarting(message);
        return Task.CompletedTask;
    }

    public Task OnHandle(GenerationProgress message)
    {
        _generationService.OnProgress(message);
        return Task.CompletedTask;
    }

    public Task OnHandle(GenerationComplete message)
    {
        _generationService.OnComplete(message);
        return Task.CompletedTask;
    }
}
