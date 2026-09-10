namespace Sensor.Api.Models;

public record SensorCreateRequest(
    string? Name,
    string? Type,
    string? Location,
    string? Unit
);
