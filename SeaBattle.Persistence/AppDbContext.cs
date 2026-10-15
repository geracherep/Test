using Microsoft.EntityFrameworkCore;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();
    public DbSet<Ship> Ships => Set<Ship>();
    public DbSet<ShipCell> ShipCells => Set<ShipCell>();
    public DbSet<Move> Moves => Set<Move>();
    public DbSet<PauseRequest> PauseRequests => Set<PauseRequest>();
    public DbSet<GameMessage> GameMessages => Set<GameMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
