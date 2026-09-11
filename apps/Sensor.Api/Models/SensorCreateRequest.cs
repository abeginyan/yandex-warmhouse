using System.ComponentModel.DataAnnotations;

namespace Sensor.Api.Models;

public record SensorCreateRequest(
    [Required] string? Name,
    [Required] string? Type,
    [Required] string? Location,
    string? Unit
);
