using System.Collections.Concurrent;
using SeaBattle.Core.Application.Interfaces;

namespace SeaBattle.WebApi.Services;

/// <summary>
/// Реестр SignalR-подключений: userId -> connectionIds, gameId -> sides по подключениям.
/// Singleton: нужен и хабу (WebApi), и GameService (Core через IOnlinePresenceTracker).
/// </summary>
public class GameConnectionTracker : IOnlinePresenceTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _userConnections = new();
    private readonly ConcurrentDictionary<string, Guid> _connectionUser = new();          // connectionId -> userId
    private readonly ConcurrentDictionary<string, (Guid GameId, int Side)> _connectionGame = new(); // connectionId -> game/side

    public void TrackConnection(string connectionId, Guid userId)
    {
        _connectionUser[connectionId] = userId;
        var set = _userConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        set.TryAdd(connectionId, 0);
    }

    public void JoinGame(string connectionId, Guid gameId, int side)
    {
        _connectionGame[connectionId] = (gameId, side);
    }

    public void UntrackConnection(string connectionId)
    {
        if (_connectionUser.TryRemove(connectionId, out var userId) &&
            _userConnections.TryGetValue(userId, out var set))
        {
            set.TryRemove(connectionId, out _);
            if (set.IsEmpty) _userConnections.TryRemove(userId, out _);
        }
        _connectionGame.TryRemove(connectionId, out _);
    }

    public bool IsOnline(Guid userId) =>
        _userConnections.TryGetValue(userId, out var set) && !set.IsEmpty;

    /// <summary>Стороны, чьи игроки сейчас подключены к игре (для персональной рассылки).</summary>
    public IReadOnlyList<int> GetConnectedSides(Guid gameId)
    {
        return _connectionGame.Values
            .Where(x => x.GameId == gameId)
            .Select(x => x.Side)
            .Distinct()
            .ToList();
    }

    /// <summary>Отлучился ли последний игрок с какой-либо стороны игры.</summary>
    public IReadOnlyList<int> GetLeftSides(Guid gameId, string closedConnectionId)
    {
        if (!_connectionGame.TryGetValue(closedConnectionId, out var info)) return Array.Empty<int>();
        var stillThere = _connectionGame
            .Where(kv => kv.Key != closedConnectionId && kv.Value.GameId == gameId)
            .Select(kv => kv.Value.Side)
            .ToHashSet();
        return stillThere.Contains(info.Side) ? Array.Empty<int>() : new[] { info.Side };
    }
}
