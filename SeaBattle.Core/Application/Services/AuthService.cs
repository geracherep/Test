using SeaBattle.Core.Application.DTOs.Auth;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;
using SeaBattle.Core.Domain.Exceptions;

namespace SeaBattle.Core.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
        if (existingUser != null)
            throw new DomainException("Пользователь с таким именем уже существует");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = UserRole.User,
            Profile = new UserProfile { Id = Guid.NewGuid(), Rank = Rank.Recruit }
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return user;
    }

    public async Task<User> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new DomainException("Неверный логин или пароль");

        return user;
    }
}
