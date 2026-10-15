namespace SeaBattle.Core.Domain.ValueObjects;

public record Coordinate(int Row, int Column)
{
    public bool IsValid() => Row >= 0 && Row < 10 && Column >= 0 && Column < 10;
}
