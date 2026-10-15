using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;
using SeaBattle.Core.Domain.Exceptions;
using SeaBattle.Core.Domain.ValueObjects;

namespace SeaBattle.Core.Application.Services;

public class ShipPlacementService : IShipPlacementService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShipPlacementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task PlaceShipsManuallyAsync(Guid gameId, Guid userId, PlaceShipsRequest request)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) 
            ?? throw new DomainException("Игра не найдена");
            
        var player = game.Players.FirstOrDefault(p => p.UserId == userId) 
            ?? throw new DomainException("Вы не участвуете в этой игре");

        if (player.ShipsPlaced)
            throw new DomainException("Корабли уже расставлены");

        ValidatePlacement(request.Ships);
        SaveShips(game.Id, player.Side, request.Ships);

        player.ShipsPlaced = true;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task PlaceShipsRandomlyAsync(Guid gameId, Guid userId)
    {
        var game = await _unitOfWork.Games.GetByIdWithPlayersAsync(gameId) 
            ?? throw new DomainException("Игра не найдена");
            
        var player = game.Players.FirstOrDefault(p => userId == Guid.Empty ? (p.IsComputer && !p.ShipsPlaced) : p.UserId == userId) 
            ?? throw new DomainException("Игрок не найден");

        if (player.ShipsPlaced) return;

        var randomShips = GenerateRandomPlacement();
        SaveShips(game.Id, player.Side, randomShips);

        player.ShipsPlaced = true;
        await _unitOfWork.SaveChangesAsync();
    }

    private void ValidatePlacement(List<ShipPlacementDto> ships)
    {
        if (ships.Count != ShipConfiguration.TotalShips)
            throw new InvalidPlacementException("Неверное количество кораблей");

        bool[,] grid = new bool[BoardSize.Rows, BoardSize.Columns];

        foreach (var ship in ships)
        {
            for (int i = 0; i < ship.Size; i++)
            {
                int r = ship.IsHorizontal ? ship.StartRow : ship.StartRow + i;
                int c = ship.IsHorizontal ? ship.StartColumn + i : ship.StartColumn;

                if (r < 0 || r >= 10 || c < 0 || c >= 10)
                    throw new InvalidPlacementException("Корабль выходит за границы поля");

                // Проверка касаний (включая диагонали)
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int nr = r + dr, nc = c + dc;
                        if (nr >= 0 && nr < 10 && nc >= 0 && nc < 10 && grid[nr, nc])
                            throw new InvalidPlacementException("Корабли касаются друг друга");
                    }
                }
                grid[r, c] = true;
            }
        }
    }

    private List<ShipPlacementDto> GenerateRandomPlacement()
    {
        var result = new List<ShipPlacementDto>();
        bool[,] grid = new bool[10, 10];
        var random = new Random();

        foreach (var size in ShipConfiguration.Ships.Keys.OrderByDescending(x => x))
        {
            int count = ShipConfiguration.Ships[size];
            for (int i = 0; i < count; i++)
            {
                bool placed = false;
                while (!placed)
                {
                    bool isHoriz = random.Next(2) == 0;
                    int r = random.Next(isHoriz ? 10 : 10 - size + 1);
                    int c = random.Next(isHoriz ? 10 - size + 1 : 10);

                    bool canPlace = true;
                    for (int j = 0; j < size; j++)
                    {
                        int cr = isHoriz ? r : r + j;
                        int cc = isHoriz ? c + j : c;
                        for (int dr = -1; dr <= 1; dr++)
                            for (int dc = -1; dc <= 1; dc++)
                            {
                                int nr = cr + dr, nc = cc + dc;
                                if (nr >= 0 && nr < 10 && nc >= 0 && nc < 10 && grid[nr, nc])
                                    canPlace = false;
                            }
                    }

                    if (canPlace)
                    {
                        result.Add(new ShipPlacementDto(size, r, c, isHoriz));
                        for (int j = 0; j < size; j++)
                        {
                            int cr = isHoriz ? r : r + j;
                            int cc = isHoriz ? c + j : c;
                            grid[cr, cc] = true;
                        }
                        placed = true;
                    }
                }
            }
        }
        return result;
    }

    private void SaveShips(Guid gameId, PlayerSide side, List<ShipPlacementDto> ships)
    {
        foreach (var dto in ships)
        {
            var ship = new Ship { Id = Guid.NewGuid(), GameId = gameId, OwnerSide = side, Size = dto.Size };
            for (int i = 0; i < dto.Size; i++)
            {
                int r = dto.IsHorizontal ? dto.StartRow : dto.StartRow + i;
                int c = dto.IsHorizontal ? dto.StartColumn + i : dto.StartColumn;
                ship.Cells.Add(new ShipCell { Id = Guid.NewGuid(), Row = r, Column = c });
            }
            _unitOfWork.Ships.AddAsync(ship);
        }
    }
}

