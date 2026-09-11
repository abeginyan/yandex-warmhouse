using System.ComponentModel.DataAnnotations;

namespace Sensor.Api.Models;

public record SensorValueUpdateRequest(
    [Required] double? Value,
    [Required] string? Status
);
