using SmartHome.Api.Domain;
using SmartHome.Api.Infrastructure.Data;
using SmartHome.Api.Infrastructure.Http;

namespace SmartHome.Api.Endpoints;

internal record TemperatureByLocationResponse(
    string Location,
    double Value,
    string Unit,
    string Status,
    DateTime Timestamp,
    string Description
);

// Marker class used as the ILogger<T> category for sensor endpoint logging
internal sealed class SensorEndpointLog { }

public static class SensorEndpoints
{
    public static void MapSensorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/sensors");

        group.MapGet("", GetSensors);
        // Register the literal "temperature" route before "/{id:int}" to ensure correct priority
        group.MapGet("/temperature/{location}", GetTemperatureByLocation);
        group.MapGet("/{id:int}", GetSensorById);
        group.MapPost("", CreateSensor);
        group.MapPut("/{id:int}", UpdateSensor);
        group.MapDelete("/{id:int}", DeleteSensor);
        group.MapPatch("/{id:int}/value", UpdateSensorValue);
    }

    // GET /api/v1/sensors
    static async Task<IResult> GetSensors(
        ISensorRepository repo,
        ITemperatureService tempService,
        ILogger<SensorEndpointLog> logger,
        CancellationToken ct)
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

        return Results.Ok(sensors);
    }

    // GET /api/v1/sensors/{id}
    static async Task<IResult> GetSensorById(
        int id,
        ISensorRepository repo,
        ITemperatureService tempService,
        ILogger<SensorEndpointLog> logger,
        CancellationToken ct)
    {
        var sensor = await repo.GetSensorByIdAsync(id, ct);
        if (sensor is null)
            return Results.NotFound(new { error = "Sensor not found" });

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

        return Results.Ok(sensor);
    }

    // GET /api/v1/sensors/temperature/{location}
    static async Task<IResult> GetTemperatureByLocation(
        string location,
        ITemperatureService tempService,
        ILogger<SensorEndpointLog> logger,
        CancellationToken ct)
    {
        TemperatureResponse tempData;
        try
        {
            tempData = await tempService.GetTemperatureAsync(location, ct);
        }
        catch (Exception ex)
        {
            return Results.Json(
                new { error = $"Failed to fetch temperature data: {ex.Message}" },
                statusCode: 500);
        }

        return Results.Ok(new TemperatureByLocationResponse(
            tempData.Location,
            tempData.Value,
            tempData.Unit,
            tempData.Status,
            tempData.Timestamp,
            tempData.Description));
    }

    // POST /api/v1/sensors
    static async Task<IResult> CreateSensor(
        SensorCreateRequest request,
        ISensorRepository repo,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Name) ||
            string.IsNullOrEmpty(request.Type) ||
            string.IsNullOrEmpty(request.Location))
        {
            return Results.BadRequest(new { error = "name, type, and location are required" });
        }

        try
        {
            var sensor = await repo.CreateSensorAsync(request, ct);
            return Results.Created($"/api/v1/sensors/{sensor.Id}", sensor);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 500);
        }
    }

    // PUT /api/v1/sensors/{id}
    static async Task<IResult> UpdateSensor(
        int id,
        SensorUpdateRequest request,
        ISensorRepository repo,
        CancellationToken ct)
    {
        try
        {
            var sensor = await repo.UpdateSensorAsync(id, request, ct);
            if (sensor is null)
                return Results.Json(new { error = "error getting sensor by ID: sensor not found" }, statusCode: 500);

            return Results.Ok(sensor);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 500);
        }
    }

    // DELETE /api/v1/sensors/{id}
    static async Task<IResult> DeleteSensor(
        int id,
        ISensorRepository repo,
        CancellationToken ct)
    {
        try
        {
            var deleted = await repo.DeleteSensorAsync(id, ct);
            if (!deleted)
                return Results.Json(new { error = "sensor not found" }, statusCode: 500);

            return Results.Ok(new { message = "Sensor deleted successfully" });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 500);
        }
    }

    // PATCH /api/v1/sensors/{id}/value
    static async Task<IResult> UpdateSensorValue(
        int id,
        SensorValueUpdateRequest request,
        ISensorRepository repo,
        CancellationToken ct)
    {
        if (request.Value is null || string.IsNullOrEmpty(request.Status))
            return Results.BadRequest(new { error = "value and status are required" });

        try
        {
            var updated = await repo.UpdateSensorValueAsync(id, request.Value.Value, request.Status, ct);
            if (!updated)
                return Results.Json(new { error = "sensor not found" }, statusCode: 500);

            return Results.Ok(new { message = "Sensor value updated successfully" });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 500);
        }
    }
}
