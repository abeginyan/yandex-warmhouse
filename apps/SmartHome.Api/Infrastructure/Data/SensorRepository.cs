using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Domain;

namespace SmartHome.Api.Infrastructure.Data;

public interface ISensorRepository
{
    Task<List<Sensor>> GetSensorsAsync(CancellationToken ct = default);
    Task<Sensor?> GetSensorByIdAsync(int id, CancellationToken ct = default);
    Task<Sensor> CreateSensorAsync(SensorCreateRequest request, CancellationToken ct = default);
    Task<Sensor?> UpdateSensorAsync(int id, SensorUpdateRequest request, CancellationToken ct = default);
    Task<bool> DeleteSensorAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateSensorValueAsync(int id, double value, string status, CancellationToken ct = default);
}

public class SensorRepository(SmartHomeDbContext context) : ISensorRepository
{
    public Task<List<Sensor>> GetSensorsAsync(CancellationToken ct = default) =>
        context.Sensors.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct);

    public Task<Sensor?> GetSensorByIdAsync(int id, CancellationToken ct = default) =>
        context.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Sensor> CreateSensorAsync(SensorCreateRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var sensor = new Sensor
        {
            Name = request.Name!,
            Type = request.Type!,
            Location = request.Location!,
            Unit = request.Unit,
            Status = "inactive",
            Value = 0.0,
            LastUpdated = now,
            CreatedAt = now
        };

        context.Sensors.Add(sensor);
        await context.SaveChangesAsync(ct);
        return sensor;
    }

    public async Task<Sensor?> UpdateSensorAsync(int id, SensorUpdateRequest request, CancellationToken ct = default)
    {
        var sensor = await context.Sensors.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sensor is null) return null;

        // Only update fields that were explicitly provided (non-empty for strings, non-null for others)
        // Matches Go's behavior: empty string fields are treated as "not provided"
        if (!string.IsNullOrEmpty(request.Name)) sensor.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Type)) sensor.Type = request.Type;
        if (!string.IsNullOrEmpty(request.Location)) sensor.Location = request.Location;
        if (request.Value.HasValue) sensor.Value = request.Value.Value;
        if (request.Unit is not null) sensor.Unit = request.Unit;
        if (!string.IsNullOrEmpty(request.Status)) sensor.Status = request.Status;
        sensor.LastUpdated = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return sensor;
    }

    public async Task<bool> DeleteSensorAsync(int id, CancellationToken ct = default)
    {
        var deleted = await context.Sensors
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public async Task<bool> UpdateSensorValueAsync(int id, double value, string status, CancellationToken ct = default)
    {
        var updated = await context.Sensors
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Value, value)
                    .SetProperty(e => e.Status, status)
                    .SetProperty(e => e.LastUpdated, DateTime.UtcNow),
                ct);
        return updated > 0;
    }
}
