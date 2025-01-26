using CommunityToolkit.Mvvm.ComponentModel;
using LumiSky.Core.Imaging.Processing;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace LumiSky.Core.Profile;

public interface IProcessingSettings : ISettings
{
    double WbRedScale { get; set; }
    double WbGreenScale { get; set; }
    double WbBlueScale { get; set; }
    int CircleMaskDiameter { get; set; }
    int CircleMaskOffsetX { get; set; }
    int CircleMaskOffsetY { get; set; }
    int CircleMaskBlur { get; set; }
    bool DrawCardinalOverlay { get; set; }
    int TextSize { get; set; }
    int TextOutline { get; set; }
    string TextColor { get; set; }
    string TextOutlineColor { get; set; }
    string CardinalTopString { get; set; }
    string CardinalBottomString { get; set; }
    string CardinalRightString { get; set; }
    string CardinalLeftString { get; set; }
    string PanoramaCardinal0AzimuthString { get; set; }
    string PanoramaCardinal90AzimuthString { get; set; }
    string PanoramaCardinal180AzimuthString { get; set; }
    string PanoramaCardinal270AzimuthString { get; set; }
    bool AutoSCurve { get; set; }
    double AutoSCurveContrast { get; set; }
    bool HotPixelCorrection { get; set; }
    int HotPixelThresholdPercent { get; set; }
    bool EnableOverlays { get; set; }
    bool EnableVariableOverlays { get; set; }
    ObservableCollection<TextOverlay> TextOverlays { get; set; }
}

public sealed partial class ProcessingSettings : Settings, IProcessingSettings
{
    protected override void HookEvents()
    {
        ((INotifyCollectionChanged)TextOverlays).CollectionChanged += OnCollectionChanged;
    }

    protected override void UnhookEvents()
    {
        ((INotifyCollectionChanged)TextOverlays).CollectionChanged -= OnCollectionChanged;
    }

    protected override void Reset()
    {
        WbRedScale = 1.0;
        WbGreenScale = 1.0;
        WbBlueScale = 1.0;
        CircleMaskDiameter = 0;
        CircleMaskOffsetX = 0;
        CircleMaskOffsetY = 0;
        CircleMaskBlur = 0;
        DrawCardinalOverlay = true;
        TextSize = 20;
        TextOutline = 2;
        TextColor = "#c8c8c8";
        TextOutlineColor = "#000000";
        CardinalTopString = "N";
        CardinalBottomString = "S";
        CardinalRightString = "E";
        CardinalLeftString = "W";
        PanoramaCardinal0AzimuthString = "N";
        PanoramaCardinal90AzimuthString = "E";
        PanoramaCardinal180AzimuthString = "S";
        PanoramaCardinal270AzimuthString = "W";
        AutoSCurve = true;
        AutoSCurveContrast = 2.2;
        HotPixelCorrection = true;
        HotPixelThresholdPercent = 25;
        EnableOverlays = true;
        EnableVariableOverlays = true;
        TextOverlays = [];
    }

    [ObservableProperty]
    public partial double WbRedScale { get; set; }

    [ObservableProperty]
    public partial double WbGreenScale { get; set; }

    [ObservableProperty]
    public partial double WbBlueScale { get; set; }

    [ObservableProperty]
    public partial int CircleMaskDiameter { get; set; }

    [ObservableProperty]
    public partial int CircleMaskOffsetX { get; set; }

    [ObservableProperty]
    public partial int CircleMaskOffsetY { get; set; }

    [ObservableProperty]
    public partial int CircleMaskBlur { get; set; }

    [ObservableProperty]
    public partial bool DrawCardinalOverlay { get; set; }

    [ObservableProperty]
    public partial int TextSize { get; set; }

    [ObservableProperty]
    public partial int TextOutline { get; set; }

    [ObservableProperty]
    public partial string TextColor { get; set; } = "#c8c8c8";

    [ObservableProperty]
    public partial string TextOutlineColor { get; set; } = "#000000";

    [ObservableProperty]
    public partial string CardinalTopString { get; set; } = "N";

    [ObservableProperty]
    public partial string CardinalBottomString { get; set; } = "S";

    [ObservableProperty]
    public partial string CardinalRightString { get; set; } = "E";

    [ObservableProperty]
    public partial string CardinalLeftString { get; set; } = "W";

    [ObservableProperty]
    public partial string PanoramaCardinal0AzimuthString { get; set; } = "N";

    [ObservableProperty]
    public partial string PanoramaCardinal90AzimuthString { get; set; } = "E";

    [ObservableProperty]
    public partial string PanoramaCardinal180AzimuthString { get; set; } = "S";

    [ObservableProperty]
    public partial string PanoramaCardinal270AzimuthString { get; set; } = "W";

    [ObservableProperty]
    public partial bool AutoSCurve { get; set; }

    [ObservableProperty]
    public partial double AutoSCurveContrast { get; set; }

    [ObservableProperty]
    public partial bool HotPixelCorrection { get; set; }

    [ObservableProperty]
    public partial int HotPixelThresholdPercent { get; set; }

    [ObservableProperty]
    public partial bool EnableOverlays { get; set; }

    [ObservableProperty]
    public partial bool EnableVariableOverlays { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TextOverlay> TextOverlays { get; set; } = [];
}

public record TextOverlay
{
    public OverlayVariable Variable { get; set; }
    public string? Format { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int FontSize { get; set; } = 30;
    public string Color { get; set; } = "#ffffff";
    public TextAnchor Anchor { get; set; }
    public string StrokeColor { get; set; } = "#000000";
    public int StrokeWidth { get; set; } = 2;
}