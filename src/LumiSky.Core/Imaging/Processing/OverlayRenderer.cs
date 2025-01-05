using CliWrap;
using Emgu.CV;
using LumiSky.Core.IO;
using LumiSky.Core.Mathematics;
using LumiSky.Core.Memory;
using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace LumiSky.Core.Imaging.Processing;

public class OverlayRenderer
{
    private static string FontPath;
    private static string PythonOverlayRendererPath;

    private readonly IProfileProvider _profile;
    private readonly IMountPositionProvider _mountPositionProvider;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    static OverlayRenderer()
    {
        var assembly = typeof(OverlayRenderer).Assembly;
        var directory = Path.GetDirectoryName(assembly.Location)!;
        FontPath = Path.Combine(directory, "Fonts", "RobotoMono-Regular.ttf");
        PythonOverlayRendererPath = Path.Combine(directory, "python", "overlay.py");
    }

    public OverlayRenderer(
        IProfileProvider profile,
        IMountPositionProvider mountPositionProvider)
    {
        _profile = profile;
        _mountPositionProvider = mountPositionProvider;
    }

    private string TextAnchorToPillow(TextAnchor anchor) => anchor switch
    {
        // https://pillow.readthedocs.io/en/stable/handbook/text-anchors.html
        TextAnchor.TopLeft => "lt",
        TextAnchor.TopMiddle => "mt",
        TextAnchor.TopRight => "rt",
        TextAnchor.MiddleLeft => "lm",
        TextAnchor.Middle => "mm",
        TextAnchor.MiddleRight => "rm",
        TextAnchor.BaselineLeft => "ls",
        TextAnchor.BaselineMiddle => "ms",
        TextAnchor.BaselineRight => "rs",
        _ => "mm",
    };

    private string FormatOverlayText(OverlayVariable variable, string format, ImageMetadata metadata)
    {
        object? value = null;

        try
        {
            value = variable switch
            {
                OverlayVariable.Timestamp => metadata.ExposureUtc.GetValueOrDefault().ToLocalTime(),
                OverlayVariable.Latitude => metadata.Latitude.GetValueOrDefault(),
                OverlayVariable.Longitude => metadata.Longitude.GetValueOrDefault(),
                OverlayVariable.Elevation => metadata.Elevation.GetValueOrDefault(),
                OverlayVariable.Exposure => metadata.ExposureDuration.GetValueOrDefault().TotalSeconds,
                OverlayVariable.Gain => metadata.Gain.GetValueOrDefault(),
                OverlayVariable.SunAltitude => metadata.SunAltitude.GetValueOrDefault(),
                OverlayVariable.Text => format,
                _ => null
            };

            if (variable == OverlayVariable.Text)
                return value?.ToString() ?? string.Empty;

            if (value is null)
                return string.Empty;

            return string.Format(format, value);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error formatting {Variable} {Value} with format {Format}", variable, value, format);
            return string.Empty;
        }
    }

