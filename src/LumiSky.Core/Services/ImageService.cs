using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LumiSky.Core.Data;
using LumiSky.Core.Imaging;
using LumiSky.Core.Imaging.Processing;
using LumiSky.Core.Profile;
using LumiSky.Core.Utilities;

namespace LumiSky.Core.Services;

public record FitsProcessingResults : IDisposable
{
    public required ImageMetadata Metadata { get; set; }
    public required double Median { get; set; }
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

public record FitsProcessTimingItem(string Name, TimeSpan Elapsed);

public static class FitsProcessTimingTracker
{
    public static List<FitsProcessTimingItem> Items { get; } = [];

    public static event EventHandler? Complete;

    public static void FireComplete()
    {
        Complete?.Invoke(null, EventArgs.Empty);
    }
}

public class ImageService
{
    private readonly IProfileProvider _profile;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SunService _sunService;

    public event EventHandler? NewImage;
    public event EventHandler? NewPanorama;
    public event EventHandler? NewFocus;

    public string? LatestImagePath { get; private set; }
    public string? LatestPanoramaPath { get; private set; }
    public string? LatestFocusPath { get; private set; }

    public ImageService(
        IProfileProvider profile,
        IServiceScopeFactory serviceScopeFactory,
        SunService sunService)
    {
        _profile = profile;
        _serviceScopeFactory = serviceScopeFactory;
        _sunService = sunService;
    }

    public void SetLatestImage(string path)
    {
        LatestImagePath = path;
        NewImage?.Invoke(this, EventArgs.Empty);
    }

    public void SetLatestPanorama(string path)
    {
        LatestPanoramaPath = path;
        NewPanorama?.Invoke(this, EventArgs.Empty);
    }

    public void SetLatestFocus(string path)
    {
        LatestFocusPath = path;
        NewFocus?.Invoke(this, EventArgs.Empty);
    }

