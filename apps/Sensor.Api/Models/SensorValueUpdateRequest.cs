using System.ComponentModel.DataAnnotations;

namespace Sensor.Api.Models;

public record SensorValueUpdateRequest(
    [property: Required] double? Value,
    [property: Required] string? Status
);
