using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaBattle.Core.Domain.Entities;

namespace SeaBattle.Persistence.Configurations;

public class PauseRequestConfiguration : IEntityTypeConfiguration<PauseRequest>
{
    public void Configure(EntityTypeBuilder<PauseRequest> builder)
    {
        builder.HasKey(pr => pr.Id);

        // Активные (неподтверждённые) запросы ищем по этому индексу
        builder.HasIndex(pr => new { pr.GameId, pr.Accepted });
    }
}
