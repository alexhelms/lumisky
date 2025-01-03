using CliWrap;
using CliWrap.Buffered;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Video;

public static class Ffprobe
{
    private static string _ffprobePath;

    public static string DefaultFfprobePath { get; }

    static Ffprobe()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            DefaultFfprobePath = @"C:\ffmpeg\bin\ffprobe.exe";
        }
        else
        {
            DefaultFfprobePath = "/usr/bin/ffprobe";
        }

        _ffprobePath = DefaultFfprobePath;
    }

    public static void SetFfprobePath(string path)
    {
        _ffprobePath = path;
        CheckFfprobePath();
    }

    private static void CheckFfprobePath()
    {
        if (!File.Exists(_ffprobePath))
            throw new FileNotFoundException("Ffprobe not found", _ffprobePath);
    }

    public static async Task<bool> IsH264(string filename, CancellationToken token = default)
    {
        var streamInfo = await GetStream0Info(filename, token);
        return streamInfo.Contains("codec_name=h264");
    }

    public static async Task<bool> IsH265(string filename, CancellationToken token = default)
    {
        var streamInfo = await GetStream0Info(filename, token);
        return streamInfo.Contains("codec_name=hevc");
    }

    public static async Task<(int, int)> GetWidthAndHeight(string filename, CancellationToken token = default)
    {
        CheckFfprobePath();

        string arguments = $"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x \"{filename}\"";

        var result = await Cli.Wrap(_ffprobePath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(token);

        var split = result.StandardOutput.Split('x');
        if (split.Length == 2)
        {
            int.TryParse(split[0], out var width);
            int.TryParse(split[1], out var height);
            return (width, height);
        }

        return (0, 0);
    }

    private static async Task<string> GetStream0Info(string filename, CancellationToken token = default)
    {
        CheckFfprobePath();

        string arguments = $"-v error -hide_banner -select_streams v:0 -show_streams \"{filename}\"";

        var result = await Cli.Wrap(_ffprobePath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(token);

        return result.StandardOutput;
    }
}
