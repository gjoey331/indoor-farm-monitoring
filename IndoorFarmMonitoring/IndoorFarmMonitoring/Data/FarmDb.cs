using IndoorFarmMonitoring.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoorFarmMonitoring.Data
{
    public class FarmDbContext : DbContext
    {
        public FarmDbContext(DbContextOptions<FarmDbContext> options) : base(options)
        {
        }

        public DbSet<PlantSensorData> PlantSensorData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PlantSensorData>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TrayId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PlantType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ActualTemperature)
                    .HasPrecision(5, 2);

                entity.Property(e => e.ActualHumidity)
                    .HasPrecision(5, 2);

                entity.Property(e => e.ActualLightIntensity)
                    .HasPrecision(8, 2);

                entity.Property(e => e.ActualPhLevel)
                    .HasPrecision(4, 2);

                entity.Property(e => e.TargetTemperature)
                    .HasPrecision(5, 2);

                entity.Property(e => e.TargetHumidity)
                    .HasPrecision(5, 2);

                entity.Property(e => e.TargetLightIntensity)
                    .HasPrecision(8, 2);

                entity.Property(e => e.TargetPhLevel)
                    .HasPrecision(4, 2);

                entity.Property(e => e.TolerancePercentage)
                    .HasPrecision(5, 2);

                entity.Property(e => e.TemperatureDeviation)
                    .HasPrecision(6, 2);

                entity.Property(e => e.HumidityDeviation)
                    .HasPrecision(6, 2);

                entity.Property(e => e.LightIntensityDeviation)
                    .HasPrecision(6, 2);

                entity.Property(e => e.PhLevelDeviation)
                    .HasPrecision(6, 2);

                entity.HasIndex(e => e.TrayId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}