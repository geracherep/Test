using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Application.DTOs.Leaderboard;

public record LeaderboardEntryDto(
    Guid UserId,
    string Username,
    Rank Rank,
    int Level,
    int Experience,
    int Wins,
    int Losses,
    double WinRate,
    double Score
);
