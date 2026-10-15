namespace SeaBattle.Core.Domain.ValueObjects;

public static class ShipConfiguration
{
    public static readonly Dictionary<int, int> Ships = new()
    {
        { 4, 1 }, // 1 четырёхпалубный
        { 3, 2 }, // 2 трёхпалубных
        { 2, 3 }, // 3 двухпалубных
        { 1, 4 }  // 4 однопалубных
    };

    public static int TotalShips => Ships.Values.Sum();
    public static int TotalCells => Ships.Sum(s => s.Key * s.Value);
}
