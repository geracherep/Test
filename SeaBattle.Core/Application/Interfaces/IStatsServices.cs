using SeaBattle.Core.Application.DTOs.Leaderboard;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Core.Application.Interfaces;

public interface IExperienceService
{
    Task AddExperienceAsync(UserProfile profile, int xp);
    Task UpdateStatsAsync(UserProfile winner, UserProfile? loser);
}

public interface ILeaderboardService
{
    Task<IReadOnlyList<LeaderboardEntryDto>> GetTopAsync(int count);
}
