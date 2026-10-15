using Domain.Errors;
using Domain.Models.IslandLayout;
using Domain.Models.Player;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Primitives;

namespace Domain.Models.Game;

// The result type is written as Results.Result throughout: this aggregate has a property
// called Result — the outcome of the game — which would otherwise shadow it.
public class Game : AggregateRoot<GameId>
{
    public DateTimeOffset StartedAt { get; private init; }
    public GameResult? Result { get; private set; }

    /// <summary>The scenario the game was played under, or null for an open game.</summary>
    public ScenarioId? ScenarioId { get; private init; }

    public IslandSetupId IslandSetupId { get; private init; }

    /// <summary>
    /// Where the boards actually sat, for a hand-built island; null when the game used one
    /// of the published layouts, whose geometry is already known from its id.
    /// <para>
    /// This is the game's own copy, not a pointer into the player's layout library: a game
    /// records what was on the table that evening, and editing or deleting a saved layout
    /// afterwards must not rewrite it.
    /// </para>
    /// </summary>
    public IslandLayoutGeometry? IslandLayout { get; private init; }

    /// <summary>
    /// The saved layout this island came from, when the player picked one out of their
    /// library rather than arranging a one-off. Kept so plays can be counted per layout;
    /// the layout may since have been renamed, reshaped or deleted.
    /// </summary>
    public CustomIslandLayoutId? CustomLayoutId { get; private init; }

    /// <summary>
    /// The board on the island that nobody played — the extra board some games add to raise
    /// the difficulty. Null when the game used one board per player.
    /// <para>
    /// Which lettered board it is matters: a board is a specific set of terrains, so an island
    /// of A/B/C reads differently from A/B/E. Null on games recorded before this was asked for,
    /// where the extra board's letter is simply not known.
    /// </para>
    /// </summary>
    public BoardId? ExtraBoard { get; private init; }

    public Difficulty Difficulty { get; private init; }
    public DifficultyModifier DifficultyModifier { get; private init; }
    public GameNote? Note { get; private set; }
    public UserId OwnerId { get; private init; }
    public IReadOnlyCollection<PlayedAdversary> PlayedAdversaries => _playedAdversaries.AsReadOnly();
    private readonly List<PlayedAdversary> _playedAdversaries = [];
    public IReadOnlyCollection<GamePlayer> Players => _players.AsReadOnly();
    private readonly List<GamePlayer> _players = [];

    private Game(
        GameId id,
        DateTimeOffset startedAt,
        UserId ownerId,
        IslandChoice island,
        IReadOnlyList<GamePlayer> players,
        IReadOnlyList<PlayedAdversary> adversaries,
        ScenarioId? scenarioId,
        Difficulty difficulty,
        DifficultyModifier difficultyModifier,
        GameNote? note,
        GameResult? result)
        : base(id)
    {
        StartedAt = startedAt;
        OwnerId = ownerId;
        IslandSetupId = island.SetupId;
        IslandLayout = island.Layout;
        CustomLayoutId = island.SavedLayoutId;
        ExtraBoard = island.ExtraBoard;
        _players.AddRange(players);
        _playedAdversaries.AddRange(adversaries);
        ScenarioId = scenarioId;
        Difficulty = difficulty;
        DifficultyModifier = difficultyModifier;
        Note = note;
        Result = result;
    }

    /// <summary>
    /// A game written down: its table, and how it ended if it has ended — without an outcome
    /// it is a draft, which <see cref="Complete"/> finishes later. Either way the table has
    /// already passed every rule, or there would be no <see cref="GameSetup"/> to hand over;
    /// and the score is the game's to compute from the outcome and its own table, never the
    /// caller's to supply (see <see cref="GameResult"/>).
    /// </summary>
    public static Game Create(
        GameId id,
        DateTimeOffset startedAt,
        UserId ownerId,
        GameSetup setup,
        GameNote? note,
        GameOutcome? outcome) =>
        new(
            id,
            startedAt,
            ownerId,
            setup.Island,
            setup.Players,
            setup.Adversaries,
            setup.ScenarioId,
            setup.Difficulty,
            setup.DifficultyModifier,
            note,
            outcome is null ? null : GameResult.Of(outcome, setup.Difficulty, setup.Players.Count));

