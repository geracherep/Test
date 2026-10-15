using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Domain.Entities;

public class Game
{
    public Guid Id { get; set; }
    public GameMode Mode { get; set; }
    public GameStatus Status { get; set; } = GameStatus.WaitingForShips;
    public PlayerSide? CurrentTurn { get; set; }
    public PlayerSide? Winner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    
    public ICollection<GamePlayer> Players { get; set; } = new List<GamePlayer>();
    public ICollection<Ship> Ships { get; set; } = new List<Ship>();
    public ICollection<Move> Moves { get; set; } = new List<Move>();
    public ICollection<PauseRequest> PauseRequests { get; set; } = new List<PauseRequest>();
    public ICollection<GameMessage> Messages { get; set; } = new List<GameMessage>();
}
