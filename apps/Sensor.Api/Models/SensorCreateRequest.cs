using System.ComponentModel.DataAnnotations;

namespace Sensor.Api.Models;

public record SensorCreateRequest(
    [Required] string? Name,
    [Required] string? Type,
    string? Location,
    string? Unit
);
