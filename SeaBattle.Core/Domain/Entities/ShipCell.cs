using SeaBattle.Core.Domain.ValueObjects;

namespace SeaBattle.Core.Domain.Entities;

public class ShipCell
{
    public Guid Id { get; set; }
    public Guid ShipId { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public bool IsHit { get; set; } = false;
    
    public Ship Ship { get; set; } = null!;
    
    public Coordinate GetCoordinate() => new(Row, Column);
}
