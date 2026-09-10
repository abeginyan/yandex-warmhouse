namespace User.Api.Models;

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string Username
);
