using LumiSky.Core.Imaging;
using LumiSky.Core.IO;
using LumiSky.Core.Profile;
using LumiSky.INDI.Primitives;
using LumiSky.INDI.Protocol;

namespace LumiSky.Core.Devices;

public class IndiCamera : ICamera, IDisposable
{
    private readonly IProfileProvider _profile;

    private IndiClient? _client;
    private IndiDevice? _device;

    public IndiCamera(IProfileProvider profile)
    {
        _profile = profile;
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _client?.Dispose();
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            var name = _profile.Current.Camera.Name;
            if (string.IsNullOrWhiteSpace(name))
                name = $"{_profile.Current.Camera.IndiHostname}:{_profile.Current.Camera.IndiPort}";
            if (string.IsNullOrWhiteSpace(name))
                name = "Indi Camera";
            throw new NotConnectedException($"{name} not connected");
        }
    }

    public async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        var deviceName = _profile.Current.Camera.Name;
        var hostname = _profile.Current.Camera.IndiHostname;
        var port = _profile.Current.Camera.IndiPort;
        _client = new IndiClient(hostname, port);

        try
        {
            await _client.Connect();
            if (!_client.IsConnected)
                return false;

            if (_client.Connection is not null)
                _client.Connection.Disconnected += Connection_Disconnected;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error connecting to INDI server {Hostname}:{Port}", hostname, port);
            return false;
        }

        // The device can take a few moments to show up since we are waiting for INDI messages to arrive.
        using (var deviceCts = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            deviceCts.CancelAfter(TimeSpan.FromSeconds(3));
            do
            {
                _device = _client.Connection!.Devices.GetDeviceOrNull(deviceName);
                if (_device is not null)
                    break;
            } while (!deviceCts.IsCancellationRequested);
        }
        
        if (_device is null)
        {
            _client.Disconnect();
            return false;
        }

        try
        {
            await _device.Connect();
            if (!_device.IsConnected)
            {
                _device = null;
                return false;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error connecting to INDI camera {Name}", deviceName);
            return false;
        }

        IsConnected = true;
        OnConnect();
        return true;
    }

    private void Connection_Disconnected()
    {
        IsConnected = false;
        OnDisconnect();

        if (_client?.Connection is not null)
            _client.Connection.Disconnected -= Connection_Disconnected;
    }

    public async Task DisconnectAsync()
    {
        if (!IsConnected) return;
        ArgumentNullException.ThrowIfNull(_client);
        ArgumentNullException.ThrowIfNull(_device);

        await _device.Disconnect();
        _client.Disconnect();
        IsConnected = false;
        OnDisconnect();
    }

    public async Task<AllSkyImage?> TakeImageAsync(ExposureParameters parameters, CancellationToken token = default)
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(_device);

        int gain = Math.Clamp(parameters.Gain, GainMin, GainMax);
        int offset = Math.Clamp(parameters.Offset, OffsetMin, OffsetMax);
        var exposure = Math.Clamp(parameters.Duration.TotalSeconds, ExposureMin.TotalSeconds, ExposureMax.TotalSeconds);
        var timeout = parameters.Duration + TimeSpan.FromSeconds(5);

        try
        {
            token.Register(async () =>
            {
                try
                {
                    await AbortImageAsync();
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Error aborting exposure when canceled");
                }
            });

            List<Task> tasks = [];
            tasks.Add(_device.Set<IndiSwitch>("CCD_TRANSFER_FORMAT", ["FORMAT_FITS", "FORMAT_NATIVE"], [true, false]));

            // TODO: support binning
            tasks.Add(_device.Set<IndiNumber>("CCD_BINNING", ["HOR_BIN", "VER_BIN"], [1, 1]));

            if (HasGain)
                tasks.Add(_device.Set<IndiNumber>("CCD_CONTROLS", "Gain", gain));

            if (HasOffset)
                tasks.Add(_device.Set<IndiNumber>("CCD_CONTROLS", "Offset", offset));

            await Task.WhenAll(tasks);

            var exposureStart = DateTime.UtcNow;

            // This blocks for the duration of the exposure + downloading the image
            await _device.Set<IndiNumber>("CCD_EXPOSURE", "CCD_EXPOSURE_VALUE", exposure, timeout, token);

            token.ThrowIfCancellationRequested();

            if (_device.Properties.TryGet("CCD1", out var vec) &&
                vec is IndiVector<IndiBlob> indiBlobs)
            {
                var fitsData = indiBlobs[0].Value ?? [];
                if (fitsData.Length == 0)
                {
                    Log.Error("INDI camera return empty image data");
                    return null;
                }

                using var tempFitsFile = new TemporaryFile();
                await File.WriteAllBytesAsync(tempFitsFile.Path, fitsData, token);
                var image = AllSkyImage.FromFits(tempFitsFile.Path);
                image.Metadata.CameraName = image.Metadata.CameraName is not null ? image.Metadata.CameraName : Name;
                image.Metadata.ExposureUtc = image.Metadata.ExposureUtc.HasValue ? image.Metadata.ExposureUtc : exposureStart;
                image.Metadata.ExposureDuration = image.Metadata.ExposureDuration.HasValue ? image.Metadata.ExposureDuration : parameters.Duration;
                image.Metadata.Gain = image.Metadata.Gain.HasValue ? image.Metadata.Gain : parameters.Gain;
                image.Metadata.Offset = image.Metadata.Offset.HasValue ? image.Metadata.Offset : parameters.Offset;
                image.Metadata.Binning = image.Metadata.Binning.HasValue ? image.Metadata.Binning : 1;
                image.Metadata.PixelSize = image.Metadata.PixelSize.HasValue ? image.Metadata.PixelSize : PixelSize;
                image.Metadata.FocalLength = _profile.Current.Camera.FocalLength;
                image.Metadata.Location = _profile.Current.Location.Location;
                image.Metadata.Latitude = _profile.Current.Location.Latitude;
                image.Metadata.Longitude = _profile.Current.Location.Longitude;
                image.Metadata.Elevation = _profile.Current.Location.Elevation;

                if (BayerPattern == BayerPattern.None)
                {
                    // Images must have a bayer pattern. Monochrome is not supported
                    // so pick a default bayer pattern.
                    image.Metadata.BayerPattern = BayerPattern.RGGB;
                }

                return image;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Log.Error(e, "Error taking image with INDI camera {Name}", _profile.Current.Camera.Name);
        }

        return null;
    }

    public async Task AbortImageAsync()
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(_device);

        try
        {
            if (_device.TryGet<IndiSwitch>("CCD_ABORT_EXPOSURE", "ABORT", out _))
            {
                await _device.Set<IndiSwitch>("CCD_ABORT_EXPOSURE", "ABORT", true);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Error aborting exposure");
        }
    }

    private void OnConnect()
    {
        ArgumentNullException.ThrowIfNull(_device);

        var indiExposure = _device.Get<IndiNumber>("CCD_EXPOSURE", "CCD_EXPOSURE_VALUE");
        ExposureMin = TimeSpan.FromSeconds(indiExposure.Min);
        ExposureMax = TimeSpan.FromSeconds(indiExposure.Max);

        if (_device.TryGet<IndiNumber>("CCD_CONTROLS", "Gain", out var indiGain))
        {
            HasGain = true;
            Gain = (int)indiGain!.Value;
            GainMin = (int)indiGain.Min;
            GainMax = (int)indiGain.Max;
        }
        else
        {
            Log.Warning("INDI property CCD_CONTROLS:Gain not found");
            HasGain = false;
            Gain = 0;
            GainMin = 0;
            GainMax = 0;
        }

        if (_device.TryGet<IndiNumber>("CCD_CONTROLS", "Offset", out var indiOffset))
        {
            HasOffset = true;
            Offset = (int)indiOffset!.Value;
            OffsetMin = (int)indiOffset.Min;
            OffsetMax = (int)indiOffset.Max;
        }
        else
        {
            Log.Warning("INDI property CCD_CONTROLS:Offset not found");
            HasOffset = false;
            Offset = 0;
            OffsetMin = 0;
            OffsetMax = 0;
        }

        var indiPixelSize = _device.Get<IndiNumber>("CCD_INFO", "CCD_PIXEL_SIZE");
        PixelSize = indiPixelSize.Value;

        if (_device.TryGet<IndiText>("CCD_CFA", "CFA_TYPE", out var indiCfa))
        {
            Enum.TryParse<BayerPattern>(indiCfa!.Value, true, out var bayerPattern);
            BayerPattern = bayerPattern;
        }
        else
        {
            Log.Warning("Monochrome cameras are not supported. The image will be debayered as RGGB.");
            BayerPattern = BayerPattern.None;
        }
    }

    private void OnDisconnect()
    {
        Gain = 0;
        GainMin = 0;
        GainMax = 0;
        Offset = 0;
        OffsetMin = 0;
        OffsetMax = 0;
        PixelSize = 0;
        BayerPattern = BayerPattern.None;
    }

    public string Name => _profile.Current.Camera.Name;
    public bool IsConnected { get; private set; }
    public bool HasGain { get; private set; }
    public bool HasOffset { get; private set; }
    public TimeSpan ExposureMin { get; private set; }
    public TimeSpan ExposureMax { get; private set; }
    public int Gain { get; private set; }
    public int GainMin { get; private set; }
    public int GainMax { get; private set; }
    public int Offset { get; private set; }
    public int OffsetMin { get; private set; }
    public int OffsetMax { get; private set; }
    public double PixelSize { get; private set; }
    public BayerPattern BayerPattern { get; private set; }
}