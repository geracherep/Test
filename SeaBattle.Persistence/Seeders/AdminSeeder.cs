using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Enums;

namespace SeaBattle.Persistence.Seeders;

public static class AdminSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!context.Users.Any(u => u.Role == UserRole.Admin))
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    Experience = 0,
                    Rank = Rank.Recruit
                }
            };

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();
        }
    }
}
