using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Domain.Entities;

public class PauseRequest
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public PlayerSide RequestedBy { get; set; }
    public bool Accepted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Game Game { get; set; } = null!;
}
