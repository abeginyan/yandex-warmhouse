using System.ComponentModel.DataAnnotations;

namespace User.Api.Models;

public record LoginRequest(
    [property: Required] string? Username,
    [property: Required] string? Password
);
