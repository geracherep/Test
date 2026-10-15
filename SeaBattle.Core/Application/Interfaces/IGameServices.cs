using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Application.Interfaces;

public interface IGameService
{
    Task<Game> CreateGameAsync(Guid userId, CreateGameRequest request);
    /// <summary>Подключение второго живого игрока к PvP-игре (по идентификатору игры).</summary>
    Task<Game> JoinGameAsync(Guid gameId, Guid userId);
    Task<Game> StartGameAsync(Guid gameId, Guid userId);
    Task<GameStateDto> GetStateAsync(Guid gameId, Guid userId);
    /// <summary>Запрос паузы. В PvP требует подтверждения вторым игроком.</summary>
    Task<PauseRequest> RequestPauseAsync(Guid gameId, Guid userId);
    /// <summary>Подтверждение паузы вторым игроком. Пауза действует с момента подтверждения.</summary>
    Task AcceptPauseAsync(Guid gameId, Guid userId);
    /// <summary>Отклонение запроса на паузу (игра продолжается).</summary>
    Task DeclinePauseAsync(Guid gameId, Guid userId);
    Task ResumeGameAsync(Guid gameId, Guid userId);
    Task StopGameAsync(Guid gameId, Guid userId);
}

public interface IShipPlacementService
{
    Task PlaceShipsManuallyAsync(Guid gameId, Guid userId, PlaceShipsRequest request);
    Task PlaceShipsRandomlyAsync(Guid gameId, Guid userId);
}

public interface IMoveService
{
    Task<MoveResultDto> MakeMoveAsync(Guid gameId, Guid userId, MakeMoveRequest request);
    Task<MoveResultDto> MakeAiMoveAsync(Guid gameId);
}
