using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Application.Interfaces;
using System.Security.Claims;

namespace SeaBattle.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovesController : ControllerBase
{
    private readonly IMoveService _moveService;

    public MovesController(IMoveService moveService)
    {
        _moveService = moveService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{gameId}")]
    public async Task<IActionResult> MakeMove(Guid gameId, [FromBody] MakeMoveRequest request)
    {
        var result = await _moveService.MakeMoveAsync(gameId, GetUserId(), request);
        return Ok(result);
    }

    [HttpPost("{gameId}/ai")]
    public async Task<IActionResult> AiMove(Guid gameId)
    {
        var result = await _moveService.MakeAiMoveAsync(gameId);
        return Ok(result);
    }
}
