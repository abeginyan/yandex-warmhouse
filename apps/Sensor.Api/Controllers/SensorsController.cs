using Microsoft.AspNetCore.Mvc;
using Sensor.Api.Infrastructure.Data;
using Sensor.Api.Infrastructure.Http;
using Sensor.Api.Models;

namespace Sensor.Api.Controllers;

[ApiController]
[Route("api/v1/sensors")]
public class SensorsController(
    ISensorRepository repo,
    ITemperatureService tempService,
    ILogger<SensorsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSensors(CancellationToken ct)
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

        return Ok(sensors);
    }

    [HttpGet("temperature/{location}")]
    public async Task<IActionResult> GetTemperatureByLocation(string location, CancellationToken ct)
    {
        TemperatureResponse tempData;
        try
        {
            tempData = await tempService.GetTemperatureAsync(location, ct);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to fetch temperature data: {ex.Message}" });
        }

        return Ok(new TemperatureByLocationResponse(
            tempData.Location,
            tempData.Value,
            tempData.Unit,
            tempData.Status,
            tempData.Timestamp,
            tempData.Description));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSensorById(int id, CancellationToken ct)
    {
        var sensor = await repo.GetSensorByIdAsync(id, ct);
        if (sensor is null)
            return NotFound(new { error = "Sensor not found" });

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

        return Ok(sensor);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSensor([FromBody] SensorCreateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Name) ||
            string.IsNullOrEmpty(request.Type) ||
            string.IsNullOrEmpty(request.Location))
        {
            return BadRequest(new { error = "name, type, and location are required" });
        }

        try
        {
            var sensor = await repo.CreateSensorAsync(request, ct);
            return Created($"/api/v1/sensors/{sensor.Id}", sensor);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSensor(int id, [FromBody] SensorUpdateRequest request, CancellationToken ct)
    {
        try
        {
            var sensor = await repo.UpdateSensorAsync(id, request, ct);
            if (sensor is null)
                return StatusCode(500, new { error = "error getting sensor by ID: sensor not found" });

            return Ok(sensor);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSensor(int id, CancellationToken ct)
    {
        try
        {
            var deleted = await repo.DeleteSensorAsync(id, ct);
            if (!deleted)
                return StatusCode(500, new { error = "sensor not found" });

            return Ok(new { message = "Sensor deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("{id:int}/value")]
    public async Task<IActionResult> UpdateSensorValue(int id, [FromBody] SensorValueUpdateRequest request, CancellationToken ct)
    {
        if (request.Value is null || string.IsNullOrEmpty(request.Status))
            return BadRequest(new { error = "value and status are required" });

        try
        {
            var updated = await repo.UpdateSensorValueAsync(id, request.Value.Value, request.Status, ct);
            if (!updated)
                return StatusCode(500, new { error = "sensor not found" });

            return Ok(new { message = "Sensor value updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
