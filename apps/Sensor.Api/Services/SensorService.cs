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
                    logger.LogInformation("Updated temperature data for sensor {SensorId} from external API", sensor.Id);
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
                    logger.LogInformation("Updated temperature data for sensor {SensorId} from external API", sensor.Id);
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

    public Task<SensorEntity> CreateSensorAsync(SensorCreateRequest request, CancellationToken ct = default) =>
        repo.CreateSensorAsync(request, ct);

    public Task<SensorEntity?> UpdateSensorAsync(int id, SensorUpdateRequest request, CancellationToken ct = default) =>
        repo.UpdateSensorAsync(id, request, ct);

    public Task<bool> DeleteSensorAsync(int id, CancellationToken ct = default) =>
        repo.DeleteSensorAsync(id, ct);

    public Task<bool> UpdateSensorValueAsync(int id, double value, string status, CancellationToken ct = default) =>
        repo.UpdateSensorValueAsync(id, value, status, ct);
}
