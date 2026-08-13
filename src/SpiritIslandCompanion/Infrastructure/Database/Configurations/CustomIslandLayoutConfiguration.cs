using Domain.Models.IslandLayout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal class CustomIslandLayoutConfiguration : IEntityTypeConfiguration<CustomIslandLayout>
{
    public void Configure(EntityTypeBuilder<CustomIslandLayout> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new CustomIslandLayoutId(x));

        builder.OwnsOne(x => x.OwnerId);

        builder.OwnsOne(x => x.Name, b =>
            b.Property(x => x.Value).HasMaxLength(IslandLayoutName.MaxLength));

        builder.OwnsOne(x => x.Geometry, b =>
            b.Property(x => x.Value).HasMaxLength(IslandLayoutGeometry.MaxLength));
    }
}
