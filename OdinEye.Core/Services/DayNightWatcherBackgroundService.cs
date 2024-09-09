using Microsoft.Extensions.Hosting;
using OdinEye.Core.DomainEvents;
using SlimMessageBus;

namespace OdinEye.Core.Services;

public class DayNightWatcherBackgroundService : BackgroundService
{
    private static readonly string SourceContextName = "DayNightWatcher";

    private readonly IHostApplicationLifetime _lifetime;
    private readonly IMessageBus _messageBus;
    private readonly SunService _sunService;

    private SkyState _prevSkyState;

    private enum SkyState
    {
        Day,
        Night,
    }

    public DayNightWatcherBackgroundService(
        IHostApplicationLifetime lifetime,
        IMessageBus messageBus,
        SunService sunService)
    {
        _lifetime = lifetime;
        _messageBus = messageBus;
        _sunService = sunService;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _prevSkyState = _sunService.IsDaytime ? SkyState.Day : SkyState.Night;

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
        {
            return;
        }

        using (var _ = Serilog.Context.LogContext.PushProperty("SourceContext", SourceContextName))
            Log.Information("Starting Day Night Watcher");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentSkyState = _sunService.IsDaytime ? SkyState.Day : SkyState.Night;

                if (currentSkyState != _prevSkyState)
                {
                    using (var _ = Serilog.Context.LogContext.PushProperty("SourceContext", SourceContextName))
                        Log.Information("Changing from {PreviousState} to {CurrentState}", _prevSkyState, currentSkyState);

                    if (currentSkyState == SkyState.Day)
                    {
                        await _messageBus.Publish(new BecomingDaytimeEvent()).ConfigureAwait(false);
                    }
                    else if (currentSkyState == SkyState.Night)
                    {
                        await _messageBus.Publish(new BecomingNighttimeEvent()).ConfigureAwait(false);
                    }
                }

                _prevSkyState = currentSkyState;
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Log.Error(e, "Unhandled exception in Day Night Watcher");
            }

            // Short delay to prevent the service from restarting too fast
            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        var canceledSource = new TaskCompletionSource();

        using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());
        using var reg2 = stoppingToken.Register(() => canceledSource.SetResult());

        var completedTask = await Task.WhenAny(startedSource.Task, canceledSource.Task).ConfigureAwait(false);

        return completedTask == startedSource.Task;
    }
}
