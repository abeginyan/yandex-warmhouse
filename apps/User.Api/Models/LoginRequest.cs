using System.ComponentModel.DataAnnotations;

namespace User.Api.Models;

public record LoginRequest(
    [Required] string? Username,
    [Required] string? Password
);