    /// <summary>
    /// The same evening written down again in somebody else's name — what a friendship ending
    /// leaves both sides with, so neither loses the games they played together.
    /// <para>
    /// The table is copied seat for seat, so the copy is the same game: the same boards, spirits
    /// and adversaries, the same result. Only who is written down in each seat reads differently
    /// from the other side of the table — see <see cref="GamePlayer.CopyFor"/> — and which local
    /// player stands in for a person the new owner cannot name is the caller's to say, through
    /// <paramref name="localPlayerFor"/>. Since the original already satisfied every rule, the
    /// copy is not asked to satisfy them again.
    /// </para>
    /// <para>
    /// The one deliberate omission is <see cref="CustomLayoutId"/>: it points into the original
    /// owner's layout library, which the new owner has no entry in. The geometry itself travels
    /// with the copy, so the island is not lost — only the link back to a shape that was never
    /// theirs.
    /// </para>
    /// </summary>
    public Game CopyForOwner(GameId id, UserId ownerId, Func<GamePlayer, PlayerId> localPlayerFor) =>
        // The seats and adversaries are entities of the new game, so they are copied into
        // identities of their own; the values — the result included — travel as they are.
        new(
            id,
            StartedAt,
            ownerId,
            new IslandChoice(IslandSetupId, IslandLayout, SavedLayoutId: null, ExtraBoard),
            _players.Select(seat => seat.CopyFor(ownerId, localPlayerFor)).ToList(),
            _playedAdversaries.Select(a => a.Copy()).ToList(),
            ScenarioId,
            Difficulty,
            DifficultyModifier,
            Note,
            Result);

    /// <summary>
    /// Records how a drafted game ended, leaving the setup untouched. The score is worked out
    /// here, from the outcome and the table it was played at. A game has one outcome: once it
    /// is written down, it is not written down again. A note given here replaces whatever the
    /// draft was saved with; none given keeps it.
    /// </summary>
    public Results.Result Complete(GameOutcome outcome, GameNote? note)
    {
        if (Result is not null)
            return Results.Result.Failure(DomainErrors.Game.AlreadyCompleted);

        Result = GameResult.Of(outcome, Difficulty, _players.Count);
        Note = note ?? Note;
        return Results.Result.Success();
    }

    private bool Seats(UserId userId) => _players.Any(p => p.UserId == userId);

    private bool Seats(PlayerId playerId) => _players.Any(p => p.PlayerId == playerId);

    /// <summary>
    /// Whether handing <paramref name="localPlayer"/>'s seats to <paramref name="user"/> would
    /// put the account at this table twice. The app never lays a table like that, and a local
    /// player standing in for someone already seated under their own account is exactly the
    /// mistake a merge would otherwise bake in — so it is refused, here and before it is asked.
    /// </summary>
    public bool SeatsBoth(PlayerId localPlayer, UserId user) => Seats(localPlayer) && Seats(user);

    /// <summary>
    /// Hands every seat the local player holds to a registered account — the guest turned out
    /// to be them (see <see cref="PlayerMerge.PlayerMergeRequest"/>). What was played stays;
    /// only who played it changes. Refused when that would seat the account twice (see
    /// <see cref="SeatsBoth"/>); a table the local player is not at is left as it is.
    /// </summary>
    public Results.Result HandSeatsToUser(PlayerId localPlayer, UserId user)
    {
        if (SeatsBoth(localPlayer, user))
            return Results.Result.Failure(DomainErrors.PlayerMerge.SeatConflict);

        foreach (var seat in _players.Where(p => p.PlayerId == localPlayer))
            seat.ReassignToUser(user);

        return Results.Result.Success();
    }

    /// <summary>
    /// Hands every seat the account holds to a local player — what happens when the friendship
    /// that let the account sit here ends. The record of the evening survives; the link to the
    /// other account does not.
    /// </summary>
    public void HandSeatsToLocalPlayer(UserId user, PlayerId localPlayer)
    {
        foreach (var seat in _players.Where(p => p.UserId == user))
            seat.ReassignToLocalPlayer(localPlayer);
    }

    /// <summary>
    /// Empties every seat the local player holds, keeping what was played — what happens to
    /// their seats when the local player is deleted. The evening was played, and a chair is
    /// not emptied by forgetting who sat in it.
    /// </summary>
    public void UnseatLocalPlayer(PlayerId localPlayer)
    {
        foreach (var seat in _players.Where(p => p.PlayerId == localPlayer))
            seat.Unassign();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private Game()
    {
    }
#pragma warning restore
}
