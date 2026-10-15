using SeaBattle.Core.Application.DTOs.Game;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Application.Interfaces;

/// <summary>
/// Инфраструктурный контракт рассылки событий онлайн-режима (реализация — SignalR в WebApi).
/// Core ничего не знает о SignalR, луковичная архитектура соблюдена.
/// </summary>
public interface IGameNotificationService
{
    /// <summary>Отправить обновлённое состояние игры всем подключённым игрокам (персонализированно).</summary>
    Task PushStateAsync(Guid gameId);

    /// <summary>Отправить персональные состояния конкретным сторонам (например, обоим сразу после хода).</summary>
    Task PushStatesAsync(Game game);

    Task NotifyShotAsync(Guid gameId, MoveResultDto result, PlayerSide shooterSide);

    Task NotifyPauseRequestedAsync(Guid gameId, PauseRequestDto pause);
    Task NotifyPauseResolvedAsync(Guid gameId, bool accepted);
    Task NotifyPlayerJoinedAsync(Guid gameId, PlayerInfoDto player);
    Task NotifyOpponentLeftAsync(Guid gameId, PlayerSide side);
}
