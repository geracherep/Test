using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;
using SeaBattle.Core.Domain.ValueObjects;

namespace SeaBattle.Core.Application.AI;

public interface IAiStrategy
{
    Coordinate ChooseShot(Game game, PlayerSide side);
}

public interface IAiStrategyFactory
{
    IAiStrategy GetStrategy(AiStrategyType type);
}

public class AiStrategyFactory : IAiStrategyFactory
{
    public IAiStrategy GetStrategy(AiStrategyType type)
    {
        return type switch
        {
            AiStrategyType.Smart => new SmartAiStrategy(),
            _ => new RandomAiStrategy()
        };
    }
}

public class RandomAiStrategy : IAiStrategy
{
    private readonly Random _random = new();

    public Coordinate ChooseShot(Game game, PlayerSide side)
    {
        var shotCells = game.Moves
            .Where(m => m.PlayerSide == side)
            .Select(m => $"{m.Row},{m.Column}")
            .ToHashSet();

        var availableCells = new List<Coordinate>();
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 10; c++)
            {
                if (!shotCells.Contains($"{r},{c}"))
                    availableCells.Add(new Coordinate(r, c));
            }
        }

        if (availableCells.Count == 0)
            return new Coordinate(0, 0);

        return availableCells[_random.Next(availableCells.Count)];
    }
}

public class SmartAiStrategy : IAiStrategy
{
    private readonly Random _random = new();

    public Coordinate ChooseShot(Game game, PlayerSide side)
    {
        var shotCells = game.Moves
            .Where(m => m.PlayerSide == side)
            .Select(m => $"{m.Row},{m.Column}")
            .ToHashSet();

        var hitCells = game.Moves
            .Where(m => m.PlayerSide == side && m.Result == MoveResult.Hit)
            .ToList();

        if (hitCells.Count > 0)
        {
            var lastHit = hitCells.Last();
            var neighbors = new List<Coordinate>();
            
            foreach (var direction in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                int nr = lastHit.Row + direction.Item1;
                int nc = lastHit.Column + direction.Item2;
                
                if (nr >= 0 && nr < 10 && nc >= 0 && nc < 10)
                {
                    if (!shotCells.Contains($"{nr},{nc}"))
                        neighbors.Add(new Coordinate(nr, nc));
                }
            }

            if (neighbors.Count > 0)
                return neighbors[_random.Next(neighbors.Count)];
        }

        var availableCells = new List<Coordinate>();
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 10; c++)
            {
                if (!shotCells.Contains($"{r},{c}") && (r + c) % 2 == 0)
                    availableCells.Add(new Coordinate(r, c));
            }
        }

        if (availableCells.Count == 0)
        {
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 10; c++)
                    if (!shotCells.Contains($"{r},{c}"))
                        availableCells.Add(new Coordinate(r, c));
        }

        return availableCells.Count > 0 
            ? availableCells[_random.Next(availableCells.Count)] 
            : new Coordinate(0, 0);
    }
}
