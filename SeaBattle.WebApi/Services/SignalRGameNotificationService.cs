using Microsoft.AspNetCore.SignalR;
using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;
using SeaBattle.WebApi.Hubs;

namespace SeaBattle.WebApi.Services;

/// <summary>
/// Реализация контракта рассылки Core-сервисов поверх SignalR.
/// Scoped, потому что зависит от IGameService (scoped) и создаёт scope для персонализированных состояний.
/// </summary>
public class SignalRGameNotificationService : IGameNotificationService
{
    private readonly IHubContext<GameHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GameConnectionTracker _tracker;

    public SignalRGameNotificationService(
        IHubContext<GameHub> hub,
        IServiceScopeFactory scopeFactory,
        GameConnectionTracker tracker)
    {
        _hub = hub;
        _scopeFactory = scopeFactory;
        _tracker = tracker;
    }

    public Task NotifyShotAsync(Guid gameId, MoveResultDto result, PlayerSide shooterSide) =>
        _hub.Clients.Group(GameHub.GroupName(gameId)).SendAsync(GameHub.Events.Shot, new { shooterSide, result });

    public Task NotifyPauseRequestedAsync(Guid gameId, PauseRequestDto pause) =>
        _hub.Clients.Group(GameHub.GroupName(gameId)).SendAsync(GameHub.Events.PauseRequested, pause);

    public Task NotifyPauseResolvedAsync(Guid gameId, bool accepted) =>
        _hub.Clients.Group(GameHub.GroupName(gameId)).SendAsync(GameHub.Events.PauseResolved, new { accepted });

    public Task NotifyPlayerJoinedAsync(Guid gameId, PlayerInfoDto player) =>
        _hub.Clients.Group(GameHub.GroupName(gameId)).SendAsync(GameHub.Events.PlayerJoined, player);

    public Task NotifyOpponentLeftAsync(Guid gameId, PlayerSide side) =>
        _hub.Clients.Group(GameHub.GroupName(gameId)).SendAsync(GameHub.Events.OpponentLeft, new { side });

    public async Task PushStateAsync(Guid gameId)
    {
        foreach (var side in _tracker.GetConnectedSides(gameId))
            await PushStateForSideAsync(gameId, (PlayerSide)side);
    }

    public Task PushStatesAsync(Game game) => PushStateAsync(game.Id);

    /// <summary>
    /// Персонализированное состояние: каждое подключение получает свой GameStateDto
    /// (со своей стороны поля «мои корабли»/«потопленные корабли противника»).
    /// </summary>
    private async Task PushStateForSideAsync(Guid gameId, PlayerSide side)
    {
        using var scope = _scopeFactory.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var game = await unitOfWork.Games.GetByIdWithPlayersAsync(gameId);
        var userId = game?.Players.FirstOrDefault(p => p.Side == side)?.UserId;
        if (userId == null) return;

        try
        {
            var state = await gameService.GetStateAsync(gameId, userId.Value);
            await _hub.Clients.Group(GameHub.SideGroupName(gameId, (int)side))
                .SendAsync(GameHub.Events.State, state);
        }
        catch
        {
            // Состояние уже могло измениться между запросом и отправкой — не роняем процесс
        }
    }
}