    public async Task DrawImageOverlays(Mat mat, ImageMetadata metadata)
    {
        using var rawData = new TemporaryFile();
        int width = mat.Cols;
        int height = mat.Rows;

        var dto = new ConfigDto
        {
            DataFilename = rawData.Path,
            FontFilename = FontPath,
            ImageWidth = width,
            ImageHeight = height,
            TextOverlays = [],
            CrosshairOverlays = [],
        };

        // Cardinal Overlays
        if (_profile.Current.Processing.DrawCardinalOverlay)
        {
            string textFill = _profile.Current.Processing.TextColor;
            string strokeFill =_profile.Current.Processing.TextOutlineColor;
            int strokeWidth = _profile.Current.Processing.TextOutline;
            int fontSize = _profile.Current.Processing.TextSize;
            int margin = fontSize / 4;
            int top = margin;
            int bottom = height - margin;
            int right = width - margin;
            int left = margin;

            // Top
            dto.TextOverlays.Add(new TextOverlayDto
            {
                X = width / 2,
                Y = top,
                FontSize = fontSize,
                StrokeFill = strokeFill,
                StrokeWidth = strokeWidth,
                Text = _profile.Current.Processing.CardinalTopString,
                TextAnchor = TextAnchorToPillow(TextAnchor.TopMiddle),
                TextFill = textFill,
            });

            // Bottom
            dto.TextOverlays.Add(new TextOverlayDto
            {
                X = width / 2,
                Y = bottom,
                FontSize = fontSize,
                StrokeFill = strokeFill,
                StrokeWidth = strokeWidth,
                Text = _profile.Current.Processing.CardinalBottomString,
                TextAnchor = TextAnchorToPillow(TextAnchor.BaselineMiddle),
                TextFill = textFill,
            });

            // Right
            dto.TextOverlays.Add(new TextOverlayDto
            {
                X = right,
                Y = height / 2,
                FontSize = fontSize,
                StrokeFill = strokeFill,
                StrokeWidth = strokeWidth,
                Text = _profile.Current.Processing.CardinalRightString,
                TextAnchor = TextAnchorToPillow(TextAnchor.MiddleRight),
                TextFill = textFill,
            });

            // Left
            dto.TextOverlays.Add(new TextOverlayDto
            {
                X = left,
                Y = height / 2,
                FontSize = fontSize,
                StrokeFill = strokeFill,
                StrokeWidth = strokeWidth,
                Text = _profile.Current.Processing.CardinalLeftString,
                TextAnchor = TextAnchorToPillow(TextAnchor.MiddleLeft),
                TextFill = textFill,
            });
        }

        // User-defined Text Overlays
        if (_profile.Current.Processing.EnableTextOverlays)
        {
            foreach (var overlay in _profile.Current.Processing.TextOverlays)
            {
                dto.TextOverlays.Add(new TextOverlayDto
                {
                    X = overlay.X,
                    Y = overlay.Y,
                    FontSize = overlay.FontSize,
                    StrokeFill = overlay.StrokeColor,
                    StrokeWidth = overlay.StrokeWidth,
                    Text = FormatOverlayText(overlay.Variable, overlay.Format ?? "{0}", metadata),
                    TextAnchor = TextAnchorToPillow(overlay.Anchor),
                    TextFill = overlay.Color,
                });
            }
        }

        // User-defined Pointing Overlays
        if (_profile.Current.Processing.EnablePointingOverlays)
        {
            var pointingOverlays = _profile.Current.Processing.PointingOverlays
                .DistinctBy(x => x.Hostname)
                .ToDictionary(x => x.Hostname, StringComparer.OrdinalIgnoreCase);
            var mountPositions = await _mountPositionProvider.GetMountPositions();
            foreach (var position in mountPositions)
            {
                // Skip telescopes pointing too low.
                if (position.Altitude < _profile.Current.Processing.PointingOverlayAltitudeThreshold)
                {
                    continue;
                }

                // Skip hostnames that are not configured.
                if (!pointingOverlays.TryGetValue(position.Name, out var pointingOverlay))
                {
                    continue;
                }

                (int x, int y) = TransformAltAzToImage(
                    position.Altitude,
                    position.Azimuth,
                    width,
                    height,
                    _profile.Current.Processing.PointingOverlayRadius,
                    _profile.Current.Processing.PointingOverlayXOffset,
                    _profile.Current.Processing.PointingOverlayYOffset,
                    _profile.Current.Processing.PointingOverlayRotation,
                    _profile.Current.Processing.PointingOverlayFlipVertical
                );

                dto.CrosshairOverlays.Add(new CrosshairOverlayDto
                {
                    X = x,
                    Y = y,
                    Size = pointingOverlay.Size,
                    Width = pointingOverlay.LineWidth,
                    Text = pointingOverlay.DisplayName,
                    FontSize = pointingOverlay.FontSize,
                    StrokeFill = pointingOverlay.StrokeColor,
                    StrokeWidth = pointingOverlay.StrokeWidth,
                    Color = pointingOverlay.Color,
                });
            }
        }

        var json = JsonSerializer.Serialize(dto, _serializerOptions);
        
        mat.ToBlob(rawData.Path);

        var stdout = new StringBuilder(4096);
        var stderr = new StringBuilder(4096);
        var result = await Cli.Wrap(Python.PythonExecutablePath)
            .WithArguments(c => c
                .Add($"\"{PythonOverlayRendererPath}\"", escape: false))
            .WithStandardInputPipe(PipeSource.FromString(json))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (stdout.Length > 0)
            Log.Debug(stdout.ToString());

        if (stderr.Length > 0)
            Log.Error(stderr.ToString());

        if (result.IsSuccess)
        {
            mat.FromBlob(rawData.Path);
        }
    }

