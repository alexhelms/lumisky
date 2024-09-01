using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using OdinEye.Core.Imaging;
using OdinEye.Core.Imaging.Processing;
using OdinEye.Core.Memory;
using OdinEye.Core.Primitives;
using OdinEye.Core.Profile;
using OdinEye.Core.Utilities;

namespace OdinEye.Core.Services;

public record FitsProcessingResults : IDisposable
{
    public required DateTime ExposureUtc { get; set; }
    public required TimeSpan ExposureDuration { get; set; }
    public required double Median { get; set; }
    public required int Gain { get; set; }
    public required Mat Image { get; set; }

    public required Mat? Panorama { get; set; }

    ~FitsProcessingResults()
    {
        Image?.Dispose();
        Panorama?.Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Image?.Dispose();
        Panorama?.Dispose();
    }
}

public class ImageService
{
    private readonly IProfileProvider _profile;

    public event EventHandler? NewImage;
    public event EventHandler? NewPanorama;

    public string? LatestImagePath { get; private set; }
    public string? LatestPanoramaPath { get; private set; }
    public Size? LatestImageSize { get; private set; }
    public Size? LatestPanoramaSize { get; private set; }

    public ImageService(IProfileProvider profile)
    {
        _profile = profile;
    }

    public void SetLatestImage(string path, Size size)
    {
        LatestImagePath = path;
        LatestImageSize = size;
        NewImage?.Invoke(this, EventArgs.Empty);
    }

    public void SetLatestPanorama(string path, Size size)
    {
        LatestPanoramaPath = path;
        LatestPanoramaSize = size;
        NewPanorama?.Invoke(this, EventArgs.Empty);
    }

    public FitsProcessingResults ProcessFits(string filename)
    {
        var fileInfo = new FileInfo(filename);
        if (!fileInfo.Exists)
            throw new FileNotFoundException(filename);

        // TODO: CFA-aware hot pixel correction

        using var debayeredImage = DebayerImage(filename);

        // Green median is used for exposure prediction
        var greenMedian = debayeredImage.Median(channel: 1);

        // Linear Operations
        WhiteBalance(debayeredImage);

        Stretch(debayeredImage);

        // Nonlinear Operations
        var image = debayeredImage.To8bitMat();
        Rotate(image);
        FlipHorizontal(image);
        FlipVertical(image);
        CircleMask(image);

        Mat? panorama = null;
        if (_profile.Current.Image.CreatePano)
        {
            panorama = CreatePanorama(image);
        }

        return new FitsProcessingResults
        {
            ExposureUtc = debayeredImage.Metadata.ExposureUtc ?? DateTime.Now,
            ExposureDuration = debayeredImage.Metadata.ExposureDuration ?? TimeSpan.Zero,
            Median = greenMedian,
            Gain = debayeredImage.Metadata.Gain ?? 0,
            Image = image,
            Panorama = panorama,
        };
    }

    public Mat CreatePanorama(Mat mat)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(mat.NumberOfChannels, 3);
        if (mat.Depth != DepthType.Cv8U)
            throw new ArgumentOutOfRangeException(nameof(mat.Depth));

        var xOffset = _profile.Current.Image.PanoXOffset;
        var yOffset = _profile.Current.Image.PanoYOffset;
        var radius = _profile.Current.Image.PanoDiameter / 2;
        var angle = _profile.Current.Image.PanoRotation;

        Mat panorama = Transform.Panorama(mat, xOffset, yOffset, radius, angle);

        if (_profile.Current.Image.PanoFlipHorizontal)
        {
            Transform.FlipHorizontal(panorama);
        }

        return panorama;
    }

    private AllSkyImage DebayerImage(string filename)
    {
        return Debayer.FromFits(filename);
    }

    private void Stretch(AllSkyImage image)
    {
        image.StretchLinked();
    }

    private void WhiteBalance(AllSkyImage image)
    {
        double rScale = Math.Clamp(_profile.Current.Processing.WbRedScale, 0, 1);
        double gScale = Math.Clamp(_profile.Current.Processing.WbGreenScale, 0, 1);
        double bScale = Math.Clamp(_profile.Current.Processing.WbBlueScale, 0, 1);
        double rBias = Math.Clamp(_profile.Current.Processing.WbRedBias, 0, 1);
        double gBias = Math.Clamp(_profile.Current.Processing.WbGreenBias, 0, 1);
        double bBias = Math.Clamp(_profile.Current.Processing.WbBlueBias, 0, 1);

        if (rScale == 1 && gScale == 1 && bScale == 1 && rBias == 0 && gBias == 0 && bBias == 0) return;

        image.WhiteBalance(rScale, gScale, bScale, rBias, gBias, bBias);
    }

    private void Rotate(Mat mat)
    {
        if (_profile.Current.Image.Rotation != 0)
        {
            Transform.Rotate(mat, _profile.Current.Image.Rotation);
        }
    }

    private void FlipHorizontal(Mat mat)
    {
        if (_profile.Current.Image.FlipHorizontal)
        {
            Transform.FlipHorizontal(mat);
        }
    }

    private void FlipVertical(Mat mat)
    {
        if (_profile.Current.Image.FlipVertical)
        {
            Transform.FlipVertical(mat);
        }
    }

    private void CircleMask(Mat mat)
    {
        if (_profile.Current.Processing.CircleMaskDiameter > 0)
        {
            int centerX = mat.Cols / 2;
            int centerY = mat.Rows / 2;
            Mask.Circle(mat,
                x: centerX + _profile.Current.Processing.CircleMaskOffsetX,
                y: centerY + _profile.Current.Processing.CircleMaskOffsetY,
                diameter: _profile.Current.Processing.CircleMaskDiameter,
                blur: _profile.Current.Processing.CircleMaskBlur);
        }
    }
}
