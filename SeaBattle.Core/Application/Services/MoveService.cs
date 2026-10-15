using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;
using SeaBattle.Core.Domain.Exceptions;
using SeaBattle.Core.Domain.ValueObjects;
using SeaBattle.Core.Application.AI;

namespace SeaBattle.Core.Application.Services;

public class MoveService : IMoveService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiStrategyFactory _aiFactory;
    private readonly IGameNotificationService? _notifications;

    public MoveService(
        IUnitOfWork unitOfWork,
        IAiStrategyFactory aiFactory,
        IGameNotificationService? notifications = null)
    {
        _unitOfWork = unitOfWork;
        _aiFactory = aiFactory;
        _notifications = notifications;
    }

    public async Task<MoveResultDto> MakeMoveAsync(Guid gameId, Guid userId, MakeMoveRequest request)
    {
        var game = await _unitOfWork.Games.GetByIdWithShipsAsync(gameId) 
            ?? throw new DomainException("Игра не найдена");
            
        var player = game.Players.FirstOrDefault(p => p.UserId == userId) 
            ?? throw new DomainException("Игрок не найден в игре");
            
        if (game.Status == GameStatus.Paused)
            throw new GameNotActiveException("Игра на паузе");
        if (game.Mode == GameMode.HumanVsHuman && game.CurrentTurn != player.Side)
            throw new NotYourMoveException("В PvP-режиме ход переходит к сопернику после каждого выстрела");
        if (game.CurrentTurn != player.Side)
            throw new NotYourMoveException();

        // ВАЖНО: добавляем ход в контекст ДО ProcessShotAsync, чтобы авто-промахи
        // вокруг потопленного корабля видели этот выстрел как уже существующий.
        // Результат проставим после обработки; если выстрел невалидный ("Сюда уже стреляли"),
        // откатываем сущность из контекста, чтобы не осталось "висячего" Added-хода
        var move = new Move
        {
            Id = Guid.NewGuid(), 
            GameId = gameId, 
            PlayerSide = player.Side,
            Row = request.Row, 
            Column = request.Column, 
            Result = MoveResult.Hit, // уточним ниже после обработки выстрела
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Moves.AddAsync(move);

        MoveResult result;
        try
        {
            result = await ProcessShotAsync(game, player.Side, request.Row, request.Column);
        }
        catch
        {
            _unitOfWork.DetachEntity(move); // убираем незавершённый ход из ChangeTracker
            throw;
        }
        move.Result = result;

        if (result == MoveResult.Miss)
            game.CurrentTurn = player.Side == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;

        bool isFinished = CheckWinCondition(game, player.Side);
        if (isFinished)
        {
            game.Status = GameStatus.Finished;
            game.Winner = player.Side;
            game.FinishedAt = DateTime.UtcNow;
            await HandleGameEndAsync(game);
        }

        await _unitOfWork.SaveChangesAsync();

        var dto = new MoveResultDto(result, request.Row, request.Column, isFinished, game.Winner);
        await NotifyMoveAsync(game, player.Side, dto);

        return dto;
    }

    public async Task<MoveResultDto> MakeAiMoveAsync(Guid gameId)
    {
        var game = await _unitOfWork.Games.GetByIdWithShipsAsync(gameId) 
            ?? throw new DomainException("Игра не найдена");

        if (!game.CurrentTurn.HasValue || game.Status != GameStatus.InProgress)
            throw new GameNotActiveException();

        var aiPlayer = game.Players.First(p => p.Side == game.CurrentTurn && p.IsComputer);
        var strategy = _aiFactory.GetStrategy(aiPlayer.AiStrategy ?? AiStrategyType.Random);
        
        var shot = strategy.ChooseShot(game, aiPlayer.Side);

        // ВАЖНО: добавляем ход в контекст ДО ProcessShotAsync (см. MakeMoveAsync)
        var move = new Move
        {
            Id = Guid.NewGuid(), 
            GameId = gameId, 
            PlayerSide = aiPlayer.Side,
            Row = shot.Row, 
            Column = shot.Column, 
            Result = MoveResult.Hit, // уточним ниже после обработки выстрела
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Moves.AddAsync(move);

        MoveResult result;
        try
        {
            result = await ProcessShotAsync(game, aiPlayer.Side, shot.Row, shot.Column);
        }
        catch
        {
            _unitOfWork.DetachEntity(move); // убираем незавершённый ход из ChangeTracker
            throw;
        }
        move.Result = result;

        if (result == MoveResult.Miss)
            game.CurrentTurn = aiPlayer.Side == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;

        bool isFinished = CheckWinCondition(game, aiPlayer.Side);
        if (isFinished)
        {
            game.Status = GameStatus.Finished;
            game.Winner = aiPlayer.Side;
            game.FinishedAt = DateTime.UtcNow;
            await HandleGameEndAsync(game);
        }

        await _unitOfWork.SaveChangesAsync();

        var dto = new MoveResultDto(result, shot.Row, shot.Column, isFinished, game.Winner);
        await NotifyMoveAsync(game, aiPlayer.Side, dto);

        return dto;
    }

    private async Task<MoveResult> ProcessShotAsync(Game game, PlayerSide shooterSide, int row, int col)
    {
        var enemySide = shooterSide == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        var enemyShips = game.Ships.Where(s => s.OwnerSide == enemySide).ToList();

        var existingMove = await _unitOfWork.Moves.FirstOrDefaultAsync(m => 
            m.GameId == game.Id && m.PlayerSide == shooterSide && m.Row == row && m.Column == col);
            
        if (existingMove != null)
            throw new DomainException("Сюда уже стреляли");

        var hitCell = enemyShips.SelectMany(s => s.Cells).FirstOrDefault(c => c.Row == row && c.Column == col);

        if (hitCell == null) return MoveResult.Miss;

        hitCell.IsHit = true;
        var ship = enemyShips.First(s => s.Cells.Contains(hitCell));

        if (ship.Cells.All(c => c.IsHit))
        {
            ship.IsSunk = true;
            await MarkSurroundingCellsAsync(game.Id, shooterSide, ship);
        }

        return MoveResult.Hit;
    }

    private async Task MarkSurroundingCellsAsync(Guid gameId, PlayerSide shooterSide, Ship sunkShip)
    {
        // 1. Координаты самого потопленного корабля — вокруг них не ставим промахи внутрь корпуса
        var shipCoords = new HashSet<string>(sunkShip.Cells.Select(c => $"{c.Row},{c.Column}"));

        // 2. Существующие ходы: и сохранённые в БД, и уже добавленные в контекст EF Core,
        //    но ещё не сохранённые (Local). Без Local мы бы не увидели текущий выстрел
        //    и другие авто-промахи из предыдущих потоплений в рамках одного SaveChanges,
        //    что и приводило к "UNIQUE constraint failed: Moves".
        var trackedMoves = _unitOfWork.GetTrackedNewMoves(gameId, shooterSide);
        var existingMoves = await _unitOfWork.Moves.FindAsync(m => m.GameId == gameId && m.PlayerSide == shooterSide);

        var processedCoords = new HashSet<string>(
            existingMoves
                .Concat(trackedMoves)
                .Select(m => $"{m.Row},{m.Column}"));

        foreach (var cell in sunkShip.Cells)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = cell.Row + dr, nc = cell.Column + dc;
                    if (nr >= 0 && nr < 10 && nc >= 0 && nc < 10)
                    {
                        string coord = $"{nr},{nc}";

                        // Пропускаем клетки самого корабля и всё, по чему уже стреляли
                        if (shipCoords.Contains(coord)) continue;
                        if (processedCoords.Contains(coord)) continue;

                        await _unitOfWork.Moves.AddAsync(new Move
                        {
                            Id = Guid.NewGuid(), 
                            GameId = gameId, 
                            PlayerSide = shooterSide,
                            Row = nr, 
                            Column = nc, 
                            Result = MoveResult.Miss, 
                            CreatedAt = DateTime.UtcNow
                        });

                        // Сразу помечаем координату обработанной, чтобы следующий залп
                        // того же или другого потопления в этой транзакции не создал дубликат
                        processedCoords.Add(coord);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Рассылка событий онлайн-режима после успешного сохранения хода.
    /// Ошибки рассылки не должны ломать игровой процесс — только логируются вызывающим кодом.
    /// </summary>
    private async Task NotifyMoveAsync(Game game, PlayerSide shooterSide, MoveResultDto dto)
    {
        if (_notifications == null || game.Mode == GameMode.ComputerVsComputer) return;

        try
        {
            await _notifications.NotifyShotAsync(game.Id, dto, shooterSide);
            await _notifications.PushStatesAsync(game);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MoveService] Не удалось отправить SignalR-событие: {ex.Message}");
        }
    }

    private bool CheckWinCondition(Game game, PlayerSide shooterSide)
    {
        var enemySide = shooterSide == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        return game.Ships.Where(s => s.OwnerSide == enemySide).All(s => s.IsSunk);
    }

    private async Task HandleGameEndAsync(Game game)
    {
        var winnerPlayer = game.Players.FirstOrDefault(p => p.Side == game.Winner);
        var loserPlayer = game.Players.FirstOrDefault(p => p.Side != game.Winner);

        if (winnerPlayer?.UserId != null)
        {
            var winner = await _unitOfWork.Users.GetByIdWithProfileAsync(winnerPlayer.UserId.Value);
            if (winner?.Profile != null)
            {
                winner.Profile.Wins++;
                winner.Profile.GamesPlayed++;
                winner.Profile.CurrentStreak++;
                if (winner.Profile.CurrentStreak > winner.Profile.BestStreak)
                    winner.Profile.BestStreak = winner.Profile.CurrentStreak;
                
                winner.Profile.Experience += 100;
                winner.Profile.Rank = CalculateRank(winner.Profile.Experience);
            }
        }

        if (loserPlayer?.UserId != null)
        {
            var loser = await _unitOfWork.Users.GetByIdWithProfileAsync(loserPlayer.UserId.Value);
            if (loser?.Profile != null)
            {
                loser.Profile.Losses++;
                loser.Profile.GamesPlayed++;
                loser.Profile.CurrentStreak = 0;
                loser.Profile.Experience += 10;
                loser.Profile.Rank = CalculateRank(loser.Profile.Experience);
            }
        }
    }

    private Rank CalculateRank(int xp)
    {
        return xp switch
        {
            >= 10001 => Rank.Admiral,
            >= 6001 => Rank.Commander,
            >= 3001 => Rank.Captain,
            >= 1501 => Rank.Navigator,
            >= 501 => Rank.Boatswain,
            >= 101 => Rank.Sailor,
            _ => Rank.Recruit
        };
    }
}
