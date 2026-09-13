namespace Sensor.Api.Models;

public record TemperatureByLocationResponse(
    string Location,
    double Value,
    string Unit,
    string Status,
    DateTime Timestamp,
    string Description
);
