using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Configurations;

public class ShipConfiguration : IEntityTypeConfiguration<Ship>
{
    public void Configure(EntityTypeBuilder<Ship> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.HasMany(s => s.Cells)
            .WithOne(sc => sc.Ship)
            .HasForeignKey(sc => sc.ShipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
