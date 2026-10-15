namespace SeaBattle.Core.Application.Interfaces;

public interface IGameRepository : IRepository<Core.Domain.Entities.Game>
{
    Task<Core.Domain.Entities.Game?> GetByIdWithPlayersAsync(Guid id);
    Task<Core.Domain.Entities.Game?> GetByIdWithShipsAsync(Guid id);
    Task<Core.Domain.Entities.Game?> GetByIdWithMovesAsync(Guid id);
    Task<IReadOnlyList<Core.Domain.Entities.Game>> GetActiveGamesAsync();
}
