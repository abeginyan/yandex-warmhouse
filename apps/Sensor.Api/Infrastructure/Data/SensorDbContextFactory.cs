using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sensor.Api.Infrastructure.Data;

public class SensorDbContextFactory : IDesignTimeDbContextFactory<SensorDbContext>
{
    public SensorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SensorDbContext>();
        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("DATABASE_URL") ??
            "Host=localhost;Port=5432;Database=smarthome;Username=postgres;Password=postgres");

        return new SensorDbContext(optionsBuilder.Options);
    }
}
