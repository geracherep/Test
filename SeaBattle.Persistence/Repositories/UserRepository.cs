using Microsoft.EntityFrameworkCore;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByIdWithProfileAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IReadOnlyList<User>> GetAllWithProfilesAsync()
    {
        return await _dbSet
            .Include(u => u.Profile)
            .ToListAsync();
    }
}
