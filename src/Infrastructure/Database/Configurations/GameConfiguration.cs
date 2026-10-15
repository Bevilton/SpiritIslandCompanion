using Domain.Models.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(x => x.Id);

        // The result is one value with two parts — the outcome the table reported, and the score —
        // so it is a complex property, and the outcome a complex property inside it: the columns
        // sit on the game's row, neither has an identity of its own, and a copied game may hold
        // the same instance. Optional: a draft has no result yet, and reads back as null. The
        // outcome's columns keep the names they had when its fields sat directly on the result,
        // so nesting it in the model changed nothing in the schema.
        builder.ComplexProperty(x => x.Result, result =>
        {
            result.IsRequired(false);
            result.ComplexProperty(r => r.Outcome, outcome =>
            {
                outcome.Property(o => o.Win).HasColumnName("Result_Win");
                outcome.Property(o => o.Duration).HasColumnName("Result_Duration");
                outcome.Property(o => o.Cards).HasColumnName("Result_Cards");
                outcome.Property(o => o.TerrorLevel).HasColumnName("Result_TerrorLevel");
                outcome.Property(o => o.Blight).HasColumnName("Result_Blight");
                outcome.Property(o => o.Dahan).HasColumnName("Result_Dahan");
                outcome.Property(o => o.ScoreModifier).HasColumnName("Result_ScoreModifier");
            });
        });

        // The game's own copy of a hand-built arrangement, and the library layout it came
        // from. No foreign key on purpose: deleting a saved layout must not cascade into the
        // games played on it. Guarding the delete is the handler's business, not the schema's —
        // DeleteIslandLayout refuses to drop a layout that recorded games still point at.
        // (CustomLayoutId is mapped by the conventions in AppDbContext, like every other id.)

        builder.OwnsMany(x => x.PlayedAdversaries, b =>
        {
            b.ToTable($"{nameof(Game)}_{nameof(PlayedAdversary)}");
            b.WithOwner().HasForeignKey(nameof(GameId));
            b.HasKey(x => x.Id);
        });

        builder.OwnsMany(x => x.Players, b =>
        {
            b.ToTable($"{nameof(Game)}_{nameof(GamePlayer)}");
            b.WithOwner().HasForeignKey(nameof(GameId));
            b.HasKey(x => x.Id);
        });
    }
}
