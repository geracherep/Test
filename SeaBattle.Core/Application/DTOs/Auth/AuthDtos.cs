namespace SeaBattle.Core.Application.DTOs.Auth;

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record AuthResponse(Guid UserId, string Username, string Role, string Token);
