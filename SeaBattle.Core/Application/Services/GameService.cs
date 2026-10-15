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

    public GameService(IUnitOfWork unitOfWork, IShipPlacementService shipPlacementService)
    {
        _unitOfWork = unitOfWork;
        _shipPlacementService = shipPlacementService;
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
        else
        {
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, UserId = userId, Side = PlayerSide.Player1 });
            game.Players.Add(new GamePlayer { Id = Guid.NewGuid(), GameId = game.Id, Side = PlayerSide.Player2 });
            await _unitOfWork.Games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();
        }

        return game;
    }

    public async Task<Game> StartGameAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) ?? throw new DomainException("Игра не найдена");
        if (game.Status != GameStatus.WaitingForShips && game.Status != GameStatus.ReadyToStart)
            throw new DomainException("Игру нельзя начать в текущем статусе");
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

        return new GameStateDto(game.Id, game.Mode, game.Status, game.CurrentTurn, game.Winner, mySide, myShips, enemySunk, myMoves, enemyMoves);
    }

    public async Task RequestPauseAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) ?? throw new DomainException("Игра не найдена");
        if (game.Status != GameStatus.InProgress) throw new GameNotActiveException();
        game.Status = GameStatus.Paused;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AcceptPauseAsync(Guid gameId, Guid userId)
    {
        await Task.CompletedTask;
    }

    public async Task ResumeGameAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdAsync(gameId) ?? throw new DomainException("Игра не найдена");
        if (game.Status != GameStatus.Paused) throw new DomainException("Игра не на паузе");
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
}
