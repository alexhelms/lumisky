using CliWrap;
using System.Text;

namespace LumiSky.Rpicam.Common;

public class RpicamService
{
    public static readonly string RpicamStill = "/usr/bin/rpicam-still";
    public static readonly string UnprocessedRaw = "/usr/bin/unprocessed_raw";

    public string WorkingDir => "/tmp/rpicam";

    public bool IsRunning { get; private set; }

    public RpicamService()
    {
        Directory.CreateDirectory(WorkingDir);
    }

    public void CheckExecutablesOrThrow()
    {
        if (!File.Exists(RpicamStill))
        {
            throw new FileNotFoundException($"{RpicamStill} not found");
        }
        
        if (!File.Exists(UnprocessedRaw))
        {
            throw new FileNotFoundException($"{UnprocessedRaw} not found");
        }
    }

    public async Task<bool> AnyCameraAvailable(CancellationToken token = default)
    {
        CheckExecutablesOrThrow();

        var stdout = new StringBuilder(1024);

        var result = await Cli.Wrap(RpicamStill)
            .WithArguments("--list-cameras")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(token);

        var content = stdout.ToString();
        return result.IsSuccess && content.Contains("Available cameras");
    }

    public async Task<RpicamResult> Execute(string args, CancellationToken token = default)
    {
        CheckExecutablesOrThrow();

        try
        {
            IsRunning = true;

            CleanWorkingDir();

            var stdout = new StringBuilder(1024);
            var stderr = new StringBuilder(1024);

            CommandResult result = await Cli.Wrap(RpicamStill)
                .WithArguments(args)
                .WithWorkingDirectory(WorkingDir)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(token);

            return new RpicamResult(result.ExitCode, result.RunTime, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            IsRunning = false;
        }
    }

    public async Task ConvertDngToTiff(CancellationToken token = default)
    {
        CheckExecutablesOrThrow();

        // Convert DNG to TIFF
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"{UnprocessedRaw} -T {WorkingDir}/*.dng\"")
            .WithWorkingDirectory(WorkingDir)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(token);
    }

    private void CleanWorkingDir()
    {
        var workDirInfo = new DirectoryInfo(WorkingDir);
        foreach (var file in workDirInfo.GetFiles())
        {
            try
            {
                file.Delete();
            }
            catch { }
        }
    }
}
