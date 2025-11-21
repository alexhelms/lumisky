using LumiSky.Core.Imaging;
using LumiSky.Core.Indi;
using LumiSky.Core.Indi.Parameters;
using LumiSky.Core.IO;
using LumiSky.Core.Profile;
using System.Text.Json;

namespace LumiSky.Core.Devices;

public class IndiCamera : ICamera, IDisposable
{
    private readonly IProfileProvider _profile;
    private readonly IndiClient _client = new();

    private IndiDevice? _device;
    private bool _isConnected;

    public IndiCamera(IProfileProvider profile)
    {
        _profile = profile;
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _client.Dispose();
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            Disconnect();

            var name = Name;
            if (string.IsNullOrWhiteSpace(Name))
                name = $"Camera {_profile.Current.Camera.IndiHostname}:{_profile.Current.Camera.IndiPort}";
            throw new NotConnectedException($"{name} not connected");
        }
    }

    public async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        var cameraName = Name;
        var hostname = _profile.Current.Camera.IndiHostname;
        var port = _profile.Current.Camera.IndiPort;

        try
        {
            await _client.Connect(hostname, port, token);
            _device = await _client.GetDevice(cameraName, token);
            await _device.EnableBlobs();

            if (!_client.IsConnected)
                return false;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error connecting to INDI server {Hostname}:{Port}", hostname, port);
            return false;
        }

        token.ThrowIfCancellationRequested();

        try
        {
            await _device.Change("CONNECTION", [("CONNECT", true), ("DISCONNECT", false)], token: token);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error connecting to INDI camera {Name}", cameraName);
            return false;
        }

        _isConnected = true;
        await OnConnect();
        return true;
    }

    public void Disconnect()
    {
        if (!IsConnected) return;

        _device = null;
        _client.Disconnect();
        _isConnected = false;
        OnDisconnect();
    }

    public async Task<AllSkyImage?> TakeImageAsync(ExposureParameters parameters, CancellationToken token = default)
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(_device);

        var exposure = Math.Clamp(parameters.Duration.TotalSeconds, ExposureMin.TotalSeconds, ExposureMax.TotalSeconds);
        var timeout = parameters.Duration + TimeSpan.FromSeconds(30);

        try
        {
            using var tokenRegistration = token.Register(async () =>
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

            // Transfer format and binning are common for all indi camera devices.
            tasks.Add(_device.Change("CCD_TRANSFER_FORMAT", [("FORMAT_FITS", true), ("FORMAT_NATIVE", false)]));
            tasks.Add(SetBinning(parameters.Binning, token));
            tasks.Add(SetGain(parameters.Gain, token));
            tasks.Add(SetOffset(parameters.Offset, token));
            tasks.Add(SetCustomProperties(token));

            await Task.WhenAll(tasks);

            var exposureStart = DateTime.UtcNow;

            token.ThrowIfCancellationRequested();

            await _device.Change("CCD_EXPOSURE", [("CCD_EXPOSURE_VALUE", exposure)]);
            await _device.WaitForChange("CCD1", timeout, token);

            var ccd1 = await _device.GetParameter("CCD1");
            var blobs = ccd1.GetItems<IndiBlob>();

            if (blobs is { Count: > 0 })
            {
                var fitsData = blobs.Values.First().Value;
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
            Log.Error(e, "Error taking image with INDI camera {Name}", Name);
        }

        return null;
    }

    public async Task AbortImageAsync()
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(_device);

        try
        {
            await _device.Change("CCD_ABORT_EXPOSURE", [("ABORT", true)]);
        }
        catch (TimeoutException te)
        {
            Log.Debug(te, "Error aborting exposure");
        }
        catch (Exception e)
        {
            Log.Warning(e, "Error aborting exposure");
        }
    }

    private async Task OnConnect()
    {
        ArgumentNullException.ThrowIfNull(_device);

        var indiExposureParameter = await _device.GetParameter("CCD_EXPOSURE");
        var indiExposureItems = indiExposureParameter.GetItems<IndiNumber>();
        var indiExposure = indiExposureItems["CCD_EXPOSURE_VALUE"];

        ExposureMin = TimeSpan.FromSeconds(indiExposure.Min);
        ExposureMax = TimeSpan.FromSeconds(indiExposure.Max);

        try
        {
            var ccdCfa = await _device.GetParameter("CCD_CFA", timeout: TimeSpan.FromMilliseconds(100));
            var ccdCfaItems = ccdCfa.GetItems<IndiText>();
            if (ccdCfaItems.TryGetValue("CFA_TYPE", out var indiCfa))
            {
                Enum.TryParse<BayerPattern>(indiCfa!.Value, true, out var bayerPattern);
                BayerPattern = bayerPattern;
            }
            else
            {
                Log.Warning("INDI property CCD_CFA:CFA_TYPE not found, reverting to RGBB");

                // This ends up assuming a bayer matrix of RGGB.
                BayerPattern = BayerPattern.None;
            }
        }
        catch (TimeoutException)
        {
            // This ends up assuming a bayer matrix of RGGB.
            BayerPattern = BayerPattern.None;
        }
    }

    private async Task SetValueFromMapping(string mapping, double value, CancellationToken token)
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(_device);

        try
        {
            var items = mapping.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (items.Length == 2)
            {
                var parameterName = items[0];
                var fieldName = items[1];
                var parameter = await _device.GetParameter(parameterName, TimeSpan.FromMilliseconds(10));
                var fields = parameter.GetItems<IndiNumber>();
                if (fields.TryGetValue(fieldName, out var field))
                {
                    var clampedValue = Math.Clamp(value, field.Min, field.Max);
                    await _device.Change(parameterName, [(fieldName, clampedValue)], token: token);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Log.Warning(e, "Could not set mapping {Mapping} to {Value}", mapping, value);
        }
    }
    
    private async Task SetGain(int gain, CancellationToken token)
    {
        // Setting gain is vendor specific. Use the mapping from the settings.
        var mapping = _profile.Current.Camera.GainMapping;
        await SetValueFromMapping(mapping, gain, token);
    }

    private async Task SetOffset(int offset, CancellationToken token)
    {
        // Setting offset is vendor specific. Use the mapping from the settings.
        var mapping = _profile.Current.Camera.OffsetMapping;
        await SetValueFromMapping(mapping, offset, token);
    }

    private async Task SetBinning(int binning, CancellationToken token)
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(_device);

        var parameter = await _device.GetParameter("CCD_BINNING", TimeSpan.FromMilliseconds(50));
        var fields = parameter.GetItems<IndiNumber>();
        var min = fields["HOR_BIN"].Min;
        var max = fields["HOR_BIN"].Max;
        var value = Math.Clamp(binning, min, max);
        await _device.Change("CCD_BINNING", [("HOR_BIN", value), ("VER_BIN", value)], token: token);
    }

    private async Task SetCustomProperties(CancellationToken token)
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(_device);

        var customPropertiesText = _profile.Current.Camera.CustomProperties;
        if (string.IsNullOrWhiteSpace(customPropertiesText))
            return;

        try
        {
            List<IndiCustomProperty> customProperties = JsonSerializer.Deserialize<List<IndiCustomProperty>>(customPropertiesText, IndiMappings.JsonOptions)
                ?? throw new ArgumentException(nameof(_profile.Current.Camera.CustomProperties));

            List<Task> tasks = [];
            foreach (var group in customProperties.GroupBy(x => x.Property))
            {
                var valuesToSend = group
                    .Select(x => (x.Field, x.Value))
                    .ToList();

                tasks.Add(_device.Change(group.Key, valuesToSend, timeout: TimeSpan.FromSeconds(1), token: token));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Could not set INDI custom properties, check if device properties are correct:\n{CustomProperties}", customPropertiesText);
        }
    }

    private void OnDisconnect()
    {
        ExposureMin = TimeSpan.Zero;
        ExposureMax = TimeSpan.Zero;
        BayerPattern = BayerPattern.None;
    }

    public string DeviceType => DeviceTypes.INDI;
    public string Name => _profile.Current.Camera.IndiDeviceName;
    public bool IsConnected => _isConnected && _client.IsConnected;
    public TimeSpan ExposureMin { get; private set; }
    public TimeSpan ExposureMax { get; private set; }
    public BayerPattern BayerPattern { get; private set; }
}