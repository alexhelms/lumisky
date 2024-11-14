using LumiSky.Core.IO.Fits;

namespace LumiSky.Core.Imaging;

public static class ImageMetadataExtensions
{
    public static ImageMetadata ToImageMetadata(this ImageHeader header)
    {
        var metadata = new ImageMetadata();

        foreach (var entry in header.Items)
        {
            Action<IHeaderEntry> parser = entry.Keyword switch
            {
                "INSTRUME" => ParseCameraName,
                "DATE" => ParseExposureUtc,
                "EXPOSURE" => ParseExposureDuration,
                "GAIN" => ParseGain,
                "OFFSET" => ParseOffset,
                "XBINNING" => ParseBinning,
                "XPIXSZ" => ParsePixelSize,
                "FOCALLEN" => ParseFocalLength,
                "BAYERPAT" => ParseBayerPattern,
                "SITENAME" => ParseLocation,
                "SITELAT" => ParseLatitude,
                "SITELON" => ParseLongitude,
                "SITEELV" => ParseElevation,
                "SUNALT" => ParseSunAltitude,
                _ => _ => { },
            };

            try
            {
                parser(entry);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Error parsing FITS header keyword {Keyword}", entry.Keyword);
            }
        }

        return metadata;

        void ParseCameraName(IHeaderEntry entry)
        {
            if (entry is StringHeaderEntry stringEntry)
                metadata.CameraName = stringEntry.Value;
        }

        void ParseExposureUtc(IHeaderEntry entry)
        {
            if (entry is StringHeaderEntry stringEntry &&
                DateTime.TryParse(stringEntry.Value, out var timestamp))
                metadata.ExposureUtc = timestamp;
        }

        void ParseExposureDuration(IHeaderEntry entry)
        {
            if (entry is FloatHeaderEntry floatEntry)
                metadata.ExposureDuration = TimeSpan.FromSeconds(floatEntry.Value);
        }

        void ParseGain(IHeaderEntry entry)
        {
            if (entry is IntegerHeaderEntry intEntry)
                metadata.Gain = intEntry.Value;
        }

        void ParseOffset(IHeaderEntry entry)
        {
            if (entry is IntegerHeaderEntry intEntry)
                metadata.Offset = intEntry.Value;
        }

        void ParseBinning(IHeaderEntry entry)
        {
            if (entry is IntegerHeaderEntry intEntry)
                metadata.Binning = intEntry.Value;
        }

        void ParsePixelSize(IHeaderEntry entry)
        {
            if (entry is FloatHeaderEntry floatEntry)
                metadata.PixelSize = floatEntry.Value;
        }

        void ParseFocalLength(IHeaderEntry entry)
        {
            if (entry is FloatHeaderEntry floatEntry)
                metadata.FocalLength = floatEntry.Value;
        }

        void ParseBayerPattern(IHeaderEntry entry)
        {
            if (entry is StringHeaderEntry stringEntry &&
                Enum.TryParse<BayerPattern>(stringEntry.Value, out var bayerPattern))
                metadata.BayerPattern = bayerPattern;
        }

        void ParseLocation(IHeaderEntry entry)
        {
            if (entry is StringHeaderEntry stringEntry)
                metadata.Location = stringEntry.Value;
        }

        void ParseLatitude(IHeaderEntry entry)
        {
            if (entry is FloatHeaderEntry floatEntry)
                metadata.Latitude = floatEntry.Value;
        }

        void ParseLongitude(IHeaderEntry entry)
        {
            if (entry is FloatHeaderEntry floatEntry)
                metadata.Longitude = floatEntry.Value;
        }

        void ParseElevation(IHeaderEntry entry)
        {
            if (entry is FloatHeaderEntry floatEntry)
                metadata.Elevation = floatEntry.Value;
        }

        void ParseSunAltitude(IHeaderEntry entry)
        {
            if (entry is FloatHeaderEntry floatEntry)
                metadata.SunAltitude = floatEntry.Value;
        }
    }

    public static IEnumerable<IHeaderEntry> ToFitsHeaderEntries(this ImageMetadata metadata)
    {
        foreach (var item in CreateHeaderEntries(metadata))
        {
            if (item is not null)
                yield return item;
        }
    }

