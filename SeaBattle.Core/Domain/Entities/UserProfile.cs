using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Experience { get; set; } = 0;
    public Rank Rank { get; set; } = Rank.Recruit;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int GamesPlayed { get; set; } = 0;
    public int BestStreak { get; set; } = 0;
    public int CurrentStreak { get; set; } = 0;
    public DateTime LastGameAt { get; set; }
    
    public User User { get; set; } = null!;
}
