using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Application.DTOs.Auth;

public record UserProfileDto(
    Guid UserId,
    string Username,
    string Role,
    int Experience,
    Rank Rank,
    int Wins,
    int Losses,
    int GamesPlayed,
    int BestStreak,
    int CurrentStreak);
