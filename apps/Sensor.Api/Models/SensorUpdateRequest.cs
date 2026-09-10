namespace Sensor.Api.Models;

public record SensorUpdateRequest(
    string? Name,
    string? Type,
    string? Location,
    double? Value,
    string? Unit,
    string? Status
);
