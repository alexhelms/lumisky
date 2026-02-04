using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using LumiSky.Core.Devices;
using LumiSky.Core.DomainEvents;
using LumiSky.Core.Imaging;
using LumiSky.Core.Imaging.Processing;
using LumiSky.Core.IO;
using LumiSky.Core.Profile;
using SlimMessageBus;

namespace LumiSky.Core.Services;

public class FocusService : IDisposable
{
    private readonly IProfileProvider _profileProvider;
    private readonly IMessageBus _bus;
    private readonly DeviceFactory _deviceFactory;
    private readonly NotificationService _notificationService;

    private CancellationTokenSource? _cts;

    public event EventHandler? Stopped;

    public bool IsRunning { get; private set; }
    public TimeSpan Exposure { get; set; } = TimeSpan.FromSeconds(1);
    public int Gain { get; set; }
    public double[] Medians { get; set; } = [];

    public FocusService(
        IProfileProvider profileProvider,
        IMessageBus bus,
        DeviceFactory deviceFactory,
        NotificationService notificationService)
    {
        _profileProvider = profileProvider;
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
            if (_cts is null)
                throw new NullReferenceException("Token is null");

            var token = _cts.Token;

            using var camera = await _deviceFactory.GetCamera(token);

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
                    Offset = _profileProvider.Current.Camera.Offset,
                    Binning = _profileProvider.Current.Camera.Binning,
                };

                Medians = [];

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

                ProcessAndSaveImage(image);
                
                await _bus.Publish(new NewFocusEvent
                {
                    Filename = LumiSkyPaths.LatestFocusImage,
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while focusing: {Message}", e.Message);
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

    private void ProcessAndSaveImage(AllSkyImage rawImage)
    {
        using AllSkyImage debayeredImage = Debayer.FromImage(rawImage);

        // If the image is mono, debayered image is single channel
        Medians = new double[debayeredImage.Channels];
        for (int c = 0; c < debayeredImage.Channels; c++)
        {
            Medians[c] = debayeredImage.SubsampledMedian(c);
        }

        debayeredImage.StretchLinked();

        using Mat uint8Mat = debayeredImage.To8BitMat();
        using var image = new Image<Rgb, byte>(uint8Mat);

        var encoderParameters = new List<KeyValuePair<ImwriteFlags, int>>();
        CvInvoke.Imwrite(LumiSkyPaths.LatestFocusImage, image, encoderParameters.ToArray());
    }
}
