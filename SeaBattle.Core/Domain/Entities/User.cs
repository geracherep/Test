using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Core.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public UserProfile? Profile { get; set; }
    public ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();
}
