using System.ComponentModel.DataAnnotations;

namespace Temperature.Api.Models;

public record CreateReadingRequest(
    [Required] string? SensorId,
    [Required] string? Location,
    [Required] double? Value,
    string? SensorType,
    string? Unit,
    string? Status,
    string? Description
);
