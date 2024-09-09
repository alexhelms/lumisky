using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OdinEye.Core.IO;

namespace OdinEye.Core.Data;

public class AppDbContext : IdentityDbContext<AppUser>, IDataProtectionKeyContext
{
    public string DbPath { get; }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public DbSet<Image> Images { get; set; }
    public DbSet<RawImage> RawImages { get; set; }
    public DbSet<Panorama> Panoramas { get; set; }
    public DbSet<Generation> Generations { get; set; }
    public DbSet<Timelapse> Timelapses { get; set; }
    public DbSet<PanoramaTimelapse> PanoramaTimelapses { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        Directory.CreateDirectory(OdinEyePaths.BasePath);
        DbPath = Path.Join(OdinEyePaths.BasePath, "odineye.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RawImage>()
           .Property(x => x.CreatedOn)
           .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Image>()
           .Property(x => x.CreatedOn)
           .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Panorama>()
           .Property(x => x.CreatedOn)
           .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Timelapse>()
           .Property(x => x.CreatedOn)
           .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Generation>()
           .Property(x => x.CreatedOn)
           .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }

    public override int SaveChanges()
    {
        ModifyUpdatedOnColumn();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ModifyUpdatedOnColumn();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ModifyUpdatedOnColumn();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ModifyUpdatedOnColumn();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Automatically update the "CreatedOn" time stamp on all modified entities.
    /// </summary>
    private void ModifyUpdatedOnColumn()
    {
        var addedEntities = ChangeTracker.Entries().Where(c => c.State == EntityState.Added);
        foreach (var entity in addedEntities)
        {
            if (entity.Properties.Any(c => c.Metadata.Name == nameof(Image.CreatedOn)))
            {
                entity.Property(nameof(Image.CreatedOn)).CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
