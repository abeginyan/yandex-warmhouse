using System.ComponentModel.DataAnnotations;

namespace Sensor.Api.Models;

public record SensorCreateRequest(
    [property: Required] string? Name,
    [property: Required] string? Type,
    [property: Required] string? Location,
    string? Unit
);
