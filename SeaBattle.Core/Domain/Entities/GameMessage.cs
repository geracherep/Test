namespace SeaBattle.Core.Domain.Entities;

public class GameMessage
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Game Game { get; set; } = null!;
}
