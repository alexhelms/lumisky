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

        services.AddSingleton<IProfileProvider, ProfileProvider>();
        services.AddSingleton<DeviceFactory>();
        services.AddSingleton<AllSkyScheduler>();
        services.AddSingleton<ImageService>();
        services.AddTransient<SunService>();
        services.AddTransient<FilenameGenerator>();
        services.AddSingleton<ExposureService>();
        services.AddSingleton<GenerationService>();
        services.AddSingleton<NotificationService>();

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

            await scheduler.ScheduleJob(dayNightTrigger);
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
            q.InterruptJobsOnShutdown = true;
            q.InterruptJobsOnShutdownWithWait = true;

            q.AddTriggerListener<GenerationJobLimiter>(GroupMatcher<TriggerKey>.GroupEquals(JobConstants.Groups.Generation));

            q.AddJobListener<JobExceptionListener>(GroupMatcher<JobKey>.GroupEquals(JobConstants.Groups.Allsky));

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
