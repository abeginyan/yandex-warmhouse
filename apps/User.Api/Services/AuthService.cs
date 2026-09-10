using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using User.Api.Infrastructure.Data;
using User.Api.Models;
using UserEntity = global::User.Api.Models.User;

namespace User.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService(
    IUserRepository repo,
    IConfiguration config,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await repo.ExistsAsync(request.Username!, request.Email!, ct))
            throw new InvalidOperationException("Username or email already exists");

        var now = DateTime.UtcNow;
        var user = new UserEntity
        {
            Username = request.Username!,
            Email = request.Email!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
            CreatedAt = now,
            UpdatedAt = now
        };

        await repo.CreateAsync(user, ct);
        logger.LogInformation("User {Username} registered successfully", user.Username);

        return GenerateToken(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await repo.GetByUsernameAsync(request.Username!, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password!, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password");

        logger.LogInformation("User {Username} logged in successfully", user.Username);
        return GenerateToken(user);
    }

    private AuthResponse GenerateToken(UserEntity user)
    {
        var jwt = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));
        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiryMinutes"] ?? "60"));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiry,
            user.Username);
    }
}
