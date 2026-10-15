using Microsoft.EntityFrameworkCore;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Persistence.Repositories;

public class GameRepository : Repository<Game>, IGameRepository
{
    public GameRepository(AppDbContext context) : base(context) { }

    public async Task<Game?> GetByIdWithPlayersAsync(Guid id)
    {
        return await _dbSet
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
                    .ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Game?> GetByIdWithShipsAsync(Guid id)
    {
        return await _dbSet
            .Include(g => g.Players)
                .ThenInclude(p => p.User)
                    .ThenInclude(u => u.Profile)
            .Include(g => g.Ships)
                .ThenInclude(s => s.Cells)
            // Убрали .Include(g => g.Moves), чтобы EF Core не путался с состоянием коллекции
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Game?> GetByIdWithMovesAsync(Guid id)
    {
        return await _dbSet
            .Include(g => g.Moves)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<IReadOnlyList<Game>> GetActiveGamesAsync()
    {
        return await _dbSet
            .Where(g => g.Status == GameStatus.InProgress || g.Status == GameStatus.Paused)
            .ToListAsync();
    }
}
