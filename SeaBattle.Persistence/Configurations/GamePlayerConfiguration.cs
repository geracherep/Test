using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Configurations;

public class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.HasKey(gp => gp.Id);
        
        builder.HasOne(gp => gp.User)
            .WithMany(u => u.GamePlayers)
            .HasForeignKey(gp => gp.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
