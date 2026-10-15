using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Application.DTOs.Game;

public record CreateGameRequest(GameMode Mode, AiStrategyType? AiStrategy);
public record PlaceShipsRequest(List<ShipPlacementDto> Ships);
public record ShipPlacementDto(int Size, int StartRow, int StartColumn, bool IsHorizontal);
public record MakeMoveRequest(int Row, int Column);

public record ShipCellDto(int Row, int Column, bool IsHit);
public record ShipDto(int Size, bool IsSunk, List<ShipCellDto> Cells);
public record MoveDto(int Row, int Column, MoveResult Result);

public record PlayerInfoDto(Guid UserId, string Username, PlayerSide Side, bool ShipsPlaced, bool IsOnline);

public record PauseRequestDto(
    Guid Id,
    PlayerSide RequestedBy,
    DateTime CreatedAt,
    /// <summary>PvP: пауза наступит только после подтверждения вторым игроком.</summary>
    bool RequiresConfirmation,
    bool Accepted);

public record GameStateDto(
    Guid GameId,
    GameMode Mode,
    GameStatus Status,
    PlayerSide? CurrentTurn,
    PlayerSide? Winner,
    PlayerSide MySide,
    List<ShipDto> MyShips,
    List<ShipDto> EnemySunkShips,
    List<MoveDto> MyMoves,
    List<MoveDto> EnemyMoves,
    List<PlayerInfoDto>? Players = null,
    PauseRequestDto? PendingPause = null);

public record MoveResultDto(MoveResult Result, int Row, int Column, bool IsGameFinished, PlayerSide? Winner);

public record CreateGameResponse(Guid GameId, PlayerSide MySide);
