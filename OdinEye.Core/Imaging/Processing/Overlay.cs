using CliWrap;
using Emgu.CV;
using OdinEye.Core.IO;
using OdinEye.Core.Memory;
using System.Runtime.InteropServices;
using System.Text;

namespace OdinEye.Core.Imaging.Processing;

public static class Overlay
{
    private static string FontPath;
    private static string PythonExecutable;
    private static string PythonOverlayPath;
    
    static Overlay()
    {
        var assembly = typeof(Overlay).Assembly;
        var directory = Path.GetDirectoryName(assembly.Location)!;
        FontPath = Path.Combine(directory, "Fonts", "RobotoMono-Regular.ttf");
        var defaultPythonExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "python" 
            : "/usr/bin/python3";
        PythonExecutable = Environment.GetEnvironmentVariable("PYTHONEXE") ?? defaultPythonExecutable;
        PythonOverlayPath = Path.Combine(directory, "python", "overlay.py");
    }

    public static async Task DrawText(
        string blobFilename, 
        int width, 
        int height, 
        string[] text,
        int[] x,
        int[] y, 
        int fontSize,
        string fill = "#ffffff", 
        string strokeFill = "#000000", 
        int strokeWidth = 1)
    {
        const char JoinChar = '~';
        var stderr = new StringBuilder();

        var result = await Cli.Wrap(PythonExecutable)
            .WithArguments(c => c
                .Add($"\"{PythonOverlayPath}\"", escape: false)
                .Add($"\"{blobFilename}\"", escape: false)
                .Add(width)
                .Add(height)
                .Add($"\"{FontPath}\"", escape: false)
                .Add($"\"{string.Join(JoinChar, text)}\"", escape: false)
                .Add(fontSize)
                .Add($"\"{string.Join(JoinChar, x.Select(i => i.ToString()))}\"", escape: false)
                .Add($"\"{string.Join(JoinChar, y.Select(i => i.ToString()))}\"", escape: false)
                .Add("--fill")
                .Add($"\"{fill}\"", escape: false)
                .Add("--stroke_fill")
                .Add($"\"{strokeFill}\"", escape: false)
                .Add("--stroke_width")
                .Add($"\"{strokeWidth}\"", escape: false))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
            .ExecuteAsync();

        if (stderr.Length > 0)
        {
            Log.Error("Error creating image overlay: {Error}", stderr.ToString());
        }
    }

    public static Task DrawText(string blobFilename, int width, int height, string text, int x, int y, int fontSize,
        string fill = "#ffffff", string strokeFill = "#000000", int strokeWidth = 1)
        => DrawText(blobFilename, width, height, [text], [x], [y], fontSize, fill, strokeFill, strokeWidth);

    public static async Task DrawCardinalPoints(
        Mat mat,
        string[] labels,
        int fontSize, 
        string fill, 
        string strokeFill, 
        int strokeWidth, 
        int margin)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(labels.Length, 4);

        int width = mat.Cols;
        int height = mat.Rows;
        var top = margin + (fontSize / 2);
        var bottom = height - (margin + (fontSize / 2));
        var right = width - (margin + (fontSize / 2));
        var left = margin + (fontSize / 2);

        int[] x = [
            width / 2,
            width / 2,
            right,
            left,
        ];

        int[] y = [
            top,
            bottom,
            height / 2,
            height / 2,
        ];

        using var rawData = new TemporaryFile();
        mat.ToBlob(rawData.Path);
        await DrawText(rawData.Path, width, height, labels, x, y, fontSize, fill, strokeFill, strokeWidth);
        mat.FromBlob(rawData.Path);
    }

    public static async Task DrawCardinalPointsPanorama(
        Mat mat,
        string[] labels,
        int fontSize,
        string fill, 
        string strokeFill, 
        int strokeWidth, 
        int margin)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(labels.Length, 4);
        
        int width = mat.Cols;
        int height = mat.Rows;
        int section = width / 4;

        int[] x = [
            2 * margin,
            2 * margin + section,
            2 * margin + section * 2,
            2 * margin + section * 3,
        ];

        int[] y = [
            height - 2 * margin,
            height - 2 * margin,
            height - 2 * margin,
            height - 2 * margin,
        ];

        using var rawData = new TemporaryFile();
        mat.ToBlob(rawData.Path);
        await DrawText(rawData.Path, width, height, labels, x, y, fontSize, fill, strokeFill, strokeWidth);
        mat.FromBlob(rawData.Path);
    }
}
