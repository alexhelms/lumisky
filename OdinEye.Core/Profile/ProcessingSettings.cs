using CommunityToolkit.Mvvm.ComponentModel;
using OdinEye.Core.Imaging.Processing;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace OdinEye.Core.Profile;

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
        TextOverlays = [];
    }

    [ObservableProperty] double _wbRedScale;
    [ObservableProperty] double _wbGreenScale;
    [ObservableProperty] double _wbBlueScale;
    [ObservableProperty] int _circleMaskDiameter;
    [ObservableProperty] int _circleMaskOffsetX;
    [ObservableProperty] int _circleMaskOffsetY;
    [ObservableProperty] int _circleMaskBlur;
    [ObservableProperty] bool _drawCardinalOverlay;
    [ObservableProperty] int _textSize;
    [ObservableProperty] int _textOutline;
    [ObservableProperty] string _textColor = "#c8c8c8";
    [ObservableProperty] string _textOutlineColor = "#000000";
    [ObservableProperty] string _cardinalTopString = "N";
    [ObservableProperty] string _cardinalBottomString = "S";
    [ObservableProperty] string _cardinalRightString = "E";
    [ObservableProperty] string _cardinalLeftString = "W";
    [ObservableProperty] string _panoramaCardinal0AzimuthString = "N";
    [ObservableProperty] string _panoramaCardinal90AzimuthString = "E";
    [ObservableProperty] string _panoramaCardinal180AzimuthString = "S";
    [ObservableProperty] string _panoramaCardinal270AzimuthString = "W";
    [ObservableProperty] bool _autoSCurve;
    [ObservableProperty] double _autoSCurveContrast;
    [ObservableProperty] bool _hotPixelCorrection;
    [ObservableProperty] int _hotPixelThresholdPercent;
    [ObservableProperty] ObservableCollection<TextOverlay> _textOverlays = [];
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