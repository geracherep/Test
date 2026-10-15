using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Application.Interfaces;
using System.Security.Claims;

namespace SeaBattle.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IShipPlacementService _shipPlacementService;

    public GamesController(IGameService gameService, IShipPlacementService shipPlacementService)
    {
        _gameService = gameService;
        _shipPlacementService = shipPlacementService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGameRequest request)
    {
        var game = await _gameService.CreateGameAsync(GetUserId(), request);
        return Ok(game.Id);
    }

    [HttpGet("{gameId}/state")]
    public async Task<IActionResult> GetState(Guid gameId)
    {
        return Ok(await _gameService.GetStateAsync(gameId, GetUserId()));
    }

    [HttpPost("{gameId}/ships/manual")]
    public async Task<IActionResult> PlaceManual(Guid gameId, [FromBody] PlaceShipsRequest request)
    {
        await _shipPlacementService.PlaceShipsManuallyAsync(gameId, GetUserId(), request);
        return Ok();
    }

    [HttpPost("{gameId}/ships/random")]
    public async Task<IActionResult> PlaceRandom(Guid gameId)
    {
        await _shipPlacementService.PlaceShipsRandomlyAsync(gameId, GetUserId());
        return Ok();
    }

    [HttpPost("{gameId}/start")]
    public async Task<IActionResult> Start(Guid gameId)
    {
        await _gameService.StartGameAsync(gameId, GetUserId());
        return Ok();
    }

    [HttpPost("{gameId}/pause")]
    public async Task<IActionResult> Pause(Guid gameId)
    {
        await _gameService.RequestPauseAsync(gameId, GetUserId());
        return Ok();
    }

    [HttpPost("{gameId}/resume")]
    public async Task<IActionResult> Resume(Guid gameId)
    {
        await _gameService.ResumeGameAsync(gameId, GetUserId());
        return Ok();
    }

    [HttpPost("{gameId}/stop")]
    public async Task<IActionResult> Stop(Guid gameId)
    {
        await _gameService.StopGameAsync(gameId, GetUserId());
        return Ok();
    }
}
