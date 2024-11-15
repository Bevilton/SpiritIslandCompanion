using Domain.Models.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new GameId(x));

        builder.OwnsOne(x => x.Result, b =>
        {
            b.OwnsOne(x => x.Id);
            b.OwnsOne(x => x.Blight);
            b.OwnsOne(x => x.Cards);
            b.OwnsOne(x => x.Score);
            b.OwnsOne(x => x.Dahan);
            b.OwnsOne(x => x.ScoreModifier);
        });
        builder.OwnsMany(x => x.PlayedAdversaries, b =>
        {
            b.ToTable($"{nameof(Game)}_{nameof(PlayedAdversary)}");
            b.WithOwner().HasForeignKey(nameof(GameId));

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasConversion(x => x.Value, x => new PlayedAdversaryId(x));

            b.OwnsOne(x => x.AdversaryId);
            b.OwnsOne(x => x.Level);
        });
        builder.OwnsOne(x => x.Scenario, b =>
        {
            b.OwnsOne(x => x.Id);
            b.OwnsOne(x => x.ScenarioId);

            b.ToTable($"{nameof(Game)}_{nameof(PlayedScenario)}");
        });
        builder.OwnsOne(x => x.IslandSetupId);
        builder.OwnsOne(x => x.Difficulty);
        builder.OwnsOne(x => x.Note);

        builder.OwnsMany(x => x.Players, b =>
        {
            b.ToTable($"{nameof(Game)}_{nameof(GamePlayer)}");
            b.WithOwner().HasForeignKey(nameof(GameId));

            b.HasKey(x => x.Id);
            b.Property(x => x.Id)
                .HasConversion(x => x.Value, x => new GamePlayerId(x));

            b.OwnsOne(x => x.SpiritId);
            b.OwnsOne(x => x.AspectId);
            b.OwnsOne(x => x.StartingBoard);
            b.OwnsOne(x => x.UserId);
            b.OwnsOne(x => x.PlayerId);
        });
    }
}