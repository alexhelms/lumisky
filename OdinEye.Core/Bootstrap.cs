using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OdinEye.Core.Data;
using OdinEye.Core.Devices;
using OdinEye.Core.Jobs;
using OdinEye.Core.Profile;
using OdinEye.Core.Services;
using Quartz;
using SlimMessageBus.Host;
using SlimMessageBus.Host.Memory;

namespace OdinEye.Core;

public static class Bootstrap
{
    public static void ConfigureOdinEyeCore(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite());
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(), lifetime: ServiceLifetime.Scoped);

        services.AddHostedService<DayNightWatcherBackgroundService>();

        services.AddSingleton<IProfileProvider, ProfileProvider>();
        services.AddSingleton<DeviceFactory>();
        services.AddSingleton<AllSkyScheduler>();
        services.AddSingleton<ImageService>();
        services.AddTransient<SunService>();
        services.AddTransient<FilenameGenerator>();
        services.AddSingleton<ExposureService>();
        services.AddSingleton<GenerationService>();

        services.AddSlimMessageBus(config => config
            .WithProviderMemory()
            .AutoDeclareFrom(typeof(Bootstrap).Assembly)
            .AddServicesFromAssembly(typeof(Bootstrap).Assembly));

        services.ConfigureScheduler();
    }

    public static void UseOdinEyeCore(IServiceProvider provider)
    {
        try
        {
            var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
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
            _ = Task.Run(allSkyScheduler.Start);
        }
    }

    private static void ConfigureScheduler(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.UseInMemoryStore();
            q.InterruptJobsOnShutdown = true;
            q.InterruptJobsOnShutdownWithWait = true;

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
