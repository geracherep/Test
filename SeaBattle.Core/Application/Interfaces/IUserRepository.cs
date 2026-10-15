using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Core.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdWithProfileAsync(Guid id);
    Task<IReadOnlyList<User>> GetAllWithProfilesAsync();
}
