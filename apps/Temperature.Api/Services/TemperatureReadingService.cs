using Temperature.Api.Infrastructure.Data;
using Temperature.Api.Models;

namespace Temperature.Api.Services;

public interface ITemperatureReadingService
{
    Task<TemperatureResponse?> GetByLocationAsync(string location, CancellationToken ct = default);
    Task<TemperatureResponse?> GetBySensorIdAsync(string sensorId, CancellationToken ct = default);
    Task<List<TemperatureResponse>> GetAllAsync(CancellationToken ct = default);
    Task<TemperatureResponse> CreateAsync(CreateReadingRequest request, CancellationToken ct = default);
    Task<TemperatureResponse> UpdateBySensorIdAsync(string sensorId, UpdateReadingRequest request, CancellationToken ct = default);
    Task<int> DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default);
}

public class TemperatureReadingService(ITemperatureRepository repo) : ITemperatureReadingService
{
    public async Task<TemperatureResponse?> GetByLocationAsync(string location, CancellationToken ct = default)
    {
        var reading = await repo.GetLatestByLocationAsync(location, ct);
        return reading is null ? null : Map(reading);
    }

    public async Task<TemperatureResponse?> GetBySensorIdAsync(string sensorId, CancellationToken ct = default)
    {
        var reading = await repo.GetLatestBySensorIdAsync(sensorId, ct);
        return reading is null ? null : Map(reading);
    }

    public async Task<List<TemperatureResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await repo.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<TemperatureResponse> CreateAsync(CreateReadingRequest request, CancellationToken ct = default)
    {
        var reading = new TemperatureReading
        {
            SensorId = request.SensorId!,
            SensorType = request.SensorType ?? "temperature",
            Location = request.Location!,
            Value = request.Value!.Value,
            Unit = request.Unit ?? "°C",
            Status = request.Status ?? "active",
            Description = request.Description ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };

        await repo.CreateAsync(reading, ct);
        return Map(reading);
    }

    public async Task<TemperatureResponse> UpdateBySensorIdAsync(
        string sensorId, UpdateReadingRequest request, CancellationToken ct = default)
    {
        var updated = await repo.UpdateLatestBySensorIdAsync(sensorId, reading =>
        {
            reading.Value = request.Value!.Value;
            if (request.Location is not null) reading.Location = request.Location;
            if (request.SensorType is not null) reading.SensorType = request.SensorType;
            if (request.Unit is not null) reading.Unit = request.Unit;
            if (request.Status is not null) reading.Status = request.Status;
            if (request.Description is not null) reading.Description = request.Description;
            reading.Timestamp = DateTime.UtcNow;
        }, ct);

        if (updated is not null)
            return Map(updated);

        // Upsert: no reading exists yet for this sensor (e.g. an earlier create-sync failed),
        // so create one from the update payload.
        var created = new TemperatureReading
        {
            SensorId = sensorId,
            SensorType = request.SensorType ?? "temperature",
            Location = request.Location ?? string.Empty,
            Value = request.Value!.Value,
            Unit = request.Unit ?? "°C",
            Status = request.Status ?? "active",
            Description = request.Description ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };

        await repo.CreateAsync(created, ct);
        return Map(created);
    }

    public Task<int> DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default) =>
        repo.DeleteBySensorIdAsync(sensorId, ct);

    private static TemperatureResponse Map(TemperatureReading r) => new(
        r.Value, r.Unit, r.Timestamp, r.Location,
        r.Status, r.SensorId, r.SensorType, r.Description);
}
