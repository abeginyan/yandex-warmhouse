using Sensor.Api.Infrastructure.Data;
using Sensor.Api.Infrastructure.Http;
using Sensor.Api.Models;
using SensorEntity = global::Sensor.Api.Models.Sensor;

namespace Sensor.Api.Services;

public interface ISensorService
{
    Task<List<SensorEntity>> GetSensorsAsync(CancellationToken ct = default);
    Task<SensorEntity?> GetSensorByIdAsync(int id, CancellationToken ct = default);
    Task<TemperatureResponse> GetTemperatureByLocationAsync(string location, CancellationToken ct = default);
    Task<SensorEntity> CreateSensorAsync(SensorCreateRequest request, CancellationToken ct = default);
    Task<SensorEntity?> UpdateSensorAsync(int id, SensorUpdateRequest request, CancellationToken ct = default);
    Task<bool> DeleteSensorAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateSensorValueAsync(int id, double value, string status, CancellationToken ct = default);
}

public class SensorService(
    ISensorRepository repo,
    ITemperatureService tempService,
    ILogger<SensorService> logger) : ISensorService
{
    // ── Read ────────────────────────────────────────────────────────────────

    public async Task<List<SensorEntity>> GetSensorsAsync(CancellationToken ct = default)
    {
        var sensors = await repo.GetSensorsAsync(ct);

        foreach (var sensor in sensors)
        {
            if (sensor.Type != "temperature") continue;
            try
            {
                var tempData = await tempService.GetTemperatureByIdAsync(sensor.Id.ToString(), ct);
                if (tempData is not null)
                {
                    sensor.Value = tempData.Value;
                    sensor.Status = tempData.Status;
                    sensor.LastUpdated = tempData.Timestamp;
                    logger.LogInformation("Updated temperature data for sensor {SensorId} from Temperature.Api", sensor.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("Failed to fetch temperature data for sensor {SensorId}: {Error}", sensor.Id, ex.Message);
            }
        }

        return sensors;
    }

    public async Task<SensorEntity?> GetSensorByIdAsync(int id, CancellationToken ct = default)
    {
        var sensor = await repo.GetSensorByIdAsync(id, ct);
        if (sensor is null) return null;

        if (sensor.Type == "temperature")
        {
            try
            {
                var tempData = await tempService.GetTemperatureByIdAsync(sensor.Id.ToString(), ct);
                if (tempData is not null)
                {
                    sensor.Value = tempData.Value;
                    sensor.Status = tempData.Status;
                    sensor.LastUpdated = tempData.Timestamp;
                    logger.LogInformation("Updated temperature data for sensor {SensorId} from Temperature.Api", sensor.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("Failed to fetch temperature data for sensor {SensorId}: {Error}", sensor.Id, ex.Message);
            }
        }

        return sensor;
    }

    public Task<TemperatureResponse> GetTemperatureByLocationAsync(string location, CancellationToken ct = default) =>
        tempService.GetTemperatureAsync(location, ct);

    // ── Create ──────────────────────────────────────────────────────────────

    public async Task<SensorEntity> CreateSensorAsync(SensorCreateRequest request, CancellationToken ct = default)
    {
        var sensor = await repo.CreateSensorAsync(request, ct);

        if (sensor.Type == "temperature")
            await SyncReadingAsync(sensor, sensor.Value, sensor.Status, ct);

        return sensor;
    }

    // ── Update metadata (PUT) ───────────────────────────────────────────────

    public async Task<SensorEntity?> UpdateSensorAsync(int id, SensorUpdateRequest request, CancellationToken ct = default)
    {
        var sensor = await repo.UpdateSensorAsync(id, request, ct);

        if (sensor is not null && sensor.Type == "temperature" && request.Value.HasValue)
            await SyncUpdateAsync(sensor, sensor.Value, sensor.Status, ct);

        return sensor;
    }

    // ── Update value only (PATCH) ───────────────────────────────────────────

    public async Task<bool> UpdateSensorValueAsync(int id, double value, string status, CancellationToken ct = default)
    {
        // Fetch the sensor first so we have type / location / unit for the temperature reading
        var sensor = await repo.GetSensorByIdAsync(id, ct);

        var updated = await repo.UpdateSensorValueAsync(id, value, status, ct);

        if (updated && sensor?.Type == "temperature")
            await SyncUpdateAsync(sensor, value, status, ct);

        return updated;
    }

    // ── Delete ──────────────────────────────────────────────────────────────

    public async Task<bool> DeleteSensorAsync(int id, CancellationToken ct = default)
    {
        // Best-effort: remove all temperature readings for this sensor before deleting it
        try
        {
            await tempService.DeleteBySensorIdAsync(id.ToString(), ct);
            logger.LogInformation("Deleted temperature readings for sensor {SensorId}", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to delete temperature readings for sensor {SensorId}: {Error}", id, ex.Message);
        }

        return await repo.DeleteSensorAsync(id, ct);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task SyncReadingAsync(SensorEntity sensor, double value, string status, CancellationToken ct)
    {
        try
        {
            await tempService.CreateReadingAsync(new CreateTemperatureReadingRequest(
                SensorId: sensor.Id.ToString(),
                Location: sensor.Location,
                Value: value,
                SensorType: sensor.Type,
                Unit: sensor.Unit ?? "°C",
                Status: status,
                Description: sensor.Name), ct);

            logger.LogInformation("Synced temperature reading for sensor {SensorId} to Temperature.Api", sensor.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to sync temperature reading for sensor {SensorId}: {Error}", sensor.Id, ex.Message);
        }
    }

    private async Task SyncUpdateAsync(SensorEntity sensor, double value, string status, CancellationToken ct)
    {
        try
        {
            await tempService.UpdateReadingAsync(sensor.Id.ToString(), new UpdateTemperatureReadingRequest(
                Value: value,
                Location: sensor.Location,
                SensorType: sensor.Type,
                Unit: sensor.Unit ?? "°C",
                Status: status,
                Description: sensor.Name), ct);

            logger.LogInformation("Synced temperature update for sensor {SensorId} to Temperature.Api", sensor.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to sync temperature update for sensor {SensorId}: {Error}", sensor.Id, ex.Message);
        }
    }
}
