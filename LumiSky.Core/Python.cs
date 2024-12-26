using CliWrap;
using LumiSky.Core.Imaging.Processing;
using System.Runtime.InteropServices;
using System.Text;

namespace LumiSky.Core;

public static class Python
{
    public static string PythonExecutablePath { get; private set; } = null!;

    public static async Task Initialize()
    {
        var assembly = typeof(OverlayRenderer).Assembly;
        var directory = Path.GetDirectoryName(assembly.Location)!;
        var defaultPythonExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "python"
            : "/usr/bin/python3";
        PythonExecutablePath = Environment.GetEnvironmentVariable("PYTHONEXE") ?? defaultPythonExecutable;

        var stdout = new StringBuilder(512);
        var result = await Cli.Wrap(PythonExecutablePath)
            .WithArguments("--version")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (result.IsSuccess)
        {
            var output = stdout.ToString().Trim();
            if (!output.StartsWith("Python"))
            {
                throw new FileNotFoundException("Python not found", PythonExecutablePath);
            }
        }
        else
        {
            throw new FileNotFoundException("Python not found", PythonExecutablePath);
        }
    }
}
