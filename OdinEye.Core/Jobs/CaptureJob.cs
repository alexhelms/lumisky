using OdinEye.Core.Data;
using OdinEye.Core.Devices;
using OdinEye.Core.Imaging;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using Quartz;

namespace OdinEye.Core.Jobs;

[DisallowConcurrentExecution]
public class CaptureJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Capture, JobConstants.Groups.Allsky);

    private readonly IProfileProvider _profile;
    private readonly AppDbContext _dbContext;
    private readonly DeviceFactory _deviceFactory;
    private readonly FilenameGenerator _filenameGenerator;
    private readonly SunService _sunService;
    private readonly ExposureService _exposureTrackingService;

    public CaptureJob(
        IProfileProvider profile,
        AppDbContext dbContext,
        DeviceFactory deviceFactory,
        FilenameGenerator filenameGenerator,
        SunService dayNightService,
        ExposureService exposureTrackingService)
    {
        _profile = profile;
        _dbContext = dbContext;
        _deviceFactory = deviceFactory;
        _filenameGenerator = filenameGenerator;
        _sunService = dayNightService;
        _exposureTrackingService = exposureTrackingService;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        IndiCamera? camera = null;

        try
        {
            camera = await CreateCamera(context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();

            using var image = await ExposeImage(camera, context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();

            var filename = SaveImage(image);
            context.CancellationToken.ThrowIfCancellationRequested();

            int rawImageId = await PersistRawImage(filename, image);

            await context.Scheduler.TriggerJob(
                ProcessingJob.Key,
                new JobDataMap
                {
                    [nameof(ProcessingJob.RawImageId)] = rawImageId,
                });
        }
        finally
        {
            if (camera is not null)
            {
                try
                {
                    await camera.DisconnectAsync();
                    camera.Dispose();
                    camera = null;
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Error disconnecting from camera");
                    throw;
                }
            }
        }
    }

    private async Task<IndiCamera> CreateCamera(CancellationToken token)
    {
        var camera = _deviceFactory.CreateCamera();
        if (camera is null)
            throw new NullReferenceException("Camera is null");

        bool connected = await camera.ConnectAsync(token);
        if (!connected)
            throw new NotConnectedException();

        return camera;
    }

    private async Task<AllSkyImage> ExposeImage(IndiCamera camera, CancellationToken token)
    {
        bool isDay = _sunService.IsDaytime;
        var exposureParameters = new ExposureParameters
        {
            Duration = _exposureTrackingService.GetNextExposure(),
            Gain = isDay ? _profile.Current.Camera.DaytimeGain : _profile.Current.Camera.NighttimeGain,
            Offset = _profile.Current.Camera.Offset,
        };

        Log.Information("Capturing image {Exposure:F6} sec, {Gain} gain, {Offset} offset",
            exposureParameters.Duration.TotalSeconds,
            exposureParameters.Gain,
            exposureParameters.Offset);

        var image = await camera.TakeImageAsync(exposureParameters, token);
        token.ThrowIfCancellationRequested();
        if (image is null)
            throw new NullReferenceException("Image is null");

        return image;
    }

    private string SaveImage(AllSkyImage image)
    {
        var timestamp = image.Metadata.ExposureUtc?.ToLocalTime() ?? DateTime.Now;
        var filename = _filenameGenerator.CreateFilename("raw", timestamp, ".fits");
        Directory.CreateDirectory(Path.GetDirectoryName(filename)!);

        image.SaveAsFits(filename, ImageOutputType.UInt16);
        return filename;
    }

    private async Task<int> PersistRawImage(string filename, AllSkyImage image)
    {
        var rawImage = new Data.RawImage
        {
            Filename = filename,
            ExposedOn = new DateTimeOffset(image.Metadata.ExposureUtc.GetValueOrDefault()).ToUnixTimeSeconds(),
        };

        _dbContext.RawImages.Add(rawImage);
        await _dbContext.SaveChangesAsync();
        return rawImage.Id;
    }
}
