using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeaBattle.Core.Application.DTOs.Auth;
using SeaBattle.Core.Application.Interfaces;
using System.Security.Claims;

namespace SeaBattle.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _unitOfWork.Users.GetByIdWithProfileAsync(GetUserId());
        if (user == null || user.Profile == null) return NotFound();

        return Ok(new UserProfileDto(
            user.Id, user.Username, user.Role.ToString(),
            user.Profile.Experience, user.Profile.Rank,
            user.Profile.Wins, user.Profile.Losses,
            user.Profile.GamesPlayed, user.Profile.BestStreak, user.Profile.CurrentStreak));
    }
}
