namespace Temperature.Api.Models;

// Shape must match Sensor.Api's TemperatureResponse exactly (snake_case JSON).
public record TemperatureResponse(
    double Value,
    string Unit,
    DateTime Timestamp,
    string Location,
    string Status,
    string SensorId,
    string SensorType,
    string Description
);
