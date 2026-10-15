using Domain.Models.IslandLayout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class CustomIslandLayoutConfiguration : IEntityTypeConfiguration<CustomIslandLayout>
{
    public void Configure(EntityTypeBuilder<CustomIslandLayout> builder) => builder.HasKey(x => x.Id);
}
