using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Experience).HasDefaultValue(0);
        builder.Property(p => p.Wins).HasDefaultValue(0);
        builder.Property(p => p.Losses).HasDefaultValue(0);
        builder.Property(p => p.GamesPlayed).HasDefaultValue(0);
        builder.Property(p => p.BestStreak).HasDefaultValue(0);
        builder.Property(p => p.CurrentStreak).HasDefaultValue(0);
    }
}
