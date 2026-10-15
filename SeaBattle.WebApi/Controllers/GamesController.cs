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
    private readonly IGameNotificationService _notifications;

    public GamesController(IGameService gameService, IShipPlacementService shipPlacementService, IGameNotificationService notifications)
    {
        _gameService = gameService;
        _shipPlacementService = shipPlacementService;
        _notifications = notifications;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGameRequest request)
    {
        var game = await _gameService.CreateGameAsync(GetUserId(), request);
        return Ok(new CreateGameResponse(game.Id, SeaBattle.Core.Domain.Enums.PlayerSide.Player1));
    }

    /// <summary>Подключение второго игрока к PvP-партии по коду (идентификатору) игры.</summary>
    [HttpPost("{gameId}/join")]
    public async Task<IActionResult> Join(Guid gameId)
    {
        var game = await _gameService.JoinGameAsync(gameId, GetUserId());
        await _notifications.PushStateAsync(game.Id);
        return Ok(new CreateGameResponse(game.Id, SeaBattle.Core.Domain.Enums.PlayerSide.Player2));
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
        await _notifications.PushStateAsync(gameId);
        return Ok();
    }

    [HttpPost("{gameId}/ships/random")]
    public async Task<IActionResult> PlaceRandom(Guid gameId)
    {
        await _shipPlacementService.PlaceShipsRandomlyAsync(gameId, GetUserId());
        await _notifications.PushStateAsync(gameId);
        return Ok();
    }

    [HttpPost("{gameId}/start")]
    public async Task<IActionResult> Start(Guid gameId)
    {
        await _gameService.StartGameAsync(gameId, GetUserId());
        await _notifications.PushStateAsync(gameId);
        return Ok();
    }

    /// <summary>Запрос паузы. В PvP — требует подтверждения соперника (см. accept-pause).</summary>
    [HttpPost("{gameId}/pause")]
    public async Task<IActionResult> Pause(Guid gameId)
    {
        var pause = await _gameService.RequestPauseAsync(gameId, GetUserId());
        if (pause.Accepted)
            await _notifications.NotifyPauseResolvedAsync(gameId, true);
        else
            await _notifications.NotifyPauseRequestedAsync(gameId,
                new PauseRequestDto(pause.Id, pause.RequestedBy, pause.CreatedAt, true, false));
        await _notifications.PushStateAsync(gameId);
        return Ok(new { requiresConfirmation = !pause.Accepted });
    }

    /// <summary>Подтверждение паузы вторым игроком.</summary>
    [HttpPost("{gameId}/accept-pause")]
    public async Task<IActionResult> AcceptPause(Guid gameId)
    {
        await _gameService.AcceptPauseAsync(gameId, GetUserId());
        await _notifications.NotifyPauseResolvedAsync(gameId, true);
        await _notifications.PushStateAsync(gameId);
        return Ok();
    }

    /// <summary>Отклонение запроса на паузу.</summary>
    [HttpPost("{gameId}/decline-pause")]
    public async Task<IActionResult> DeclinePause(Guid gameId)
    {
        await _gameService.DeclinePauseAsync(gameId, GetUserId());
        await _notifications.NotifyPauseResolvedAsync(gameId, false);
        await _notifications.PushStateAsync(gameId);
        return Ok();
    }

    [HttpPost("{gameId}/resume")]
    public async Task<IActionResult> Resume(Guid gameId)
    {
        await _gameService.ResumeGameAsync(gameId, GetUserId());
        await _notifications.NotifyPauseResolvedAsync(gameId, true);
        await _notifications.PushStateAsync(gameId);
        return Ok();
    }

    [HttpPost("{gameId}/stop")]
    public async Task<IActionResult> Stop(Guid gameId)
    {
        await _gameService.StopGameAsync(gameId, GetUserId());
        await _notifications.PushStateAsync(gameId);
        return Ok();
    }
}
