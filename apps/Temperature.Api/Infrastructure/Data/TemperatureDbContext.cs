using Microsoft.EntityFrameworkCore;
using Temperature.Api.Models;

namespace Temperature.Api.Infrastructure.Data;

public class TemperatureDbContext(DbContextOptions<TemperatureDbContext> options) : DbContext(options)
{
    public DbSet<TemperatureReading> Readings => Set<TemperatureReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TemperatureReading>(entity =>
        {
            entity.ToTable("temperature_readings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SensorId).HasColumnName("sensor_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SensorType).HasColumnName("sensor_type").HasMaxLength(50).IsRequired().HasDefaultValue("temperature");
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasColumnName("value").IsRequired();
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired().HasDefaultValue("°C");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue("active");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255).IsRequired().HasDefaultValue("");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");

            entity.HasIndex(e => e.SensorId).HasDatabaseName("idx_temperature_readings_sensor_id");
            entity.HasIndex(e => e.Location).HasDatabaseName("idx_temperature_readings_location");
        });
    }
}
