using System.ComponentModel.DataAnnotations;

namespace Temperature.Api.Models;

public record UpdateReadingRequest(
    [Required] double? Value,
    string? Location,
    string? SensorType,
    string? Unit,
    string? Status,
    string? Description
);
