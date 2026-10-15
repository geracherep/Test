using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Application.Services;

public class ExperienceService : IExperienceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExperienceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task AddExperienceAsync(UserProfile profile, int xp)
    {
        profile.Experience += xp;
        
        // Расчет ранга
        profile.Rank = profile.Experience switch
        {
            >= 10001 => Rank.Admiral,
            >= 6001 => Rank.Commander,
            >= 3001 => Rank.Captain,
            >= 1501 => Rank.Navigator,
            >= 501 => Rank.Boatswain,
            >= 101 => Rank.Sailor,
            _ => Rank.Recruit
        };
    }

    public async Task UpdateStatsAsync(UserProfile winner, UserProfile? loser)
    {
        // Статистика победителя
        winner.Wins++;
        winner.GamesPlayed++;
        winner.CurrentStreak++;
        if (winner.CurrentStreak > winner.BestStreak)
            winner.BestStreak = winner.CurrentStreak;
        
        await AddExperienceAsync(winner, 100);

        // Статистика проигравшего
        if (loser != null)
        {
            loser.Losses++;
            loser.GamesPlayed++;
            loser.CurrentStreak = 0;
            await AddExperienceAsync(loser, 10);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
