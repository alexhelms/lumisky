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

    [ObservableProperty] bool _keepRawImages;
    [ObservableProperty] ImageFileType _fileType;
    [ObservableProperty] int _jpegQuality;
    [ObservableProperty] int _pngCompression;
    [ObservableProperty] double _rotation;
    [ObservableProperty] bool _flipHorizontal;
    [ObservableProperty] bool _flipVertical;
    [ObservableProperty] bool _createPano;
    [ObservableProperty] double _panoDiameter;
    [ObservableProperty] double _panoXOffset;
    [ObservableProperty] double _panoYOffset;
    [ObservableProperty] double _panoRotation;
    [ObservableProperty] bool _panoFlipHorizontal;
}
