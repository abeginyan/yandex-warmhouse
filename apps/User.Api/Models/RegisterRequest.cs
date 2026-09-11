using System.ComponentModel.DataAnnotations;

namespace User.Api.Models;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50)] string? Username,
    [Required, EmailAddress, MaxLength(255)] string? Email,
    [Required, MinLength(6), MaxLength(100)] string? Password
);
