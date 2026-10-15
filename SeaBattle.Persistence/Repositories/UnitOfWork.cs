using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    
    private IUserRepository? _users;
    private IGameRepository? _games;
    private IRepository<Ship>? _ships;
    private IRepository<Move>? _moves;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IGameRepository Games => _games ??= new GameRepository(_context);
    public IRepository<Ship> Ships => _ships ??= new Repository<Ship>(_context);
    public IRepository<Move> Moves => _moves ??= new Repository<Move>(_context);

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
