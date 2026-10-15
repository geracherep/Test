using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Configurations;

public class ShipCellConfiguration : IEntityTypeConfiguration<ShipCell>
{
    public void Configure(EntityTypeBuilder<ShipCell> builder)
    {
        builder.HasKey(sc => sc.Id);
        
        builder.HasIndex(sc => new { sc.ShipId, sc.Row, sc.Column })
            .IsUnique();
    }
}
