using Microsoft.EntityFrameworkCore;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    
    private IUserRepository? _users;
    private IGameRepository? _games;
    private IRepository<Ship>? _ships;
    private IRepository<Move>? _moves;
    private IRepository<PauseRequest>? _pauseRequests;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IGameRepository Games => _games ??= new GameRepository(_context);
    public IRepository<Ship> Ships => _ships ??= new Repository<Ship>(_context);
    public IRepository<Move> Moves => _moves ??= new Repository<Move>(_context);
    public IRepository<PauseRequest> PauseRequests => _pauseRequests ??= new Repository<PauseRequest>(_context);

    public IReadOnlyList<Move> GetTrackedNewMoves(Guid gameId, PlayerSide playerSide)
    {
        // Local смотрит только в память контекста (без запроса к БД) и включает
        // сущности в состоянии Added, которые ещё не записаны в базу.
        // Это позволяет видеть текущий выстрел и ранее добавленные авто-промахи
        // до вызова SaveChangesAsync и не создавать дубликаты.
        return _context.ChangeTracker.Entries<Move>()
            .Where(e => e.State == EntityState.Added
                        && e.Entity.GameId == gameId
                        && e.Entity.PlayerSide == playerSide)
            .Select(e => e.Entity)
            .ToList();
    }

    public void DetachEntity(object entity)
    {
        _context.Entry(entity).State = EntityState.Detached;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
