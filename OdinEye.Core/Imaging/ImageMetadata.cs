using System.Linq.Expressions;

namespace OdinEye.Core.Imaging;

public class ImageMetadata
{
    private readonly Dictionary<string, object?> _metadata = new();

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    private class MetadataAttribute : Attribute
    {
    }

    public ImageMetadata() { }

    public ImageMetadata(Dictionary<string, object?> metadata)
    {
        _metadata = metadata;
    }

    public ImageMetadata Clone() => new ImageMetadata(new(_metadata));

    private void SetMetadata<TMetadata>(Expression<Func<ImageMetadata, TMetadata?>> expression, TMetadata? value)
    {
        var key = expression.GetPropertyName();
        _metadata[key] = value;
    }

    private TMetadata? GetMetadata<TMetadata>(Expression<Func<ImageMetadata, TMetadata?>> expression, TMetadata? defaultValue = default)
    {
        var key = expression.GetPropertyName();
        if (_metadata.TryGetValue(key, out object? value))
            return (TMetadata?)value;
        return defaultValue;
    }

    public void Merge(ImageMetadata other)
    {
        var props = other.GetType()
            .GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(MetadataAttribute)));

        foreach (var prop in props)
        {
            if (!prop.CanWrite)
                continue;

            var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var value = prop.GetValue(other);
            if (value is not null)
                value = Convert.ChangeType(value, t);

            if (value is not null)
                prop.SetValue(this, value);
        }
    }

    #region Camera

    public string? CameraName
    {
        get => GetMetadata(x => x.CameraName);
        set => SetMetadata(x => x.CameraName, value);
    }

    [Metadata]
    public DateTime? ExposureUtc
    {
        get => GetMetadata(x => x.ExposureUtc);
        set => SetMetadata(x => x.ExposureUtc, value);
    }

    [Metadata]
    public TimeSpan? ExposureDuration
    {
        get => GetMetadata(x => x.ExposureDuration);
        set => SetMetadata(x => x.ExposureDuration, value);
    }

    [Metadata]
    public int? Gain
    {
        get => GetMetadata(m => m.Gain);
        set => SetMetadata(m => m.Gain, value);
    }

    [Metadata]
    public int? Offset
    {
        get => GetMetadata(m => m.Offset);
        set => SetMetadata(m => m.Offset, value);
    }

    [Metadata]
    public int? Binning
    {
        get => GetMetadata(m => m.Binning);
        set => SetMetadata(m => m.Binning, value);
    }

    [Metadata]
    public double? PixelSize
    {
        get => GetMetadata(m => m.PixelSize);
        set => SetMetadata(m => m.PixelSize, value);
    }

    [Metadata]
    public double? FocalLength
    {
        get => GetMetadata(m => m.FocalLength);
        set => SetMetadata(m => m.FocalLength, value);
    }

    [Metadata]
    public BayerPattern? BayerPattern
    {
        get => GetMetadata(m => m.BayerPattern);
        set => SetMetadata(m => m.BayerPattern, value);
    }

    #endregion

    #region Location

    [Metadata]
    public string? Location
    {
        get => GetMetadata(x => x.Location);
        set => SetMetadata(x => x.Location, value);
    }

    [Metadata]
    public double? Latitude
    {
        get => GetMetadata(x => x.Latitude);
        set => SetMetadata(x => x.Latitude, value);
    }

    [Metadata]
    public double? Longitude
    {
        get => GetMetadata(x => x.Longitude);
        set => SetMetadata(x => x.Longitude, value);
    }

    [Metadata]
    public double? Elevation
    {
        get => GetMetadata(x => x.Elevation);
        set => SetMetadata(x => x.Elevation, value);
    }

    #endregion
}