    private static IEnumerable<IHeaderEntry?> CreateHeaderEntries(ImageMetadata metadata)
    {
        string? dateUtc = metadata.ExposureUtc?.ToString("O");
        string? dateLocal = metadata.ExposureUtc?.ToLocalTime().ToString("O");
        double? exposureSeconds = metadata.ExposureDuration.HasValue
            ? Math.Round(metadata.ExposureDuration.Value.TotalSeconds, 6)
            : null;
        double? pixelSizeWithBinning = metadata.PixelSize.HasValue && metadata.Binning.HasValue
            ? Math.Round(metadata.PixelSize.Value * metadata.Binning.Value, 6)
            : null;
        double? latitude = metadata.Latitude.HasValue
            ? Math.Round(metadata.Latitude.Value, 6)
            : null;
        double? longitude = metadata.Longitude.HasValue
            ? Math.Round(metadata.Longitude.Value, 6)
            : null;
        double? elevation = metadata.Elevation.HasValue
            ? Math.Round(metadata.Elevation.Value, 1)
            : null;
        double? sunAltitude = metadata.SunAltitude.HasValue
            ? Math.Round(metadata.SunAltitude.Value, 2)
            : null;

        yield return CreateEntry("DATE", dateUtc, "Observation date, UTC");
        yield return CreateEntry("DATE-OBS", dateUtc, "Observation date, UTC");
        yield return CreateEntry("DATE-LOC", dateLocal, "Observation date, Local");
        yield return CreateEntry("CREATOR", $"LumiSky {Utilities.RuntimeUtil.Version}");
        yield return CreateEntry("INSTRUME", metadata.CameraName);
        yield return CreateEntry("EXPOSURE", exposureSeconds, "[s] Exposure time");
        yield return CreateEntry("EXPTIME", exposureSeconds, "[s] Exposure time");
        yield return CreateEntry("GAIN", metadata.Gain, "Sensor software gain");
        yield return CreateEntry("OFFSET", metadata.Offset, "Sensor software offset");
        yield return CreateEntry("IMAGETYP", "LIGHT");
        yield return CreateEntry("XBINNING", metadata.Binning);
        yield return CreateEntry("YBINNING", metadata.Binning);
        yield return CreateEntry("XPIXSZ", pixelSizeWithBinning, "[um] Pixel size");
        yield return CreateEntry("YPIXSZ", pixelSizeWithBinning, "[um] Pixel size");
        yield return CreateEntry("SITENAME", metadata.Location, "Location name");
        yield return CreateEntry("SITELAT", latitude, "[deg] Latitude");
        yield return CreateEntry("SITELON", longitude, "[deg] Longitude");
        yield return CreateEntry("SITEELV", elevation, "[m] Elevation");
        yield return CreateEntry("FOCALLEN", metadata.FocalLength, "[mm] Focal length");
        yield return CreateEntry("BAYERPAT", metadata.BayerPattern?.ToString(), "Bayer pattern");
        yield return CreateEntry("SUNALT", sunAltitude, "[deg] Sun altitude");
    }

    private static IHeaderEntry? CreateEntry<T>(string keyword, T? value, string? comment = null)
    {
        comment ??= string.Empty;

        try
        {
            if (value is not null)
            {
                var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (type == typeof(string))
                {
                    var str = value as string ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(str))
                        return new StringHeaderEntry(keyword, value as string ?? string.Empty, comment);
                }
                else if (type == typeof(double) ||
                    type == typeof(float))
                {
                    return new FloatHeaderEntry(keyword, Convert.ToDouble(value), comment);
                }
                else if (type == typeof(int))
                {
                    return new IntegerHeaderEntry(keyword, Convert.ToInt32(value), comment);
                }
                else if (type == typeof(bool))
                {
                    return new BooleanHeaderEntry(keyword, Convert.ToBoolean(value), comment);
                }
                else
                {
                    Log.Warning("Unable to convert metadata {Keyword} = {Value} to FITS header entry", keyword, value);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error converting metadata {Keyword} = {Value} to FITS header entry", keyword, value);
        }

        return null;
    }
}
