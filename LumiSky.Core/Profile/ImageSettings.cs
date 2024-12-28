using CommunityToolkit.Mvvm.ComponentModel;
using LumiSky.Core.Imaging;

namespace LumiSky.Core.Profile;

public interface IImageSettings : ISettings
{
    bool KeepRawImages { get; set; }
    ImageFileType FileType { get; set; }
    int JpegQuality { get; set; }
    int PngCompression { get; set; }
    double Rotation { get; set; }
    bool FlipHorizontal { get; set; }
    bool FlipVertical { get; set; }
    bool CreatePano { get; set; }
    double PanoDiameter { get; set; }
    double PanoXOffset { get; set; }
    double PanoYOffset { get; set; }
    double PanoRotation { get; set; }
    bool PanoFlipHorizontal { get; set; }
}

public sealed partial class ImageSettings : Settings, IImageSettings
{
    protected override void Reset()
    {
        KeepRawImages = false;
        FileType = ImageFileType.JPEG;
        JpegQuality = 90;
        PngCompression = 5;
        Rotation = 0;
        FlipHorizontal = false;
        FlipVertical = false;
        CreatePano = true;
        PanoDiameter = 1000;
        PanoXOffset = 0;
        PanoYOffset = 0;
        PanoRotation = 0;
        PanoFlipHorizontal = false;
    }

    [ObservableProperty]
    public partial bool KeepRawImages { get; set; }

    [ObservableProperty]
    public partial ImageFileType FileType { get; set; }

    [ObservableProperty]
    public partial int JpegQuality { get; set; }

    [ObservableProperty]
    public partial int PngCompression { get; set; }

    [ObservableProperty]
    public partial double Rotation { get; set; }

    [ObservableProperty]
    public partial bool FlipHorizontal { get; set; }

    [ObservableProperty]
    public partial bool FlipVertical { get; set; }

    [ObservableProperty]
    public partial bool CreatePano { get; set; }

    [ObservableProperty]
    public partial double PanoDiameter { get; set; }

    [ObservableProperty]
    public partial double PanoXOffset { get; set; }

    [ObservableProperty]
    public partial double PanoYOffset { get; set; }

    [ObservableProperty]
    public partial double PanoRotation { get; set; }

    [ObservableProperty]
    public partial bool PanoFlipHorizontal { get; set; }
}