    public async Task DrawPanoramaOverlays(Mat mat)
    {
        using var rawData = new TemporaryFile();
        int width = mat.Cols;
        int height = mat.Rows;

        var dto = new ConfigDto
        {
            DataFilename = rawData.Path,
            FontFilename = FontPath,
            ImageWidth = width,
            ImageHeight = height,
            TextOverlays = [],
        };

        string textFill = _profile.Current.Processing.TextColor;
        string strokeFill =_profile.Current.Processing.TextOutlineColor;
        int strokeWidth = _profile.Current.Processing.TextOutline;
        int fontSize = _profile.Current.Processing.TextSize;
        int margin = fontSize / 2;
        int section = width / 4;

        // 0 Azimuth
        dto.TextOverlays.Add(new TextOverlayDto
        {
            X = margin,
            Y = height - margin,
            FontSize = fontSize,
            StrokeFill = strokeFill,
            StrokeWidth = strokeWidth,
            Text = _profile.Current.Processing.PanoramaCardinal0AzimuthString,
            TextAnchor = TextAnchorToPillow(TextAnchor.BaselineMiddle),
            TextFill = textFill,
        });

        // 90 Azimuth
        dto.TextOverlays.Add(new TextOverlayDto
        {
            X = margin + section,
            Y = height - margin,
            FontSize = fontSize,
            StrokeFill = strokeFill,
            StrokeWidth = strokeWidth,
            Text = _profile.Current.Processing.PanoramaCardinal90AzimuthString,
            TextAnchor = TextAnchorToPillow(TextAnchor.BaselineMiddle),
            TextFill = textFill,
        });

        // 180 Azimuth
        dto.TextOverlays.Add(new TextOverlayDto
        {
            X = margin + section * 2,
            Y = height - margin,
            FontSize = fontSize,
            StrokeFill = strokeFill,
            StrokeWidth = strokeWidth,
            Text = _profile.Current.Processing.PanoramaCardinal180AzimuthString,
            TextAnchor = TextAnchorToPillow(TextAnchor.BaselineMiddle),
            TextFill = textFill,
        });

        // 270 Azimuth
        dto.TextOverlays.Add(new TextOverlayDto
        {
            X = margin + section * 3,
            Y = height - margin,
            FontSize = fontSize,
            StrokeFill = strokeFill,
            StrokeWidth = strokeWidth,
            Text = _profile.Current.Processing.PanoramaCardinal270AzimuthString,
            TextAnchor = TextAnchorToPillow(TextAnchor.BaselineMiddle),
            TextFill = textFill,
        });

        var json = JsonSerializer.Serialize(dto, _serializerOptions);

        mat.ToBlob(rawData.Path);

        var stdout = new StringBuilder(512);
        var stderr = new StringBuilder(512);
        var result = await Cli.Wrap(Python.PythonExecutablePath)
            .WithArguments(c => c
                .Add($"\"{PythonOverlayRendererPath}\"", escape: false))
            .WithStandardInputPipe(PipeSource.FromString(json))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (stdout.Length > 0)
            Log.Debug(stdout.ToString());

        if (stderr.Length > 0)
            Log.Error(stderr.ToString());

        if (result.IsSuccess)
        {
            mat.FromBlob(rawData.Path);
        }
    }

    private static (int, int) TransformAltAzToImage(
        double altitude,
        double azimuth,
        int width,
        int height,
        int radius,
        int offsetX,
        int offsetY,
        double rotation,
        bool flipVertical)
    {
        // All parameters are in degrees.
        // Rotation parameter assumes CCW is positive.

        radius = radius > 0 ? radius : (int)(Math.Max(width, height) / 2.0);
        double altitudeRad = altitude * LumiSkyMath.Deg2Rad;
        double azimuthRad = azimuth * LumiSkyMath.Deg2Rad;
        double x = radius * Math.Cos(altitudeRad) * Math.Sin(azimuthRad);
        double y = radius * Math.Cos(altitudeRad) * Math.Cos(azimuthRad);
        
        // Rotate the points about the center.
        var mat = Matrix3x2.CreateRotation((float)(-1 * rotation * LumiSkyMath.Deg2Rad));
        var vec = Vector2.Transform(new Vector2((float)x, (float)y), mat);

        // Assumption: Center +/- x/y offset is zenith.
        double cx = offsetX + width / 2.0;
        double cy = offsetY + height / 2.0;

        int xOut = (int)(cx + vec.X);
        int yOut = (int)(cy + vec.Y);

        if (flipVertical)
        {
            yOut = -1 * yOut;
        }

        return (xOut, yOut);
    }

    private record ConfigDto
    {
        public required string DataFilename { get; init; }
        public required int ImageWidth { get; init; }
        public required int ImageHeight { get; init; }
        public required string FontFilename { get; init; }
        public List<TextOverlayDto> TextOverlays { get; init; } = [];
        public List<CrosshairOverlayDto> CrosshairOverlays { get; init; } = [];
    }

    private record TextOverlayDto
    {
        public required int X { get; init; }
        public required int Y { get; init; }
        public required string Text { get; init; }
        public required int FontSize { get; init; }
        public required string TextFill { get; init; }
        public required string TextAnchor { get; init; }
        public required string StrokeFill { get; init; }
        public required int StrokeWidth { get; init; }
    }

    private record CrosshairOverlayDto
    {
        public required int X { get; init; }
        public required int Y { get; init; }
        public required int Size { get; init; }
        public required int Width { get; init; }
        public required string Text { get; init; }
        public required int FontSize { get; init; }
        public required string StrokeFill { get; init; }
        public required int StrokeWidth { get; init; }
        public required string Color { get; init; }
    }
}
