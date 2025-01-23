using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LumiSky.Core.Data;
using LumiSky.Core.Devices;
using LumiSky.Core.Jobs;
using LumiSky.Core.Profile;
using LumiSky.Core.Services;
using Quartz;
using Quartz.Impl.Matchers;
using SlimMessageBus.Host;
using SlimMessageBus.Host.Memory;
using System.Reflection;
using System.Runtime.InteropServices;
using LumiSky.Core.IO.Fits;
using LumiSky.Core.Utilities;
using LumiSky.Core.Video;
using LumiSky.Core.IO;
using LumiSky.Rpicam.Common;

namespace LumiSky.Core;

public static class Bootstrap
{
    static Bootstrap()
    {
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
    }

    public static void ConfigureLumiSkyCore(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite());
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(), lifetime: ServiceLifetime.Scoped);
        services.AddMemoryCache();

        services.AddSingleton<IProfileProvider, ProfileProvider>();
        services.AddSingleton<DeviceFactory>();
        services.AddSingleton<AllSkyScheduler>();
        services.AddSingleton<ImageService>();
        services.AddTransient<SunService>();
        services.AddTransient<FilenameGenerator>();
        services.AddSingleton<ExposureService>();
        services.AddSingleton<GenerationService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<FocusService>();
        services.AddSingleton<PublishService>();
        services.AddSingleton<RpicamService>();
        services.AddTransient<IndiCamera>();
        services.AddTransient<RaspiWebCamera>();
        services.AddTransient<RaspiNativeCamera>();

        services.AddSlimMessageBus(config => config
            .WithProviderMemory()
            .AutoDeclareFrom(typeof(Bootstrap).Assembly)
            .AddServicesFromAssembly(typeof(Bootstrap).Assembly));

        services.ConfigureScheduler();
    }

    public static async Task UseLumiSkyCore(IServiceProvider provider)
    {
        try
        {
            var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (dbContext.Database.GetPendingMigrations().Any())
            {
                await dbContext.Database.MigrateAsync();
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error applying database migration");
            throw;
        }

        try
        {
            var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Delete any incomplete generation jobs.
            var rowsToDelete = dbContext.Generations
                .Where(generation => generation.State == GenerationState.Queued || generation.State == GenerationState.Running);
            dbContext.Generations.RemoveRange(rowsToDelete);
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error cleaning up generation database rows");
            throw;
        }

        var profile = provider.GetRequiredService<IProfileProvider>();
        try
        {
            profile.LoadProfiles();
            profile.Current.App.AppVersion = RuntimeUtil.Version;
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error loading profiles");
            throw;
        }

        try
        {
            await VerifyNativeDependencies(provider);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error verifying native dependencies");
            throw;
        }

        ConfigureFfmpeg(profile);

        Directory.CreateDirectory(LumiSkyPaths.Temp);

        if (profile.Current.Capture.AutoStart)
        {
            var allSkyScheduler = provider.GetRequiredService<AllSkyScheduler>();
            await allSkyScheduler.Start();
        }

        try
        {
            // Start maintenance jobs
            var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
            var scheduler = await schedulerFactory.GetScheduler();

            var dayNightTrigger = TriggerBuilder.Create()
                .WithIdentity(TriggerKeys.DayNight)
                .ForJob(DayNightJob.Key)
                .WithSimpleSchedule(o => o
                    .WithInterval(TimeSpan.FromMinutes(1))
                    .RepeatForever())
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(5))
                .Build();

            var cleanupTrigger = TriggerBuilder.Create()
                .WithIdentity(TriggerKeys.Cleanup)
                .ForJob(CleanupJob.Key)
                .WithCronSchedule("0 0 10 * * ?")  // 10am every day
                .Build();

            await scheduler.ScheduleJob(dayNightTrigger);
            await scheduler.ScheduleJob(cleanupTrigger);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error starting maintenance jobs");
            throw;
        }
    }

    private static void ConfigureScheduler(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.UseInMemoryStore();
            q.UseDedicatedThreadPool(maxConcurrency: 10);
            q.InterruptJobsOnShutdown = true;
            q.InterruptJobsOnShutdownWithWait = true;

            q.AddTriggerListener<GenerationJobLimiter>(GroupMatcher<TriggerKey>.GroupEquals(JobConstants.Groups.Generation));

            q.AddJobListener<JobExceptionListener>(GroupMatcher<JobKey>.GroupEquals(JobConstants.Groups.Allsky));

            q.AddJob<ExportJob>(c => c
                .WithIdentity(ExportJob.Key)
                .StoreDurably()
                .Build());

            q.AddJob<PublishJob>(c => c
                .WithIdentity(PublishJob.Key)
                .StoreDurably()
                .Build());

            q.AddJob<CleanupJob>(c => c
                .WithIdentity(CleanupJob.Key)
                .StoreDurably()
                .Build());

            q.AddJob<DayNightJob>(c => c
                .WithIdentity(DayNightJob.Key)
                .StoreDurably()
                .Build());

            q.AddJob<TimelapseJob>(c => c
                .WithIdentity(TimelapseJob.Key)
                .StoreDurably()
                .Build());

            q.AddJob<PanoramaTimelapseJob>(c => c
                .WithIdentity(PanoramaTimelapseJob.Key)
                .StoreDurably()
                .Build());
        });

        services.AddQuartzHostedService();
    }

    private static async Task VerifyNativeDependencies(IServiceProvider provider)
    {
        // Invoking this calls the static ctor which checks cfitsio for reentrancy flag.
        _ = FitsFile.Native.FitsIsReentrant();

        // Invoking this calls down to the opencv native binary.
        _ = Emgu.CV.CvInvoke.BuildInformation;

        await Python.Initialize();
    }

    private static void ConfigureFfmpeg(IProfileProvider profile)
    {
        // Do not throw if ffmpeg/ffprobe not found, user can set these in the settings.

        try
        {
            Ffmpeg.SetFfmpegPath(profile.Current.Generation.FfmpegPath);
        }
        catch (FileNotFoundException)
        {
            Log.Error("Ffmpeg not found at {Path}", profile.Current.Generation.FfmpegPath);
        }

        try
        {
            Ffprobe.SetFfprobePath(profile.Current.Generation.FfprobePath);
        }
        catch (FileNotFoundException)
        {
            Log.Error("Ffprobe not found at {Path}", profile.Current.Generation.FfprobePath);
        }
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        var prefix = string.Empty;
        var extension = ".dll";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            prefix = "lib";
            extension = ".so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            prefix = "lib";
            extension = ".dylib";
        }

        IntPtr handle = IntPtr.Zero;
        NativeLibrary.TryLoad($"./runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{prefix}{libraryName}{extension}", assembly, searchPath, out handle);
        return handle;
    }
}
