using LumiSky.Rpicam.Common;
using Microsoft.AspNetCore.Mvc;
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
        
        app.MapGet("/ping", () =>
        {
            return Results.Ok();
        });

        app.MapPost("/execute", async ([FromQuery] string args, [FromServices] RpicamService rpicam, CancellationToken token) =>
        {
            if (rpicam.IsRunning)
                return Results.Conflict();

            var result = await rpicam.Execute(args, token);
            if (result.ExitCode == 0)
            {
                await rpicam.ConvertDngToTiff(token);
            }

            return Results.Json(result);
        });

        app.MapGet("/download", ([FromQuery] string filename, [FromServices] RpicamService rpicam) =>
        {
            var fileInfo = new FileInfo(Path.Combine(rpicam.WorkingDir, filename));
            if (fileInfo.Exists)
            {
                return Results.File(fileInfo.FullName, "application/octet-stream", filename);
            }

            return Results.NotFound();
        });

        app.Run();
    }
}