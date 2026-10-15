using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Configurations;

public class MoveConfiguration : IEntityTypeConfiguration<Move>
{
    public void Configure(EntityTypeBuilder<Move> builder)
    {
        builder.HasKey(m => m.Id);
        
        builder.HasIndex(m => new { m.GameId, m.Row, m.Column, m.PlayerSide })
            .IsUnique();
    }
}
