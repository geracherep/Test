using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Domain.Entities;

public class Ship
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public PlayerSide OwnerSide { get; set; }
    public int Size { get; set; }
    public bool IsSunk { get; set; } = false;
    
    public Game Game { get; set; } = null!;
    public ICollection<ShipCell> Cells { get; set; } = new List<ShipCell>();
}
