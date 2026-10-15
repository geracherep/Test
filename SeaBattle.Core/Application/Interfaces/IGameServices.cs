using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Application.Interfaces;

public interface IGameService
{
    Task<Game> CreateGameAsync(Guid userId, CreateGameRequest request);
    Task<Game> StartGameAsync(Guid gameId, Guid userId);
    Task<GameStateDto> GetStateAsync(Guid gameId, Guid userId);
    Task RequestPauseAsync(Guid gameId, Guid userId);
    Task AcceptPauseAsync(Guid gameId, Guid userId);
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
