using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Temperature.Api.Infrastructure.Data;

public class TemperatureDbContextFactory : IDesignTimeDbContextFactory<TemperatureDbContext>
{
    public TemperatureDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TemperatureDbContext>();
        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("DATABASE_URL") ??
            "Host=localhost;Port=5432;Database=temperature;Username=postgres;Password=postgres");

        return new TemperatureDbContext(optionsBuilder.Options);
    }
}
