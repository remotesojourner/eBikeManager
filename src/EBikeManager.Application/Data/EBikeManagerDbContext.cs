using EBikeManager.Application.Data.Converters;
using EBikeManager.Application.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Data;

internal class EBikeManagerDbContext : DbContext
{
    public EBikeManagerDbContext(DbContextOptions<EBikeManagerDbContext> options) : base(options)
    {
    }

    public DbSet<ConfigEntry> Configs => Set<ConfigEntry>();
    public DbSet<SecretEntry> Secrets => Set<SecretEntry>();
    public DbSet<Bike> Bikes => Set<Bike>();
    public DbSet<BikePicture> BikePictures => Set<BikePicture>();
    public DbSet<BikeDocument> BikeDocuments => Set<BikeDocument>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<RideExport> RideExports => Set<RideExport>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConfigEntry>(entity =>
        {
            entity.ToTable("config");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value").IsRequired();
        });

        modelBuilder.Entity<SecretEntry>(entity =>
        {
            entity.ToTable("secrets");
            entity.HasKey(e => e.Name);
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        });

        modelBuilder.Entity<Bike>(entity =>
        {
            entity.ToTable("bikes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Model).HasColumnName("model").IsRequired();
            entity.Property(e => e.AddedAt).HasColumnName("addedAt");
            entity.Property(e => e.ProfileJson).HasColumnName("profileJson");
            entity.Property(e => e.StateOfChargeJson).HasColumnName("stateOfChargeJson");
            entity.Property(e => e.PassJson).HasColumnName("passJson");
            entity.Property(e => e.LocationJson).HasColumnName("locationJson");
            entity.Property(e => e.HasFlowPlus).HasColumnName("hasFlowPlus");
            entity.Property(e => e.DetailsUpdatedAt).HasColumnName("detailsUpdatedAt");
        });

        modelBuilder.Entity<BikePicture>(entity =>
        {
            entity.ToTable("bikePictures");
            entity.HasKey(e => e.BikeId);
            entity.Property(e => e.BikeId).HasColumnName("bikeId");
            entity.Property(e => e.SourceUrl).HasColumnName("sourceUrl").IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("contentType").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.SavedAt).HasColumnName("savedAt");
            entity.HasOne<Bike>().WithOne().HasForeignKey<BikePicture>(e => e.BikeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BikeDocument>(entity =>
        {
            entity.ToTable("bikeDocuments");
            entity.HasKey(e => new { e.BikeId, e.FileId });
            entity.Property(e => e.BikeId).HasColumnName("bikeId");
            entity.Property(e => e.FileId).HasColumnName("fileId");
            entity.Property(e => e.FileType).HasColumnName("fileType").IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("contentType").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.AddedAt).HasColumnName("addedAt");
            entity.Property(e => e.SourceUpdatedAt).HasColumnName("sourceUpdatedAt");
            entity.Property(e => e.SavedAt).HasColumnName("savedAt");
            entity.HasOne<Bike>().WithMany().HasForeignKey(e => e.BikeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ride>(entity =>
        {
            entity.ToTable("rides");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BikeId).HasColumnName("bikeId").IsRequired();
            entity.Property(e => e.BikeName).HasColumnName("bikeName");
            entity.Property(e => e.BikeModel).HasColumnName("bikeModel");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.StartTime).HasColumnName("startTime");
            entity.Property(e => e.EndTime).HasColumnName("endTime");
            entity.Property(e => e.TimeZone).HasColumnName("timeZone");
            entity.Property(e => e.DistanceMeters).HasColumnName("distanceMeters");
            entity.Property(e => e.MovingSeconds).HasColumnName("movingSeconds");
            entity.Property(e => e.CaloriesKcal).HasColumnName("caloriesKcal");
            entity.Property(e => e.ElevationGainMeters).HasColumnName("elevationGainMeters");
            entity.Property(e => e.AverageSpeedKmh).HasColumnName("averageSpeedKmh");
            entity.Property(e => e.RiderEnergySharePercent).HasColumnName("riderEnergySharePercent");
            entity.Property(e => e.AverageRiderPowerWatts).HasColumnName("averageRiderPowerWatts");
            entity.Property(e => e.SummaryJson).HasColumnName("summaryJson").HasDefaultValue("{}");
            entity.Property(e => e.FirstSeenAt).HasColumnName("firstSeenAt");
            entity.Property(e => e.FitPath).HasColumnName("fitPath");
            entity.Property(e => e.FitSha256).HasColumnName("fitSha256");
            entity.Property(e => e.FitSizeBytes).HasColumnName("fitSizeBytes");
            entity.Property(e => e.FitDownloadedAt).HasColumnName("fitDownloadedAt");
            entity.Property(e => e.FitUnavailable).HasColumnName("fitUnavailable").HasDefaultValue(false);
            entity.Property(e => e.FitError).HasColumnName("fitError");
            entity.Property(e => e.FitTimerSeconds).HasColumnName("fitTimerSeconds");
            entity.Property(e => e.FitDistanceMeters).HasColumnName("fitDistanceMeters");
            entity.Property(e => e.FitAveragePowerWatts).HasColumnName("fitAveragePowerWatts");
            entity.Property(e => e.FitHasGps).HasColumnName("fitHasGps");
            entity.Property(e => e.GpxPath).HasColumnName("gpxPath");
            entity.Property(e => e.GpxDownloadedAt).HasColumnName("gpxDownloadedAt");
            entity.Property(e => e.GpxUnavailable).HasColumnName("gpxUnavailable").HasDefaultValue(false);
            entity.HasIndex(e => e.StartTime);
        });

        modelBuilder.Entity<RideExport>(entity =>
        {
            entity.ToTable("rideExports");
            entity.HasKey(e => new { e.RideId, e.Integration });
            entity.Property(e => e.RideId).HasColumnName("rideId");
            entity.Property(e => e.Integration).HasColumnName("integration");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.RemoteId).HasColumnName("remoteId");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Problem).HasColumnName("problem");
            entity.Property(e => e.Attempts).HasColumnName("attempts");
            entity.Property(e => e.LastAttemptAt).HasColumnName("lastAttemptAt");
            entity.Property(e => e.ExportedAt).HasColumnName("exportedAt");
            entity.HasOne<Ride>().WithMany().HasForeignKey(e => e.RideId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
