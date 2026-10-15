using SeaBattle.Core.Application.DTOs.Leaderboard;
using SeaBattle.Core.Application.Interfaces;

namespace SeaBattle.Core.Application.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public LeaderboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetTopAsync(int count)
    {
        var users = await _unitOfWork.Users.GetAllWithProfilesAsync();

        var leaderboard = users
            .Where(u => u.Profile != null)
            .Select(u =>
            {
                double winRate = u.Profile!.GamesPlayed > 0 
                    ? (double)u.Profile.Wins / u.Profile.GamesPlayed * 100 
                    : 0;

                double score = (u.Profile.Wins * 100) 
                             + ((int)u.Profile.Rank * 50) 
                             + (winRate * 2) 
                             + (u.Profile.BestStreak * 20);

                return new LeaderboardEntryDto(
                    UserId: u.Id,
                    Username: u.Username,
                    Rank: u.Profile.Rank,
                    Level: (int)u.Profile.Rank,
                    Experience: u.Profile.Experience,
                    Wins: u.Profile.Wins,
                    Losses: u.Profile.Losses,
                    WinRate: Math.Round(winRate, 2),
                    Score: Math.Round(score, 2)
                );
            })
            .OrderByDescending(x => x.Score)
            .Take(count)
            .ToList();

        return leaderboard;
    }
}
