using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Domain.Entities;

public class GamePlayer
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Guid? UserId { get; set; }
    public PlayerSide Side { get; set; }
    public bool IsComputer { get; set; } = false;
    public AiStrategyType? AiStrategy { get; set; }
    public bool ShipsPlaced { get; set; } = false;
    
    public Game Game { get; set; } = null!;
    public User? User { get; set; }
}
