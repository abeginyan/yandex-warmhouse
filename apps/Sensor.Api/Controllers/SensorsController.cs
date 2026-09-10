using Microsoft.AspNetCore.Mvc;
using Sensor.Api.Models;
using Sensor.Api.Services;

namespace Sensor.Api.Controllers;

[ApiController]
[Route("api/v1/sensors")]
public class SensorsController(ISensorService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSensors(CancellationToken ct) =>
        Ok(await service.GetSensorsAsync(ct));

    [HttpGet("temperature/{location}")]
    public async Task<IActionResult> GetTemperatureByLocation(string location, CancellationToken ct)
    {
        try
        {
            var tempData = await service.GetTemperatureByLocationAsync(location, ct);
            return Ok(new TemperatureByLocationResponse(
                tempData.Location,
                tempData.Value,
                tempData.Unit,
                tempData.Status,
                tempData.Timestamp,
                tempData.Description));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to fetch temperature data: {ex.Message}" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSensorById(int id, CancellationToken ct)
    {
        var sensor = await service.GetSensorByIdAsync(id, ct);
        return sensor is null
            ? NotFound(new { error = "Sensor not found" })
            : Ok(sensor);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSensor([FromBody] SensorCreateRequest request, CancellationToken ct)
    {
        try
        {
            var sensor = await service.CreateSensorAsync(request, ct);
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
            var sensor = await service.UpdateSensorAsync(id, request, ct);
            return sensor is null
                ? StatusCode(500, new { error = "error getting sensor by ID: sensor not found" })
                : Ok(sensor);
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
            var deleted = await service.DeleteSensorAsync(id, ct);
            return deleted
                ? Ok(new { message = "Sensor deleted successfully" })
                : StatusCode(500, new { error = "sensor not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("{id:int}/value")]
    public async Task<IActionResult> UpdateSensorValue(int id, [FromBody] SensorValueUpdateRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await service.UpdateSensorValueAsync(id, request.Value!.Value, request.Status!, ct);
            return updated
                ? Ok(new { message = "Sensor value updated successfully" })
                : StatusCode(500, new { error = "sensor not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
