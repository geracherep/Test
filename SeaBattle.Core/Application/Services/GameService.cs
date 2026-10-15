using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;
using SeaBattle.Core.Domain.Exceptions;

namespace SeaBattle.Core.Application.Services;

public class GameService : IGameService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShipPlacementService _shipPlacementService;
    private readonly IOnlinePresenceTracker _presence;
    private readonly IGameNotificationService? _notifications;

    public GameService(
        IUnitOfWork unitOfWork,
        IShipPlacementService shipPlacementService,
        IOnlinePresenceTracker presence,
        IGameNotificationService? notifications = null)
    {
        _unitOfWork = unitOfWork;
        _shipPlacementService = shipPlacementService;
        _presence = presence;
        _notifications = notifications;
    }

    public async Task<Game> CreateGameAsync(Guid userId, CreateGameRequest request)
    {
        var game = new Game { Id = Guid.NewGuid(), Mode = request.Mode, Status = GameStatus.WaitingForShips };

        if (request.Mode == GameMode.ComputerVsComputer)
        {
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, UserId = userId, Side = PlayerSide.Player1, IsComputer = true, AiStrategy = request.AiStrategy ?? AiStrategyType.Random });
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, Side = PlayerSide.Player2, IsComputer = true, AiStrategy = request.AiStrategy ?? AiStrategyType.Random });
            await _unitOfWork.Games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();
            await _shipPlacementService.PlaceShipsRandomlyAsync(game.Id, Guid.Empty);
            await _shipPlacementService.PlaceShipsRandomlyAsync(game.Id, Guid.Empty);
        }
        else if (request.Mode == GameMode.HumanVsComputer)
        {
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, UserId = userId, Side = PlayerSide.Player1 });
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, Side = PlayerSide.Player2, IsComputer = true, AiStrategy = request.AiStrategy ?? AiStrategyType.Random });
            await _unitOfWork.Games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();
            await _shipPlacementService.PlaceShipsRandomlyAsync(game.Id, Guid.Empty);
        }
        else // HumanVsHuman: второй игрок подключится позже по коду игры
        {
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, UserId = userId, Side = PlayerSide.Player1 });
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, Side = PlayerSide.Player2 });
            await _unitOfWork.Games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();
        }

        return game;
    }

    /// <summary>
    /// Подключение второго живого игрока к PvP-партии. Первый игрок всегда Player1.
    /// </summary>
    public async Task<Game> JoinGameAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId)
            ?? throw new DomainException("Игра не найдена");

        if (game.Mode != GameMode.HumanVsHuman)
            throw new DomainException("В эту игру нельзя подключиться (не PvP-режим)");

        var me = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (me != null) return game; // уже участвует — повторный вход

        var host = game.Players.First(p => p.Side == PlayerSide.Player1);
        if (host.UserId == null)
            throw new DomainException("Место первого игрока занято");

        var guest = game.Players.First(p => p.Side == PlayerSide.Player2);
        if (guest.UserId != null && guest.UserId != userId)
            throw new DomainException("Игра уже заполнена");

        if (game.Status != GameStatus.WaitingForShips)
            throw new DomainException("Игра уже началась");

        // Освобождаем слот хоста и занимаем гостевой
        host.UserId = null;
        guest.UserId = userId;
        await _unitOfWork.SaveChangesAsync();

        if (_notifications != null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            await _notifications.NotifyPlayerJoinedAsync(game.Id,
                new PlayerInfoDto(userId, user?.Username ?? "?", guest.Side, false, true));
        }

        return game;
    }

    public async Task<Game> StartGameAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) ?? throw new DomainException("Игра не найдена");
        if (game.Status != GameStatus.WaitingForShips && game.Status != GameStatus.ReadyToStart)
            throw new DomainException("Игру нельзя начать в текущем статусе");

        if (game.Mode == GameMode.HumanVsHuman)
        {
            var occupied = game.Players.Count(p => p.UserId != null);
            if (occupied < 2)
                throw new DomainException("Ожидание второго игрока: поделитесь кодом игры");
        }

        if (!game.Players.All(p => p.ShipsPlaced))
            throw new DomainException("Не все игроки расставили корабли");

        game.Status = GameStatus.InProgress;
        game.CurrentTurn = PlayerSide.Player1;
        game.StartedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return game;
    }

    public async Task<GameStateDto> GetStateAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithShipsAsync(gameId) ?? throw new DomainException("Игра не найдена");

        var me = game.Players.FirstOrDefault(p => p.UserId == userId);
        var mySide = me?.Side ?? PlayerSide.Player1;
        var enemySide = mySide == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;

        var myShips = game.Ships.Where(s => s.OwnerSide == mySide)
            .Select(s => new ShipDto(s.Size, s.IsSunk, s.Cells.Select(c => new ShipCellDto(c.Row, c.Column, c.IsHit)).ToList()))
            .ToList();

        var enemySunk = game.Ships.Where(s => s.OwnerSide == enemySide && s.IsSunk)
            .Select(s => new ShipDto(s.Size, s.IsSunk, s.Cells.Select(c => new ShipCellDto(c.Row, c.Column, c.IsHit)).ToList()))
            .ToList();

        var moves = await _unitOfWork.Moves.FindAsync(m => m.GameId == gameId);
        var myMoves = moves.Where(m => m.PlayerSide == mySide).Select(m => new MoveDto(m.Row, m.Column, m.Result)).ToList();
        var enemyMoves = moves.Where(m => m.PlayerSide == enemySide).Select(m => new MoveDto(m.Row, m.Column, m.Result)).ToList();

        var players = game.Players
            .Where(p => p.UserId != null)
            .Select(p => new PlayerInfoDto(
                p.UserId!,
                p.User?.Username ?? "?",
                p.Side,
                p.ShipsPlaced,
                _presence.IsOnline(p.UserId!.Value)))
            .ToList();

        var pendingPause = await GetPendingPauseAsync(game);

        return new GameStateDto(game.Id, game.Mode, game.Status, game.CurrentTurn, game.Winner, mySide,
            myShips, enemySunk, myMoves, enemyMoves, players, pendingPause);
    }

    /// <summary>
    /// Запрос паузы. В режиме «человек-человек» игра ставится на паузу только после
    /// подтверждения вторым игроком (сущность PauseRequest). В одиночных режимах — сразу.
    /// </summary>
    public async Task<PauseRequest> RequestPauseAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) ?? throw new DomainException("Игра не найдена");
        if (game.Status != GameStatus.InProgress) throw new GameNotActiveException();

        var me = game.Players.FirstOrDefault(p => p.UserId == userId)
            ?? throw new DomainException("Вы не участвуете в этой игре");

        // Уже есть незакрытый запрос от этого игрока — возвращаем его (идемпотентность)
        var existing = await FindPendingPauseAsync(gameId);
        if (existing != null) return existing;

        var isPvp = game.Mode == GameMode.HumanVsHuman && game.Players.Count(p => p.UserId != null) >= 2;

        var request = new PauseRequest
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            RequestedBy = me.Side,
            Accepted = !isPvp, // вне PvP пауза подтверждена сразу
            CreatedAt = DateTime.UtcNow
        };

        if (isPvp)
        {
            // Статус НЕ меняем — ждём подтверждения соперника
            await _unitOfWork.PauseRequests.AddAsync(request);
        }
        else
        {
            game.Status = GameStatus.Paused;
            // Паузу одиночной игры тоже фиксируем записью, чтобы Resume снял её корректно
            await _unitOfWork.PauseRequests.AddAsync(request);
        }

        await _unitOfWork.SaveChangesAsync();
        return request;
    }

    /// <summary>Подтверждение паузы вторым игроком. С этого момента игра на паузе.</summary>
    public async Task AcceptPauseAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) ?? throw new DomainException("Игра не найдена");
        if (game.Status != GameStatus.InProgress)
            throw new DomainException("Нет активного запроса на паузу");

        var me = game.Players.FirstOrDefault(p => p.UserId == userId)
            ?? throw new DomainException("Вы не участвуете в этой игре");

        var pending = await FindPendingPauseAsync(gameId)
            ?? throw new DomainException("Нет активного запроса на паузу");

        if (pending.RequestedBy == me.Side)
            throw new DomainException("Нельзя подтвердить собственный запрос паузы");

        pending.Accepted = true;
        game.Status = GameStatus.Paused;
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Отклонение запроса: игра продолжается без паузы.</summary>
    public async Task DeclinePauseAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) ?? throw new DomainException("Игра не найдена");

        var me = game.Players.FirstOrDefault(p => p.UserId == userId)
            ?? throw new DomainException("Вы не участвуете в этой игре");

        var pending = await FindPendingPauseAsync(gameId)
            ?? throw new DomainException("Нет активного запроса на паузу");

        if (pending.RequestedBy == me.Side)
            throw new DomainException("Нельзя отклонить собственный запрос паузы");

        _unitOfWork.PauseRequests.Remove(pending);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResumeGameAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdAsync(gameId) ?? throw new DomainException("Игра не найдена");
        if (game.Status != GameStatus.Paused) throw new DomainException("Игра не на паузе");

        // Закрываем все подтверждённые записи паузы этого матча
        var pauses = await _unitOfWork.PauseRequests.FindAsync(p => p.GameId == gameId && p.Accepted);
        foreach (var p in pauses) _unitOfWork.PauseRequests.Remove(p);

        game.Status = GameStatus.InProgress;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task StopGameAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdAsync(gameId) ?? throw new DomainException("Игра не найдена");
        game.Status = GameStatus.Aborted;
        game.FinishedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<PauseRequest?> FindPendingPauseAsync(Guid gameId)
    {
        var requests = await _unitOfWork.PauseRequests.FindAsync(p => p.GameId == gameId && !p.Accepted);
        return requests.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
    }

    private async Task<PauseRequestDto?> GetPendingPauseAsync(Game game)
    {
        if (game.Status == GameStatus.Paused)
        {
            var accepted = (await _unitOfWork.PauseRequests.FindAsync(p => p.GameId == game.Id && p.Accepted))
                .OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            if (accepted != null)
                return new PauseRequestDto(accepted.Id, accepted.RequestedBy, accepted.CreatedAt, false, true);
        }

        var pending = await FindPendingPauseAsync(game.Id);
        if (pending == null) return null;

        var requiresConfirmation = game.Mode == GameMode.HumanVsHuman && game.Status == GameStatus.InProgress;
        return new PauseRequestDto(pending.Id, pending.RequestedBy, pending.CreatedAt, requiresConfirmation, pending.Accepted);
    }
}
