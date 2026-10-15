using SeaBattle.Core.Application.DTOs.Auth;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Core.Application.Interfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterRequest request);
    Task<User> LoginAsync(LoginRequest request);
}

public interface IJwtService
{
    string GenerateToken(User user);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
