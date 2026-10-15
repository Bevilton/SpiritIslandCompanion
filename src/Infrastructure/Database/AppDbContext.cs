using Application.Data;
using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.IslandLayout;
using Domain.Models.Player;
using Domain.Models.PlayerMerge;
using Domain.Models.Static;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<PlayerMergeRequest> PlayerMergeRequests => Set<PlayerMergeRequest>();
    public DbSet<CustomIslandLayout> CustomIslandLayouts => Set<CustomIslandLayout>();

    public void DiscardChanges() => ChangeTracker.Clear();

    /// <summary>
    /// Every single-value value object in the domain is one column, wherever it appears — a
    /// key, a reference to the catalogue, a count, a name. Said once here, by type, rather
    /// than property by property in each configuration; a value object's own limit is its
    /// column width. See <see cref="ValueObjectConverter{TValueObject,TValue}"/> for why a
    /// converter and not an owned type.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Identities.
        Map<GameId, Guid>();
        Map<GamePlayerId, Guid>();
        Map<PlayedAdversaryId, Guid>();
        Map<PlayerId, Guid>();
        Map<UserId, Guid>();
        Map<FriendshipId, Guid>();
        Map<PlayerMergeRequestId, Guid>();
        Map<CustomIslandLayoutId, Guid>();

        // References into the static catalogue.
        Map<SpiritId, string>();
        Map<AspectId, string>();
        Map<BoardId, string>();
        Map<AdversaryId, string>();
        Map<ScenarioId, string>();
        Map<IslandSetupId, string>();

        // Counts and scores.
        Map<AdversaryLevel, int>();
        Map<Difficulty, int>();
        Map<DifficultyModifier, int>();
        Map<Score, int>();
        Map<ScoreModifier, int>();
        Map<CardsCount, int>();
        Map<BlightCount, int>();
        Map<DahanCount, int>();

        // Text, cut to the length the domain accepts.
        Map<Email, string>().HaveMaxLength(Email.MaxLength);
        Map<Nickname, string>().HaveMaxLength(Nickname.MaxLength);
        Map<PlayerName, string>().HaveMaxLength(PlayerName.MaxLength);
        Map<GameNote, string>().HaveMaxLength(GameRestrictions.MaximumNoteLength);
        Map<IslandLayoutName, string>().HaveMaxLength(IslandLayoutName.MaxLength);
        Map<IslandLayoutGeometry, string>().HaveMaxLength(IslandLayoutGeometry.MaxLength);

        PropertiesConfigurationBuilder<TValueObject> Map<TValueObject, TValue>() where TValueObject : notnull =>
            configurationBuilder.Properties<TValueObject>().HaveConversion<ValueObjectConverter<TValueObject, TValue>>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
