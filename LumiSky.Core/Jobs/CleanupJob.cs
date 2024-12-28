using Humanizer;
using LumiSky.Core.Data;
using LumiSky.Core.Profile;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace LumiSky.Core.Jobs;

[DisallowConcurrentExecution]
public class CleanupJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.CleanupJob, JobConstants.Groups.Maintenance);
    
    private readonly IProfileProvider _profile;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public CleanupJob(
        IProfileProvider profile,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _profile = profile;
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task OnExecute(IJobExecutionContext context)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);
        if (!_profile.Current.App.EnableCleanup)
        {
            Log.Information("Cleanup is disabled");
            return;
        }

        Log.Information("Starting cleanup");

        if (_profile.Current.App.EnableImageCleanup)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            await CleanUp(dbContext.Images.AsQueryable<ICanBeCleanedUp>(), _profile.Current.App.ImageCleanupAge);
        }

        if (_profile.Current.App.EnableRawImageCleanup)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            await CleanUp(dbContext.RawImages.AsQueryable<ICanBeCleanedUp>(), _profile.Current.App.RawImageCleanupAge);
        }

        if (_profile.Current.App.EnableTimelapseCleanup)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            await CleanUp(dbContext.Timelapses.AsQueryable<ICanBeCleanedUp>(), _profile.Current.App.TimelapseCleanupAge);
            
            // Delete timelapse generations
            var expirationDate = DateTime.UtcNow.AddDays(-1 * Math.Abs(_profile.Current.App.TimelapseCleanupAge));
            int deletedCount = await dbContext.Generations
                .Where(x => x.CompletedOn < expirationDate)
                .Where(x => x.TimelapseId != null)
                .ExecuteDeleteAsync();
            if (deletedCount > 0)
                Log.Information("{Count} timelapse generations deleted", deletedCount);
        }

        if (_profile.Current.App.EnablePanoramaCleanup)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            await CleanUp(dbContext.Panoramas.AsQueryable<ICanBeCleanedUp>(), _profile.Current.App.PanoramaCleanupAge);
        }

        if (_profile.Current.App.EnablePanoramaTimelapseCleanup)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            await CleanUp(dbContext.PanoramaTimelapses.AsQueryable<ICanBeCleanedUp>(), _profile.Current.App.PanoramaTimelapseCleanupAge);

            // Delete panorama timelapse generations
            var expirationDate = DateTime.UtcNow.AddDays(-1 * Math.Abs(_profile.Current.App.PanoramaTimelapseCleanupAge));
            int deletedCount = await dbContext.Generations
                .Where(x => x.CompletedOn < expirationDate)
                .Where(x => x.PanoramaTimelapseId != null)
                .ExecuteDeleteAsync();
            if (deletedCount > 0)
                Log.Information("{Count} panorama timelapse generations deleted", deletedCount);
        }

        await PrunOrphanedEntities();
        
        DeleteEmptyDirectories(_profile.Current.App.ImageDataPath);

        Log.Information("Cleanup complete");
    }

    private static async Task CleanUp(IQueryable<ICanBeCleanedUp> items, int age)
    {
        var expirationDate = DateTime.UtcNow.AddDays(-1 * Math.Abs(age));

        var query = items
            .AsNoTracking()
            .Where(x => x.CreatedOn < expirationDate)
            .Where(x => !x.IsFavorite)
            .AsQueryable();

        var itemsToDelete = await query
            .Select(x => new { x.Id, x.Filename })
            .ToListAsync();

        var deletedCount = await query.ExecuteDeleteAsync();
        if (deletedCount > 0)
        {
            foreach (var item in itemsToDelete)
            {
                var fileInfo = new FileInfo(item.Filename);
                if (fileInfo.Exists)
                {
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e, "Could not delete {Filename}", item.Filename);
                    }
                }
            }

            Log.Information("{Count} {Type} deleted",
                deletedCount,
                items.GetType().GenericTypeArguments[0].Name.Titleize().ToLowerInvariant().Pluralize());
        }
    }

    private async Task PrunOrphanedEntities()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        await PruneOrphanedDbEntries(dbContext.Images);
        await PruneOrphanedDbEntries(dbContext.RawImages);
        await PruneOrphanedDbEntries(dbContext.Timelapses);
        await PruneOrphanedDbEntries(dbContext.Panoramas);
        await PruneOrphanedDbEntries(dbContext.PanoramaTimelapses);
    }

    private static async Task PruneOrphanedDbEntries(IQueryable<ICanBeCleanedUp> items)
    {
        // Delete any db rows where the filename associated with that entity cannot be found.

        var entities = await items
            .AsNoTracking()
            .Select(x => new { x.Id, x.Filename })
            .ToListAsync();

        var entitiesToDelete = new HashSet<int>(entities.Count);

        foreach (var item in entities)
        {
            var fileInfo = new FileInfo(item.Filename);
            if (!fileInfo.Exists)
            {
                entitiesToDelete.Add(item.Id);
            }
        }

        if (entitiesToDelete.Count > 0)
        {
            int deletedCount = await items
                .Where(x => entitiesToDelete.Contains(x.Id))
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                Log.Information("{Count} orphaned {Type} pruned",
                    deletedCount,
                    items.GetType().GenericTypeArguments[0].Name.Titleize().ToLowerInvariant().Pluralize());
            }
        }
    }

    private static void DeleteEmptyDirectories(string path)
    {
        try
        {
            foreach (var d in Directory.EnumerateDirectories(path))
            {
                DeleteEmptyDirectories(d);
            }

            var entries = Directory.EnumerateFileSystemEntries(path);

            if (!entries.Any())
            {
                try
                {
                    Directory.Delete(path);
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
    }
}
