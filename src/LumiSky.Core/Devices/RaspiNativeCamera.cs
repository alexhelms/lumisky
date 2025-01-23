using LumiSky.Core.Imaging;
using LumiSky.Core.Profile;
using LumiSky.Rpicam.Common;

namespace LumiSky.Core.Devices;

public class RaspiNativeCamera : ICamera
{
    private readonly IProfileProvider _profile;
    private readonly RpicamService _rpicamService;

    private CancellationTokenSource? _exposeCts;

    public string DeviceType => DeviceTypes.RaspiNative;

    public string Name => "Raspi Native Camera";

    public bool IsConnected { get; private set; }

    public RaspiNativeCamera(
        IProfileProvider profile,
        RpicamService rpicamService)
    {
        _profile = profile;
        _rpicamService = rpicamService;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _exposeCts?.Dispose();
    }

    public async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        IsConnected = await _rpicamService.AnyCameraAvailable(token);
        return IsConnected;
    }

    public void Disconnect()
    {
        _exposeCts?.Cancel();
    }

    public async Task<AllSkyImage?> TakeImageAsync(ExposureParameters parameters, CancellationToken token = default)
    {
        _exposeCts?.Cancel();
        _exposeCts?.Dispose();
        _exposeCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _exposeCts.CancelAfter(parameters.Duration + TimeSpan.FromSeconds(5));

        parameters = RaspiCamUtils.ClampExposureParameters(parameters);
        string args = RaspiCamUtils.CreateArgs(parameters, outputName: "img");

        try
        {
            var exposureStart = DateTime.UtcNow;

            var result = await _rpicamService.Execute(args, _exposeCts.Token);
            if (result.ExitCode != 0)
            {
                Log.Error("Raspi camera returned non-zero exit code: {ExitCode}", result.ExitCode);
                Log.Error("Raspi camera args: {Args}", args);
                Log.Error("Raspi camera stdout: {Stdout}", result.Stdout);
                Log.Error("Raspi camera Stderr: {Stdout}", result.Stderr);
                return null;
            }

            await _rpicamService.ConvertDngToTiff(_exposeCts.Token);

            var tiffFileInfo = new FileInfo(Path.Join(_rpicamService.WorkingDir, "img.dng.tiff"));
            if (!tiffFileInfo.Exists)
            {
                Log.Error("Image file not found");
                return null;
            }

            // Successful output puts text in stderr, NOT stdout.
            BayerPattern bayerPattern = RaspiCamUtils.GetBayerPatternFromOutput(result.Stderr);

            using var stream = tiffFileInfo.OpenRead();
            var image = AllSkyImage.FromTiff(stream);
            image.Metadata.CameraName = Name;
            image.Metadata.ExposureUtc = exposureStart;
            image.Metadata.ExposureDuration = parameters.Duration;
            image.Metadata.Gain = parameters.Gain;
            image.Metadata.Offset = parameters.Offset;
            image.Metadata.Binning = parameters.Binning;
            image.Metadata.FocalLength = _profile.Current.Camera.FocalLength;
            image.Metadata.Location = _profile.Current.Location.Location;
            image.Metadata.Latitude = _profile.Current.Location.Latitude;
            image.Metadata.Longitude = _profile.Current.Location.Longitude;
            image.Metadata.Elevation = _profile.Current.Location.Elevation;
            image.Metadata.BayerPattern = bayerPattern;

            return image;
        }
        catch (OperationCanceledException)
        {
            if (_exposeCts.IsCancellationRequested && !token.IsCancellationRequested)
                throw new TimeoutException("Timed out waiting for raspi camera image");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error taking image with raspi camera {Name}: {Message}", Name, e.Message);
        }

        return null;
    }

    public Task AbortImageAsync()
    {
        _exposeCts?.Cancel();
        return Task.CompletedTask;
    }
}
