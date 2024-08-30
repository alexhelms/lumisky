using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OdinEye.Core.Data;
using OdinEye.Core.Devices;
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
        services.AddSingleton<IProfileProvider, ProfileProvider>();
        services.AddSingleton<DeviceFactory>();
        services.AddSingleton<AllSkyScheduler>();
        services.AddSingleton<ImageService>();
        services.AddTransient<SunService>();
        services.AddTransient<FilenameGenerator>();
        services.AddSingleton<ExposureService>();
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
        });
    }
}
