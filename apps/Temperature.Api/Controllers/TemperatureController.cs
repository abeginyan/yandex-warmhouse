using Microsoft.AspNetCore.Mvc;
using Temperature.Api.Models;
using Temperature.Api.Services;

namespace Temperature.Api.Controllers;

[ApiController]
[Route("temperature")]
public class TemperatureController(ITemperatureReadingService service) : ControllerBase
{
    // GET /temperature?location={location}
    // Called by Sensor.Api to enrich sensors of type "temperature" by location.
    [HttpGet]
    public async Task<IActionResult> GetByLocation([FromQuery] string? location, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(location))
            return BadRequest(new { error = "location query parameter is required" });

        var response = await service.GetByLocationAsync(location, ct);
        return response is null
            ? NotFound(new { error = $"No reading found for location: {location}" })
            : Ok(response);
    }

    // GET /temperature/all — registered before /{sensorId} so "all" is not captured as a sensor ID
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    // GET /temperature/{sensorId}
    // Called by Sensor.Api to enrich a specific sensor by its ID.
    [HttpGet("{sensorId}")]
    public async Task<IActionResult> GetBySensorId(string sensorId, CancellationToken ct)
    {
        var response = await service.GetBySensorIdAsync(sensorId, ct);
        return response is null
            ? NotFound(new { error = $"No reading found for sensor: {sensorId}" })
            : Ok(response);
    }

    // POST /temperature
    // Called by Sensor.Api on sensor create / value update, or pushed directly by a device.
    [HttpPost]
    public async Task<IActionResult> CreateReading([FromBody] CreateReadingRequest request, CancellationToken ct)
    {
        try
        {
            var response = await service.CreateAsync(request, ct);
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // PUT /temperature/{sensorId}
    // Called by Sensor.Api when a temperature sensor is updated — updates the sensor's latest
    // reading in place (creating one if none exists yet).
    [HttpPut("{sensorId}")]
    public async Task<IActionResult> UpdateBySensorId(string sensorId, [FromBody] UpdateReadingRequest request, CancellationToken ct)
    {
        try
        {
            var response = await service.UpdateBySensorIdAsync(sensorId, request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // DELETE /temperature/{sensorId}
    // Called by Sensor.Api when a sensor is deleted — removes all readings for that sensor.
    [HttpDelete("{sensorId}")]
    public async Task<IActionResult> DeleteBySensorId(string sensorId, CancellationToken ct)
    {
        var deleted = await service.DeleteBySensorIdAsync(sensorId, ct);
        return Ok(new { deleted });
    }
}
