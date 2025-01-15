using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using LumiSky.Core.Devices;
using LumiSky.Core.DomainEvents;
using LumiSky.Core.Imaging;
using LumiSky.Core.Imaging.Processing;
using LumiSky.Core.IO;
using LumiSky.Core.Primitives;
using SlimMessageBus;

namespace LumiSky.Core.Services;

public class FocusService : IDisposable
{
    private readonly IMessageBus _bus;
    private readonly DeviceFactory _deviceFactory;
    private readonly NotificationService _notificationService;

    private CancellationTokenSource? _cts;

    public event EventHandler? Stopped;

    public bool IsRunning { get; private set; }
    public TimeSpan Exposure { get; set; } = TimeSpan.FromSeconds(1);
    public int Gain { get; set; }

    public FocusService(
        IMessageBus bus,
        DeviceFactory deviceFactory,
        NotificationService notificationService)
    {
        _bus = bus;
        _deviceFactory = deviceFactory;
        _notificationService = notificationService;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public Task Start() => Start(TimeSpan.FromSeconds(1), 0);

    public Task Start(TimeSpan exposure, int gain)
    {
        if (IsRunning) return Task.CompletedTask;

        Exposure = exposure;
        Gain = gain;
        IsRunning = true;

        _cts = new CancellationTokenSource();
        _ = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();

        while (IsRunning)
            await Task.Delay(10);
    }

    private async Task Run()
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);

        int failedCaptureCount = 0;

        try
        {
            throw new Exception("Test exception!");

            if (_cts is null)
                throw new NullReferenceException("Token is null");

            var token = _cts.Token;

            IndiCamera camera = await _deviceFactory.GetOrCreateConnectedCamera(token);

            while (!token.IsCancellationRequested)
            {
                if (failedCaptureCount > 3)
                {
                    throw new Exception("Too many failed captures");
                }

                var exposureParameters = new ExposureParameters
                {
                    Duration = Exposure,
                    Gain = Gain,
                    Offset = 0,
                };

                using var image = await camera.TakeImageAsync(exposureParameters, token);
                token.ThrowIfCancellationRequested();

                if (image is null)
                {
                    failedCaptureCount++;
                    continue;
                }
                else
                {
                    failedCaptureCount = 0;
                }

                var size = ProcessAndSaveImage(image);
                
                await _bus.Publish(new NewFocusEvent
                {
                    Filename = LumiSkyPaths.LatestFocusImage,
                    Size = size,
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while focusing");
            await _notificationService.SendNotification(new NotificationMessage
            {
                Summary = "Error While Focusing",
                Detail = new string(e.Message.Take(100).ToArray()),
                Type = NotificationType.Error,
            });
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            IsRunning = false;
            Stopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private Size ProcessAndSaveImage(AllSkyImage rawImage)
    {
        using AllSkyImage debayeredImage = Debayer.FromImage(rawImage);
        debayeredImage.StretchLinked();

        using Mat uint8Mat = debayeredImage.To8bitMat();
        using var image = new Image<Rgb, byte>(uint8Mat);

        var encoderParameters = new List<KeyValuePair<ImwriteFlags, int>>();
        CvInvoke.Imwrite(LumiSkyPaths.LatestFocusImage, image, encoderParameters.ToArray());

        return new Size(image.Width, image.Height);
    }
}
