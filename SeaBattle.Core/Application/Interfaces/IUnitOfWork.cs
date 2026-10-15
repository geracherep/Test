namespace SeaBattle.Core.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IGameRepository Games { get; }
    IRepository<Core.Domain.Entities.Ship> Ships { get; }
    IRepository<Core.Domain.Entities.Move> Moves { get; }
    IRepository<Core.Domain.Entities.PauseRequest> PauseRequests { get; }

    /// <summary>
    /// Возвращает ходы, которые уже добавлены в контекст EF Core (Added), но ещё не сохранены в БД.
    /// Нужен для корректной дедупликации авто-промахов вокруг потопленных кораблей
    /// в рамках одной транзакции SaveChanges.
    /// </summary>
    IReadOnlyList<Core.Domain.Entities.Move> GetTrackedNewMoves(Guid gameId, Core.Domain.Enums.PlayerSide playerSide);

    /// <summary>
    /// Отсоединяет сущность из ChangeTracker (отменяет неотложенное добавление).
    /// Используется для отката хода, если обработка выстрела завершилась исключением.
    /// </summary>
    void DetachEntity(object entity);

    Task<int> SaveChangesAsync();
}
