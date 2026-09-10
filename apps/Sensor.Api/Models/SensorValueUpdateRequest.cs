namespace Sensor.Api.Models;

public record SensorValueUpdateRequest(
    double? Value,
    string? Status
);
