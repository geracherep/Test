using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Application.Interfaces;

namespace SeaBattle.WebApi.Hubs;

/// <summary>
/// SignalR-хаб онлайн-режима «человек-человек».
/// Клиент вызывает методы (JoinGame, RequestPause и т.д.), сервер рассылает события
/// (GameState, ShotMade, PauseRequested, ...) — см. GameHub.Events.
/// </summary>
[Authorize]
public class GameHub : Hub
{
    public static class Events
    {
        public const string State = "GameState";
        public const string Shot = "ShotMade";
        public const string PauseRequested = "PauseRequested";
        public const string PauseResolved = "PauseResolved";
        public const string PlayerJoined = "PlayerJoined";
        public const string OpponentLeft = "OpponentLeft";
    }

    public static string GroupName(Guid gameId) => $"game:{gameId}";
    public static string SideGroupName(Guid gameId, int side) => $"game:{gameId}:side:{side}";

    private readonly IGameService _gameService;
    private readonly IMoveService _moveService;
    private readonly IShipPlacementService _placementService;
    private readonly IGameNotificationService _notifications;
    private readonly GameConnectionTracker _tracker;

    public GameHub(
        IGameService gameService,
        IMoveService moveService,
        IShipPlacementService placementService,
        IGameNotificationService notifications,
        GameConnectionTracker tracker)
    {
        _gameService = gameService;
        _moveService = moveService;
        _placementService = placementService;
        _notifications = notifications;
        _tracker = tracker;
    }

    private Guid UserId =>
        Guid.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("Не авторизован"));

    public override Task OnConnectedAsync()
    {
        _tracker.TrackConnection(Context.ConnectionId, UserId);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Игру запоминаем в Items подключения на этапе JoinGame
        if (Context.Items["gameId"] is Guid gameId && gameId != Guid.Empty)
        {
            var leftSides = _tracker.GetLeftSides(gameId, Context.ConnectionId);
            _tracker.UntrackConnection(Context.ConnectionId);
            foreach (var side in leftSides)
                await _notifications.NotifyOpponentLeftAsync(gameId, (SeaBattle.Core.Domain.Enums.PlayerSide)side);
            await _notifications.PushStateAsync(gameId); // соперник увидит «соперник не в сети»
        }
        else
        {
            _tracker.UntrackConnection(Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Вход игрока в комнату игры: проверяет участие (через GetStateAsync), подписывает на
    /// персональную группу стороны и общую группу матча, возвращает свежее состояние.
    /// </summary>
    public async Task<GameStateDto> JoinGame(Guid gameId)
    {
        var userId = UserId;
        GameStateDto state;
        try
        {
            state = await _gameService.GetStateAsync(gameId, userId);
        }
        catch
        {
            // Возможно, это PvP-игра по коду, где слот ещё свободен — пробуем подключиться как второй игрок
            var game = await _gameService.JoinGameAsync(gameId, userId);
            state = await _gameService.GetStateAsync(game.Id, userId);
        }

        var side = (int)state.MySide;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(gameId));
        await Groups.AddToGroupAsync(Context.ConnectionId, SideGroupName(gameId, side));
        Context.Items["gameId"] = gameId;
        _tracker.JoinGame(Context.ConnectionId, gameId, side);

        // Уведомляем соперника и синхронизируем обоих
        await _notifications.PushStateAsync(gameId);
        return state;
    }

    public async Task PlaceShipsRandomly(Guid gameId)
    {
        await _placementService.PlaceShipsRandomlyAsync(gameId, UserId);
        await _notifications.PushStateAsync(gameId);
    }

    public async Task PlaceShipsManually(Guid gameId, PlaceShipsRequest request)
    {
        await _placementService.PlaceShipsManuallyAsync(gameId, UserId, request);
        await _notifications.PushStateAsync(gameId);
    }

    public async Task StartGame(Guid gameId)
    {
        await _gameService.StartGameAsync(gameId, UserId);
        await _notifications.PushStateAsync(gameId);
    }

    public async Task MakeMove(Guid gameId, MakeMoveRequest request)
    {
        await _moveService.MakeMoveAsync(gameId, UserId, request); // сам разошлёт ShotMade + состояния
    }

    /// <summary>Запрос паузы: в PvP создаёт ожидающий подтверждения запрос, в одиночных режимах ставит сразу.</summary>
    public async Task RequestPause(Guid gameId)
    {
        var pause = await _gameService.RequestPauseAsync(gameId, UserId);
        if (pause.Accepted)
            await _notifications.NotifyPauseResolvedAsync(gameId, true);
        else
            await _notifications.NotifyPauseRequestedAsync(gameId,
                new PauseRequestDto(pause.Id, pause.RequestedBy, pause.CreatedAt, true, false));
        await _notifications.PushStateAsync(gameId);
    }

    /// <summary>Подтверждение паузы вторым игроком — только после него игра реально встаёт на паузу.</summary>
    public async Task AcceptPause(Guid gameId)
    {
        await _gameService.AcceptPauseAsync(gameId, UserId);
        await _notifications.NotifyPauseResolvedAsync(gameId, true);
        await _notifications.PushStateAsync(gameId);
    }

    public async Task DeclinePause(Guid gameId)
    {
        await _gameService.DeclinePauseAsync(gameId, UserId);
        await _notifications.NotifyPauseResolvedAsync(gameId, false);
        await _notifications.PushStateAsync(gameId);
    }

    public async Task ResumeGame(Guid gameId)
    {
        await _gameService.ResumeGameAsync(gameId, UserId);
        await _notifications.NotifyPauseResolvedAsync(gameId, true);
        await _notifications.PushStateAsync(gameId);
    }

    public async Task StopGame(Guid gameId)
    {
        await _gameService.StopGameAsync(gameId, UserId);
        await _notifications.PushStateAsync(gameId);
    }
}
