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
    public DbSet<Ride> Rides => Set<Ride>();

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
            entity.Property(e => e.AddedAt).HasColumnName("addedAt");
        });

        modelBuilder.Entity<Ride>(entity =>
        {
            entity.ToTable("rides");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BikeId).HasColumnName("bikeId").IsRequired();
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
            entity.HasIndex(e => e.StartTime);
        });
    }
}
