using LumiSky.Core.Data;
using LumiSky.Core.Imaging;
using LumiSky.Core.Profile;

namespace LumiSky.Core.Services;

public class FilenameGenerator
{
    private readonly IProfileProvider _profile;
    private readonly SunService _sunService;

    public FilenameGenerator(
        IProfileProvider profile,
        SunService dayNightService)
    {
        _profile = profile;
        _sunService = dayNightService;
    }

    public string ImageExtension => _profile.Current.Image.FileType switch
    {
        ImageFileType.JPEG => ".jpg",
        ImageFileType.PNG => ".png",
        _ => throw new NotImplementedException(),
    };

    public string CreateImageFilename(string imageType, DateTime timestamp, string extension)
    {
        bool isDay = _sunService.IsDaytime;
        var timestampMinus12 = timestamp.AddHours(-12);
        var filename = $"{imageType}_{timestamp:yyyyMMdd}_{timestamp:HHmmss}{extension}";
        var directory = Path.Combine(
            _profile.Current.Capture.DataDirectory,
            imageType,
            isDay ? timestamp.ToString("yyyyMMdd") : timestampMinus12.ToString("yyyyMMdd"),
            isDay ? "day" : "night");
        var path = Path.Combine(directory, filename);
        return path;
    }

    public string CreateTimelapseFilename(GenerationKind generationKind, DateTime timestamp, DateTime begin, DateTime end)
    {
        var kind = generationKind switch
        {
            GenerationKind.Timelapse => "timelapse",
            GenerationKind.PanoramaTimelapse => "panorama",
            _ => throw new NotImplementedException()
        };

        var directory = Path.Combine(_profile.Current.Capture.DataDirectory, "video", kind);
        var filename = $"{kind}_{timestamp:yyyyMMdd-HHmmss}_{begin:yyyyMMdd-HHmmss}_to_{end:yyyyMMdd-HHmmss}.mp4";
        var path = Path.Combine(directory, filename);
        return path;
    }
}
