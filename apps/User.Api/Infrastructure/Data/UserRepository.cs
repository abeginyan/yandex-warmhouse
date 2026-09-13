using Microsoft.EntityFrameworkCore;
using User.Api.Models;
using UserEntity = global::User.Api.Models.User;

namespace User.Api.Infrastructure.Data;

public interface IUserRepository
{
    Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default);
    Task<UserEntity> CreateAsync(UserEntity user, CancellationToken ct = default);
}

public class UserRepository(UserDbContext context) : IUserRepository
{
    public Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default) =>
        context.Users.AnyAsync(u => u.Username == username || u.Email == email, ct);

    public async Task<UserEntity> CreateAsync(UserEntity user, CancellationToken ct = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
        return user;
    }
}
