using Microsoft.EntityFrameworkCore;
using OdinEye.Core.Data;
using OdinEye.Core.DomainEvents;
using OdinEye.Core.IO;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using Quartz;
using SlimMessageBus;
using System.Text;
using Xabe.FFmpeg;

namespace OdinEye.Core.Jobs;

[DisallowConcurrentExecution]
public class PanoramaTimelapseJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.PanoramaTimelapse, JobConstants.Groups.Generation);

    private readonly IProfileProvider _profile;
    private readonly IMessageBus _messageBus;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly FilenameGenerator _filenameGenerator;

    public int GenerationId { get; set; }

    private GenerationKind Kind => GenerationKind.PanoramaTimelapse;

    public PanoramaTimelapseJob(
        IProfileProvider profile,
        IMessageBus messageBus,
        IDbContextFactory<AppDbContext> dbContextFactory,
        FilenameGenerator filenameGenerator)
    {
        _profile = profile;
        _messageBus = messageBus;
        _dbContextFactory = dbContextFactory;
        _filenameGenerator = filenameGenerator;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        if (GenerationId == 0) throw new InvalidOperationException($"{nameof(GenerationId)} has not been set");

        if (await ShouldSkipJob())
        {
            // User deleted a queued job or canceled a queued job.
            return;
        }

        var stdout = new StringBuilder(4096);

        try
        {
            await PersistGenerationStart(context.FireInstanceId);
            await _messageBus.Publish(new GenerationStarting { Id = GenerationId });

            SetupFfmpegPath();

            // Get the begin and end range
            var (begin, end) = await GetBeginAndEndTimestamps();
            var beginLocal = DateTimeOffset.FromUnixTimeSeconds(begin).ToLocalTime();
            var endLocal = DateTimeOffset.FromUnixTimeSeconds(end).ToLocalTime();

            // Get the panoramas used for the timelapse
            List<Panorama> panoramas = [];
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                panoramas = await dbContext.Panoramas
                    .AsNoTracking()
                    .Where(img => begin <= img.ExposedOn && img.ExposedOn <= end)
                    .ToListAsync();
            }

            if (panoramas.Count == 0)
                throw new JobExecutionException($"No panoramas between {beginLocal:s} and {endLocal:s}");

            var conversion = FFmpeg.Conversions
                .New()
                .SetPriority(System.Diagnostics.ProcessPriorityClass.BelowNormal);

            conversion.OnDataReceived += async (sender, args) =>
            {
                stdout.Append(args.Data);

                int frameNumber = 0;
                if (args.Data is { } && args.Data.StartsWith("frame"))
                {
                    // frame=  198 fps= 21 q=29.0 size=  145920KiB time=00:00:03.26 bitrate=365931.7kbits/s speed=0.35x
                    var split = args.Data.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (split.Length == 3)
                    {
                        int.TryParse(split[1], out frameNumber);
                    }
                }

                int progress = (int)((double)frameNumber / panoramas.Count * 100.0);
                await PersistGenerationProgress(progress);
                await _messageBus.Publish(new GenerationProgress { Id = GenerationId });
            };

            using var tempDir = new TemporaryDirectory();
            string outputFilename = BuildOutputFilename(panoramas, tempDir.Path, beginLocal, endLocal);
            var args = BuildFfmpegArgs(panoramas, tempDir.Path, outputFilename);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilename)!);

            Log.Information("Creating panorama timelapse between {Begin:s} and {End:s}, {FrameCount} frames", beginLocal, endLocal, panoramas.Count);
            Log.Information("ffmpeg {Arguments}", args);
            var result = await conversion.Start(args, context.CancellationToken);
            context.CancellationToken.ThrowIfCancellationRequested();

            await PersistGenerationProgress(100);
            await _messageBus.Publish(new GenerationProgress { Id = GenerationId });

            await VerifyOutput(outputFilename);
            await PersistTimelapse(beginLocal, endLocal, outputFilename);
            await PersistGenerationAsSuccess(outputFilename);

            Log.Information("Panorama timelapse finished in {Elapsed:F3} seconds", result.Duration.TotalSeconds);
        }
        catch (Xabe.FFmpeg.Exceptions.ConversionException)
        {
            Log.Debug(stdout.ToString());
            throw;
        }
        catch (Exception e)
        {
            await PersistGenerationAsFailure();
            Log.Error(e, "Error generating panorama timelapse");
            throw;
        }
        finally
        {
            await _messageBus.Publish(new GenerationComplete { Id = GenerationId });
        }
    }

    private void SetupFfmpegPath()
    {
        var ffmpegFileInfo = new FileInfo(_profile.Current.Generation.FfmpegPath);
        if (!ffmpegFileInfo.Exists)
            throw new FileNotFoundException($"ffmpeg not found at {_profile.Current.Generation.FfmpegPath}", _profile.Current.Generation.FfmpegPath);

        FFmpeg.SetExecutablesPath(ffmpegFileInfo.DirectoryName);
    }

    private async Task<(long, long)> GetBeginAndEndTimestamps()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var generation = await dbContext.Generations.FirstAsync(generation => generation.Id == GenerationId);
        return (generation.RangeBegin, generation.RangeEnd);
    }

    private string BuildFfmpegArgs(List<Panorama> panoramas, string tempPath, string outputFilename)
    {
        string imageListFilename = CreateFileList(panoramas, tempPath);
        string encoder = _profile.Current.Generation.PanoramaCodec switch
        {
            Profile.VideoCodec.H264 => "libx264",
            Profile.VideoCodec.H265 => "libx265",
            _ => "libx264",
        };

        var argsBuilder = new StringBuilder(512);
        argsBuilder.Append("-y -f concat -safe 0 ");
        argsBuilder.AppendFormat("-r {0} ", _profile.Current.Generation.PanoramaFrameRate);
        argsBuilder.AppendFormat("-i \"{0}\" ", imageListFilename);

        // Height must be divisible by 2 per libx264/libx265
        if (_profile.Current.Generation.PanoramaWidth > 0)
            argsBuilder.AppendFormat("-vf \"scale={0}:-2\" ", _profile.Current.Generation.PanoramaWidth);
        else
            argsBuilder.Append("-vf \"scale=iw:-2\" ");

        argsBuilder.AppendFormat("-c:v {0} ", encoder);
        argsBuilder.Append("-preset slow ");
        argsBuilder.AppendFormat("-crf {0} ", _profile.Current.Generation.PanoramaQuality);
        argsBuilder.AppendFormat("\"{0}\"", outputFilename);

        return argsBuilder.ToString();
    }

    private string BuildOutputFilename(List<Panorama> panoramas, string tempPath, DateTimeOffset begin, DateTimeOffset end)
    {
        string imageListFilename = CreateFileList(panoramas, tempPath);
        string outputFilename = _filenameGenerator.CreateTimelapseFilename(Kind, DateTime.Now, begin.DateTime, end.DateTime);
        return outputFilename;
    }

    private async Task<bool> ShouldSkipJob()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var generation = await dbContext.Generations.FirstOrDefaultAsync(generation => generation.Id == GenerationId);

        // Generation was deleted, don't process this job.
        if (generation is null) return true;

        // Generations only in the Queued state can be processed.
        return generation.State != GenerationState.Queued;
    }

    private async Task PersistGenerationStart(string instanceId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var generation = await dbContext.Generations.FirstAsync(generation => generation.Id == GenerationId);
        generation.State = GenerationState.Running;
        generation.StartedOn = DateTime.UtcNow;
        generation.JobInstanceId = instanceId;
        await dbContext.SaveChangesAsync();
    }

    private async Task PersistGenerationProgress(int progress)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        await dbContext.Generations
            .Where(generation => generation.Id == GenerationId)
            .ExecuteUpdateAsync(x => x.SetProperty(g => g.Progress, progress));
    }

    private async Task PersistGenerationAsFailure()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var generation = await dbContext.Generations.FirstAsync(generation => generation.Id == GenerationId);
        generation.State = GenerationState.Failed;
        generation.CompletedOn = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private async Task PersistGenerationAsSuccess(string outputFilename)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var generation = await dbContext.Generations.FirstAsync(generation => generation.Id == GenerationId);
        generation.State = GenerationState.Success;
        generation.CompletedOn = DateTime.UtcNow;
        generation.OutputFilename = outputFilename;
        await dbContext.SaveChangesAsync();
    }

    private async Task PersistTimelapse(DateTimeOffset begin, DateTimeOffset end, string outputFilename)
    {
        var panorama = new Data.PanoramaTimelapse
        {
            RangeBegin = begin.ToUnixTimeSeconds(),
            RangeEnd = end.ToUnixTimeSeconds(),
            Filename = outputFilename,
        };

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PanoramaTimelapses.Add(panorama);
        await dbContext.SaveChangesAsync();

        var generation = await dbContext.Generations.FirstAsync(generation => generation.Id == GenerationId);
        generation.PanoramaTimelapseId = panorama.Id;
        await dbContext.SaveChangesAsync();
    }

    private string CreateFileList(List<Panorama> panoramas, string directory)
    {
        // Create the image list in a format ffmpeg can read
        string imageListFilename = Path.Combine(directory, "imagelist.txt");
        File.WriteAllLines(imageListFilename, panoramas.Select(pano => $"file '{pano.Filename}'"));
        return imageListFilename;
    }

    private async Task VerifyOutput(string filename)
    {
        var fileInfo = new FileInfo(filename);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Output panorama timelapse not found", filename);

        var mediaInfo = await FFmpeg.GetMediaInfo(filename);
        if (mediaInfo.Size == 0)
            throw new InvalidOperationException("Output panorama timelapse is malformed");
    }
}
