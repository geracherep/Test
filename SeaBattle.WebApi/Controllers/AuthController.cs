using Microsoft.AspNetCore.Mvc;
using SeaBattle.Core.Application.DTOs.Auth;
using SeaBattle.Core.Application.Interfaces;

namespace SeaBattle.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;

    public AuthController(IAuthService authService, IJwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request);
        var token = _jwtService.GenerateToken(user);
        return Ok(new AuthResponse(user.Id, user.Username, user.Role.ToString(), token));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.LoginAsync(request);
        var token = _jwtService.GenerateToken(user);
        return Ok(new AuthResponse(user.Id, user.Username, user.Role.ToString(), token));
    }
}
