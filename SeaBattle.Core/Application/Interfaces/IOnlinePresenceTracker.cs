namespace SeaBattle.Core.Application.Interfaces;

/// <summary>
/// Отслеживание онлайн-присутствия пользователей (реализация — SignalR в WebApi).
/// Нужен, чтобы в состоянии игры показывать, подключён ли соперник.
/// </summary>
public interface IOnlinePresenceTracker
{
    bool IsOnline(Guid userId);
}
