using CliWrap;
using CliWrap.Buffered;
using System.Runtime.InteropServices;

namespace OdinEye.Core.Video;

public static class Ffprobe
{
    private static string _ffprobePath;

    static Ffprobe()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ffprobePath = @"C:\ffmpeg\ffprobe.exe";
        }
        else
        {
            _ffprobePath = "/usr/bin/ffprobe";
        }
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
