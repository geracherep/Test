using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Domain.Entities;

public class Move
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public PlayerSide PlayerSide { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public MoveResult Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Game Game { get; set; } = null!;
}
