using Microsoft.EntityFrameworkCore;
using Temperature.Api.Models;

namespace Temperature.Api.Infrastructure.Data;

public interface ITemperatureRepository
{
    Task<TemperatureReading?> GetLatestBySensorIdAsync(string sensorId, CancellationToken ct = default);
    Task<TemperatureReading?> GetLatestByLocationAsync(string location, CancellationToken ct = default);
    Task<List<TemperatureReading>> GetAllAsync(CancellationToken ct = default);
    Task<TemperatureReading> CreateAsync(TemperatureReading reading, CancellationToken ct = default);
    Task<TemperatureReading?> UpdateLatestBySensorIdAsync(string sensorId, Action<TemperatureReading> apply, CancellationToken ct = default);
    Task<int> DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default);
}

public class TemperatureRepository(TemperatureDbContext context) : ITemperatureRepository
{
    public Task<TemperatureReading?> GetLatestBySensorIdAsync(string sensorId, CancellationToken ct = default) =>
        context.Readings
            .AsNoTracking()
            .Where(r => r.SensorId == sensorId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(ct);

    public Task<TemperatureReading?> GetLatestByLocationAsync(string location, CancellationToken ct = default) =>
        context.Readings
            .AsNoTracking()
            .Where(r => r.Location == location)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(ct);

    public Task<List<TemperatureReading>> GetAllAsync(CancellationToken ct = default) =>
        context.Readings
            .AsNoTracking()
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

    public async Task<TemperatureReading> CreateAsync(TemperatureReading reading, CancellationToken ct = default)
    {
        context.Readings.Add(reading);
        await context.SaveChangesAsync(ct);
        return reading;
    }

    public async Task<TemperatureReading?> UpdateLatestBySensorIdAsync(
        string sensorId, Action<TemperatureReading> apply, CancellationToken ct = default)
    {
        // Tracked query (no AsNoTracking) so EF picks up the mutation on SaveChanges.
        var reading = await context.Readings
            .Where(r => r.SensorId == sensorId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(ct);

        if (reading is null) return null;

        apply(reading);
        await context.SaveChangesAsync(ct);
        return reading;
    }

    public Task<int> DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default) =>
        context.Readings
            .Where(r => r.SensorId == sensorId)
            .ExecuteDeleteAsync(ct);
}
