namespace SeaBattle.Core.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IGameRepository Games { get; }
    IRepository<Core.Domain.Entities.Ship> Ships { get; }
    IRepository<Core.Domain.Entities.Move> Moves { get; }
    Task<int> SaveChangesAsync();
}
