using OdinEye.Core.Imaging;
using OdinEye.Core.Profile;

namespace OdinEye.Core.Services;

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

    public string CreateFilename(string imageType, DateTime timestamp, string extension)
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
}
