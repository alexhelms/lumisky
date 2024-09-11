using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OdinEye.Core.Data;
using OdinEye.Core.Devices;
using OdinEye.Core.Jobs;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using Quartz;
using Quartz.Impl.Matchers;
using SlimMessageBus.Host;
using SlimMessageBus.Host.Memory;

namespace OdinEye.Core;

public static class Bootstrap
{
    public static void ConfigureOdinEyeCore(this IServiceCollection services)
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

    public static async Task UseOdinEyeCore(IServiceProvider provider)
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
}
