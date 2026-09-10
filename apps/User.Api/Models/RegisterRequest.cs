using System.ComponentModel.DataAnnotations;

namespace User.Api.Models;

public record RegisterRequest(
    [property: Required, MinLength(3), MaxLength(50)] string? Username,
    [property: Required, EmailAddress, MaxLength(255)] string? Email,
    [property: Required, MinLength(6), MaxLength(100)] string? Password
);
