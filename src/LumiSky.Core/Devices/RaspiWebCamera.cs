using LumiSky.Core.Imaging;
using LumiSky.Core.Profile;
using LumiSky.Rpicam.Common;
using System.Net.Http.Json;

namespace LumiSky.Core.Devices;

public class RaspiWebCamera : ICamera
{
    private readonly IProfileProvider _profile;

    private HttpClient _client;
    private CancellationTokenSource? _exposeCts;

    public string DeviceType => DeviceTypes.RaspiWeb;

    public string Name => "Raspi Web Camera";

    public bool IsConnected { get; private set; }

    public RaspiWebCamera(IProfileProvider profile)
    {
        _profile = profile;

        _client = CreateHttpClient();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _exposeCts?.Dispose();
        _client.Dispose();
    }

    private HttpClient CreateHttpClient(TimeSpan? timeout = null)
    {
        var client = new HttpClient();
        client.BaseAddress = new(_profile.Current.Camera.RaspiCameraUrl);
        client.Timeout = timeout ?? Timeout.InfiniteTimeSpan;
        return client;
    }

    public async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        using var client = CreateHttpClient(timeout: TimeSpan.FromSeconds(3));
        using var response = await client.GetAsync("/ping", token);
        IsConnected = response.IsSuccessStatusCode;
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

        parameters = RaspiCamUtils.ClampExposureParameters(parameters);
        string args = RaspiCamUtils.CreateArgs(parameters, outputName: "img");
        string escapedArgs = Uri.EscapeDataString(args);

        _exposeCts.CancelAfter(parameters.Duration + TimeSpan.FromSeconds(5));

        try
        {
            var exposureStart = DateTime.UtcNow;

            // Long poll, returns when the exposure is complete
            using var executeResponse = await _client.PostAsync($"/execute?args={escapedArgs}", content: null, cancellationToken: _exposeCts.Token);
            executeResponse.EnsureSuccessStatusCode();

            _exposeCts.Token.ThrowIfCancellationRequested();

            var result = await executeResponse.Content.ReadFromJsonAsync<RpicamResult>(_exposeCts.Token)
                ?? throw new Exception("Error deserializing raspi camera response");

            if (result.ExitCode != 0)
            {
                Log.Error("Raspi camera returned non-zero exit code: {ExitCode}", result.ExitCode);
                Log.Error("Raspi camera args: {Args}", args);
                Log.Error("Raspi camera stdout: {Stdout}", result.Stdout);
                Log.Error("Raspi camera Stderr: {Stdout}", result.Stderr);
                return null;
            }

            // LumiSky.Rpicam creates a TIFF from the DNG and the extension is appended.
            var tiff = Uri.EscapeDataString("img.dng.tiff");
            using var downloadResponse = await _client.GetAsync($"/download?filename={tiff}", _exposeCts.Token);
            downloadResponse.EnsureSuccessStatusCode();

            // Successful output puts text in stderr, NOT stdout.
            BayerPattern bayerPattern = RaspiCamUtils.GetBayerPatternFromOutput(result.Stderr);

            using var stream = await downloadResponse.Content.ReadAsStreamAsync(_exposeCts.Token);
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
