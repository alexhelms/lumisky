using CliWrap;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LumiSky.Rpicam;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        builder.Services.AddSingleton<RpicamService>();

        var app = builder.Build();

        app.MapPost("/execute", async ([FromQuery] string args, [FromServices] RpicamService rpicam, CancellationToken token) =>
        {
            if (rpicam.IsRunning)
                return Results.Conflict();

            var result = await rpicam.Execute(args, token);
            return Results.Json(result);
        });

        app.MapGet("/download", ([FromQuery] string filename, [FromServices] RpicamService rpicam) =>
        {
            var fileInfo = new FileInfo(Path.Combine(rpicam.WorkingDir, filename));
            if (fileInfo.Exists)
            {
                return Results.File(fileInfo.FullName, "application/octet-stream");
            }

            return Results.NotFound();
        });

        app.Run();
    }
}

public class RpicamService
{
    private readonly string _rpicamStillPath;

    public string WorkingDir => "/tmp/rpicam";

    public bool IsRunning { get; private set; }

    public RpicamService(IConfiguration config)
    {
        _rpicamStillPath = config["rpicam-still"] ?? throw new ArgumentException("rpicam-still");
        if (!File.Exists(_rpicamStillPath))
        {
            throw new FileNotFoundException("rpicam-still not found");
        }

        Directory.CreateDirectory(WorkingDir);
    }

    public async Task<RpicamResult> Execute(string args, CancellationToken token)
    {
        try
        {
            IsRunning = true;

            var stdout = new StringBuilder(1024);
            var stderr = new StringBuilder(1024);

            CommandResult result = await Cli.Wrap(_rpicamStillPath)
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
}

public record RpicamResult(int ExitCode, TimeSpan Elapsed, string Stdout, string Stderr);