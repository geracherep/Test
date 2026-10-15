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

    public MoveService(IUnitOfWork unitOfWork, IAiStrategyFactory aiFactory)
    {
        _unitOfWork = unitOfWork;
        _aiFactory = aiFactory;
    }

    public async Task<MoveResultDto> MakeMoveAsync(Guid gameId, Guid userId, MakeMoveRequest request)
    {
        var game = await _unitOfWork.Games.GetByIdWithShipsAsync(gameId) 
            ?? throw new DomainException("Игра не найдена");
            
        var player = game.Players.FirstOrDefault(p => p.UserId == userId) 
            ?? throw new DomainException("Игрок не найден в игре");
            
        if (game.CurrentTurn != player.Side)
            throw new NotYourMoveException();

        var result = await ProcessShotAsync(game, player.Side, request.Row, request.Column);
        
        var move = new Move
        {
            Id = Guid.NewGuid(), 
            GameId = gameId, 
            PlayerSide = player.Side,
            Row = request.Row, 
            Column = request.Column, 
            Result = result, 
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Moves.AddAsync(move);

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

        return new MoveResultDto(result, request.Row, request.Column, isFinished, game.Winner);
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
        var result = await ProcessShotAsync(game, aiPlayer.Side, shot.Row, shot.Column);

        var move = new Move
        {
            Id = Guid.NewGuid(), 
            GameId = gameId, 
            PlayerSide = aiPlayer.Side,
            Row = shot.Row, 
            Column = shot.Column, 
            Result = result, 
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Moves.AddAsync(move);

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

        return new MoveResultDto(result, shot.Row, shot.Column, isFinished, game.Winner);
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
        // 1. Загружаем ВСЕ существующие ходы из БД один раз
        var existingMoves = await _unitOfWork.Moves.FindAsync(m => m.GameId == gameId && m.PlayerSide == shooterSide);
        
        // 2. Создаём HashSet для мгновенной проверки (и из БД, и из тех, что мы добавим в этом цикле)
        var processedCoords = new HashSet<string>(existingMoves.Select(m => $"{m.Row},{m.Column}"));

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
                        
                        // Проверяем: если клетки ещё нет ни в БД, ни в списке добавленных в этом цикле
                        if (!processedCoords.Contains(coord))
                        {
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
                            
                            // Сразу добавляем в HashSet, чтобы при проверке следующей палубы того же корабля 
                            // эта клетка уже считалась обработанной!
                            processedCoords.Add(coord);
                        }
                    }
                }
            }
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