    public async Task FavoriteRawImage(int id, bool value)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.RawImages
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsFavorite, value));
    }

    public async Task FavoriteImage(int id, bool value)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Images
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsFavorite, value));
    }

    public async Task FavoritePanorama(int id, bool value)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Panoramas
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsFavorite, value));
    }

    public async Task FavoriteTimelapse(int id, bool value)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Timelapses
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsFavorite, value));
    }

    public async Task FavoritePanoramaTimelapse(int id, bool value)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.PanoramaTimelapses
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsFavorite, value));
    }

    public async Task DeleteRawImage(int id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rawImage = await dbContext.RawImages.FirstOrDefaultAsync(x => x.Id == id);
        if (rawImage is not null)
        {
            dbContext.RawImages.Remove(rawImage);
            if (await dbContext.SaveChangesAsync() > 0)
            {
                TryDeleteFile(rawImage.Filename);
            }
        }
    }

    public async Task DeleteImage(int id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var image = await dbContext.Images.FirstOrDefaultAsync(x => x.Id == id);
        if (image is not null)
        {
            dbContext.Images.Remove(image);
            if (await dbContext.SaveChangesAsync() > 0)
            {
                TryDeleteFile(image.Filename);
            }
        }
    }

    public async Task DeletePanorama(int id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var panorama = await dbContext.Panoramas.FirstOrDefaultAsync(x => x.Id == id);
        if (panorama is not null)
        {
            dbContext.Panoramas.Remove(panorama);
            if (await dbContext.SaveChangesAsync() > 0)
            {
                TryDeleteFile(panorama.Filename);
            }
        }
    }

    public async Task DeleteTimelapse(int id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timelapse = await dbContext.Timelapses.FirstOrDefaultAsync(x => x.Id == id);
        if (timelapse is not null)
        {
            dbContext.Timelapses.Remove(timelapse);
            if (await dbContext.SaveChangesAsync() > 0)
            {
                TryDeleteFile(timelapse.Filename);
            }
        }
    }

    public async Task DeletePanoramaTimelapse(int id)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var panoramaTimelapse = await dbContext.PanoramaTimelapses.FirstOrDefaultAsync(x => x.Id == id);
        if (panoramaTimelapse is not null)
        {
            dbContext.PanoramaTimelapses.Remove(panoramaTimelapse);
            if (await dbContext.SaveChangesAsync() > 0)
            {
                TryDeleteFile(panoramaTimelapse.Filename);
            }
        }
    }

    private void TryDeleteFile(string filename)
    {
        try
        {
            File.Delete(filename);
        }
        catch (FileNotFoundException)
        {
            Log.Warning("Tried to delete {Filename} but it was not found", filename);
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception deleting file");
        }
    }

    public FitsProcessingResults ProcessFits(string filename)
    {
        var fileInfo = new FileInfo(filename);
        if (!fileInfo.Exists)
            throw new FileNotFoundException(filename);

        FitsProcessTimingTracker.Items.Clear();

        using var rawImage = LoadRawImage(filename);
        RemoveHotPixels(rawImage);

        using var debayeredImage = DebayerImage(rawImage);

        var median = Median(debayeredImage);

        // Linear Operations
        WhiteBalance(debayeredImage);

        Stretch(debayeredImage);

        // Nonlinear Operations
        AutoSCurve(debayeredImage);

        var image = To8BitMat(debayeredImage);
        Rotate(image);
        FlipHorizontal(image);
        FlipVertical(image);
        CircleMask(image);

        Mat? panorama = null;
        if (_profile.Current.Image.CreatePano)
        {
            panorama = CreatePanorama(image);
        }

        FitsProcessTimingTracker.FireComplete();

        return new FitsProcessingResults
        {
            Metadata = rawImage.Metadata,
            Median = median,
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

    private AllSkyImage LoadRawImage(string filename)
    {
        using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Load Raw Image", t)));
        var rawImage = AllSkyImage.FromFits(filename);
        return rawImage;
    }

    private void RemoveHotPixels(AllSkyImage image)
    {
        if (_profile.Current.Processing.HotPixelCorrection)
        {
            using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Remove Hot Pixels", t)));
            image.BayerHotPixelCorrection(_profile.Current.Processing.HotPixelThresholdPercent);
        }
    }

    private AllSkyImage DebayerImage(AllSkyImage image)
    {
        using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Debayer", t)));
        return Debayer.FromImage(image);
    }

    private double Median(AllSkyImage image)
    {
        using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Median", t)));
        // Green median is used for exposure prediction
        var greenMedian = image.Median(channel: 1);
        return greenMedian;
    }

    private void Stretch(AllSkyImage image)
    {
        using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Linked Stretch", t)));
        image.StretchLinked();
    }

    private void AutoSCurve(AllSkyImage image)
    {
        if (_profile.Current.Processing.AutoSCurve)
        {
            using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Auto S Curve", t)));
            image.AutoSCurve(_profile.Current.Processing.AutoSCurveContrast);
        }
    }

    private Mat To8BitMat(AllSkyImage image)
    {
        using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("To 8-Bit Mat", t)));
        return image.To8BitMat();
    }

    private void WhiteBalance(AllSkyImage image)
    {
        using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("White Balance", t)));

        double biasR = _sunService.IsDaytime() ? _profile.Current.Camera.DaytimeBiasR : _profile.Current.Camera.NighttimeBiasR;
        double biasG = _sunService.IsDaytime() ? _profile.Current.Camera.DaytimeBiasG : _profile.Current.Camera.NighttimeBiasG;
        double biasB = _sunService.IsDaytime() ? _profile.Current.Camera.DaytimeBiasB : _profile.Current.Camera.NighttimeBiasB;
        double scaleR = Math.Clamp(_profile.Current.Processing.WbRedScale, 0, 1);
        double scaleG = Math.Clamp(_profile.Current.Processing.WbGreenScale, 0, 1);
        double scaleB = Math.Clamp(_profile.Current.Processing.WbBlueScale, 0, 1);
        biasR = Math.Clamp(biasR, 0, 1);
        biasG = Math.Clamp(biasG, 0, 1);
        biasB = Math.Clamp(biasB, 0, 1);

        if (scaleR == 1 && scaleG == 1 && scaleB == 1 && biasR == 0 && biasG == 0 && biasB == 0) return;

        image.WhiteBalance(scaleR, scaleG, scaleB, biasR, biasG, biasB);
    }

    private void Rotate(Mat mat)
    {
        if (_profile.Current.Image.Rotation != 0)
        {
            using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Rotate", t)));
            Transform.Rotate(mat, _profile.Current.Image.Rotation);
        }
    }

    private void FlipHorizontal(Mat mat)
    {
        if (_profile.Current.Image.FlipHorizontal)
        {
            using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Flip Horizontal", t)));
            Transform.FlipHorizontal(mat);
        }
    }

    private void FlipVertical(Mat mat)
    {
        if (_profile.Current.Image.FlipVertical)
        {
            using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Flip Vertical", t)));
            Transform.FlipVertical(mat);
        }
    }

    private void CircleMask(Mat mat)
    {
        if (_profile.Current.Processing.CircleMaskDiameter > 0)
        {
            using var _ = Benchmark.Start(t => FitsProcessTimingTracker.Items.Add(new("Circle Mask", t)));
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
