using Microsoft.EntityFrameworkCore;
using SensorEntity = global::Sensor.Api.Domain.Sensor;

namespace Sensor.Api.Infrastructure.Data;

public class SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : DbContext(options)
{
    public DbSet<SensorEntity> Sensors => Set<SensorEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorEntity>(entity =>
        {
            entity.ToTable("sensors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasColumnName("value").HasDefaultValue(0.0);
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue("inactive");
            entity.Property(e => e.LastUpdated).HasColumnName("last_updated");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Type).HasDatabaseName("idx_sensors_type");
            entity.HasIndex(e => e.Location).HasDatabaseName("idx_sensors_location");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_sensors_status");
        });
    }
}
