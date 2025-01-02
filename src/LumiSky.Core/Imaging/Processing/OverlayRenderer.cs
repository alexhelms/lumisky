using CliWrap;
using Emgu.CV;
using LumiSky.Core.IO;
using LumiSky.Core.Memory;
using LumiSky.Core.Profile;
using System.Text;
using System.Text.Json;

namespace LumiSky.Core.Imaging.Processing;

public class OverlayRenderer
{
    private static string FontPath;
    private static string PythonOverlayRendererPath;

    private readonly IProfileProvider _profile;

    static OverlayRenderer()
    {
        var assembly = typeof(OverlayRenderer).Assembly;
        var directory = Path.GetDirectoryName(assembly.Location)!;
        FontPath = Path.Combine(directory, "Fonts", "RobotoMono-Regular.ttf");
        PythonOverlayRendererPath = Path.Combine(directory, "python", "overlay.py");
    }

    public OverlayRenderer(IProfileProvider profile)
    {
        _profile = profile;
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

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        
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

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });

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

    private record ConfigDto
    {
        public required string DataFilename { get; init; }
        public required int ImageWidth { get; init; }
        public required int ImageHeight { get; init; }
        public required string FontFilename { get; init; }
        public List<TextOverlayDto> TextOverlays { get; init; } = [];
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
}
