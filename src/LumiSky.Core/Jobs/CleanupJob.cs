using Humanizer;
using LumiSky.Core.Data;
using LumiSky.Core.Profile;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Linq.Expressions;

namespace LumiSky.Core.Jobs;

[DisallowConcurrentExecution]
public class CleanupJob : JobBase
{
    public static readonly JobKey Key = new(JobConstants.Jobs.Cleanup, JobConstants.Groups.Maintenance);
    
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

        await PruneOrphanedEntities();

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

    private async Task PruneOrphanedEntities()
    {
        await PruneOrphanedDbEntries(_dbContextFactory.CreateDbContext, dbCtx => dbCtx.Images);
        await PruneOrphanedDbEntries(_dbContextFactory.CreateDbContext, dbCtx => dbCtx.RawImages);
        await PruneOrphanedDbEntries(_dbContextFactory.CreateDbContext, dbCtx => dbCtx.Timelapses);
        await PruneOrphanedDbEntries(_dbContextFactory.CreateDbContext, dbCtx => dbCtx.Panoramas);
        await PruneOrphanedDbEntries(_dbContextFactory.CreateDbContext, dbCtx => dbCtx.PanoramaTimelapses);
    }

    private static async Task PruneOrphanedDbEntries(
        Func<AppDbContext> dbContextFactory,
        Expression<Func<AppDbContext, IQueryable<ICanBeCleanedUp>>> itemExpression)
    {
        // Delete any db rows where the filename associated with that entity cannot be found.

        IQueryable<ICanBeCleanedUp>? items = null;

        try
        {
            using var dbContext = dbContextFactory();
            items = itemExpression.Compile()(dbContext);

            var entities = await items
                .AsNoTracking()
                .Select(x => new { x.Id, x.Filename })
                .ToListAsync();

            var entitiesToDelete = new HashSet<int>(entities.Count);

            foreach (var item in entities)
            {
                try
                {
                    var fileInfo = new FileInfo(item.Filename);
                    if (!fileInfo.Exists)
                    {
                        entitiesToDelete.Add(item.Id);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Error getting file properties for {File}", item.Filename);
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
        catch (Exception e)
        {
            if (items is not null)
            {
                var name = items.GetType().GetGenericArguments()[0].Name.Titleize();
                Log.Warning(e, "Error pruning orphaned {Name} db entities", name);
            }
            else
            {
                Log.Warning(e, "Error pruning orphaned db entities");
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
        catch (Exception e)
        {
            Log.Warning(e, "Error deleting empty directories");
        }
    }
}
