using Application.Data;
using Application.Features.Games;
using Application.Features.Games.Dtos;
using Application.Features.IslandLayouts;
using Application.Features.Players;
using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.Game.Enums;
using Domain.Models.Static;
using Domain.Models.Static.Data;
using Domain.Models.User;
using Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Demo;

/// <summary>Fills the demo template database. See <see cref="DemoDataSeeder"/>.</summary>
public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds the demo account's world: a play group with two and a half years of history, written
/// through the same commands the app itself uses, so every difficulty and score on record is
/// one the app's own rules produced.
/// <para>
/// The dataset is shaped so that every screen has something to show wherever a visitor drills
/// in: every spirit played at least twice and every aspect at least once, every adversary
/// fought twice at every level (climbing over time, with win rates that fall as the levels
/// rise), every scenario attempted, plus thematic maps, extra boards, hand-built islands (both
/// ad-hoc and saved to the library), solo nights and full tables, friends and local players,
/// shared games owned by a friend, notes, house-ruled difficulty tweaks, and a few unfinished
/// drafts. The rolls are deterministic (fixed random seed); only the timeline moves — it is
/// re-anchored to end "yesterday" every time the template is rebuilt, which the registry does
/// once per <see cref="DemoSandboxRegistry.TemplateLifetime"/>, so the demo always looks
/// freshly played no matter how long the process has been up.
/// </para>
/// </summary>
public sealed class DemoDataSeeder(IMediator mediator, IAppDbContext db) : IDemoDataSeeder
{
    private static readonly Guid PetraUserId = Guid.Parse("d3300000-0000-4000-8000-000000000002");
    private static readonly Guid JonasUserId = Guid.Parse("d3300000-0000-4000-8000-000000000003");
    private static readonly Guid MayaUserId = Guid.Parse("d3300000-0000-4000-8000-000000000004");

    private static readonly Guid TwinBaysLayoutId = Guid.Parse("d3300000-0000-4000-8000-00000000000a");
    private static readonly Guid SerpentCoilLayoutId = Guid.Parse("d3300000-0000-4000-8000-00000000000b");

    /// <summary>Hand-built arrangements in the island playground's own JSON (boards + seams).</summary>
    private const string TwinBaysGeometry =
        """{"boards":[{"rot":0,"x":0,"y":0},{"rot":180,"x":296.5,"y":14.25}],"bonds":[{"a":0,"ae":2,"b":1,"be":2,"s":0.5}]}""";
    private const string SerpentCoilGeometry =
        """{"boards":[{"rot":0,"x":0,"y":0},{"rot":120,"x":268.4,"y":112.6},{"rot":240,"x":74.2,"y":286.9}],"bonds":[{"a":0,"ae":1,"b":1,"be":2,"s":0.4},{"a":1,"ae":0,"b":2,"be":1,"s":0.5}]}""";
    private const string OpenPalmGeometry =
        """{"boards":[{"rot":30,"x":0,"y":0},{"rot":210,"x":288.1,"y":22.5}],"bonds":[{"a":0,"ae":0,"b":1,"be":0,"s":0.55}]}""";
    private const string BrokenRingGeometry =
        """{"boards":[{"rot":0,"x":0,"y":0},{"rot":90,"x":274.9,"y":86.3},{"rot":180,"x":205.7,"y":342.0},{"rot":270,"x":-72.4,"y":264.1}],"bonds":[{"a":0,"ae":1,"b":1,"be":2,"s":0.5},{"a":1,"ae":0,"b":2,"be":1,"s":0.45},{"a":2,"ae":0,"b":3,"be":1,"s":0.5}]}""";

    /// <summary>
    /// The group's go-to spirits — weighted heavily when a seat has no coverage duty to fill,
    /// so the statistics show favourites with real histories rather than a uniform smear.
    /// </summary>
    private static readonly string[] FavouriteSpirits =
    [
        Spirits.LightningsSwiftStrike.Value,
        Spirits.RiverSurgesInSunlight.Value,
        Spirits.ASpreadOfRampantGreen.Value,
        Spirits.KeeperOfTheForbiddenWilds.Value,
        Spirits.LureOfTheDeepWilderness.Value,
        Spirits.HearthVigil.Value,
    ];

    private static readonly string[] Notes =
    [
        "Blighted island by turn 4 — held on with two dahan and a prayer.",
        "Anna's first time with this spirit. Instant favourite.",
        "Part of our long England campaign — the coast never stood a chance.",
        "Forgot the escalation effect for two whole turns. Counted it anyway.",
        "New year, new island. Same invaders.",
        "Terror III victory on the last possible turn.",
        "We keep underestimating the ravage step. Every. Single. Time.",
        "Marek tried a fear-heavy build and it actually worked.",
        "Lost the west coast early and never recovered.",
        "Rematch of last week's disaster — sweet, sweet revenge.",
        "Board H is cursed for us, no other explanation.",
        "First game with the new expansion on the table.",
        "Two-spirit solo evening. Brain melted, worth it.",
        "House rule: one extra blight on setup. Regretted immediately.",
    ];

    private readonly Random _rng = new(20260820);
    private readonly Dictionary<string, int> _spiritSeatCounts = new();
    private readonly HashSet<string> _aspectsUsed = [];

    private DateTimeOffset _windowStart;
    private DateTimeOffset _windowEnd;
    private List<Guid> _localPlayerIds = [];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Every build of the template starts from an empty database, so anything here already
        // means the registry handed out the wrong one — and doubling the world is worse than
        // doing nothing.
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        _windowEnd = DateTimeOffset.UtcNow.AddDays(-1);
        _windowStart = _windowEnd.AddDays(-900);

        await SeedAccountsAndFriendsAsync(cancellationToken);
        await SeedLocalPlayersAsync(cancellationToken);
        await SeedSavedLayoutsAsync(cancellationToken);

        var plans = new List<GamePlan>();
        plans.AddRange(PlanAdversaryCoverage());
        plans.AddRange(PlanFavouriteFoeExtras());
        plans.AddRange(PlanNoAdversaryGames());
        plans.AddRange(PlanEventNights());
        plans.AddRange(PlanSavedLayoutGames());
        plans.AddRange(PlanRecentNights());
        AttachScenarios(plans);
        plans.AddRange(PlanCoverageFixups());
        plans.AddRange(PlanDrafts());

        foreach (var plan in plans)
            await SendGameAsync(plan, cancellationToken);
    }

    // ---------------------------------------------------------------- accounts

    private async Task SeedAccountsAndFriendsAsync(CancellationToken cancellationToken)
    {
        // Fresh ExpansionId instances per user: they are owned entities, and EF requires each
        // owner to hold its own — the shared instances in GameData must never be attached.
        static List<ExpansionId> Expansions(int count) =>
            GameData.Expansions.Take(count).Select(e => new ExpansionId(e.Id.Value)).ToList();
        var everything = GameData.Expansions.Count;

        // The demo account owns everything — the pickers should show the whole catalog.
        db.Users.Add(CreateUser(DemoSandbox.DemoUserId, DemoSandbox.DemoUserEmail,
            DemoSandbox.DemoUserNickname, Expansions(everything), _windowStart.AddDays(-14)));
        db.Users.Add(CreateUser(PetraUserId, "petra@spirit-island-companion.local",
            "Petra", Expansions(everything), _windowStart.AddDays(-7)));
        db.Users.Add(CreateUser(JonasUserId, "jonas@spirit-island-companion.local",
            "Jonas", Expansions(3), _windowStart.AddDays(30)));
        db.Users.Add(CreateUser(MayaUserId, "maya@spirit-island-companion.local",
            "Maya", Expansions(3), _windowEnd.AddDays(-20)));

        db.Friendships.Add(CreateFriendship(DemoSandbox.DemoUserId, PetraUserId, accepted: true));
        db.Friendships.Add(CreateFriendship(JonasUserId, DemoSandbox.DemoUserId, accepted: true));
        // Petra and Jonas know each other too, so mixed tables validate whoever owns the game.
        db.Friendships.Add(CreateFriendship(PetraUserId, JonasUserId, accepted: true));
        // Left pending on purpose: the People page gets an incoming request to show off.
        db.Friendships.Add(CreateFriendship(MayaUserId, DemoSandbox.DemoUserId, accepted: false));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(
        Guid id, string email, string nickname, List<ExpansionId> expansions, DateTimeOffset registered)
    {
        var settings = UserSettings.Create(new UserSettingsId(Guid.NewGuid()), expansions);
        return User.Create(
            new UserId(id),
            Email.Create(email).Value,
            Nickname.Create(nickname).Value,
            settings,
            registered);
    }

    private static Friendship CreateFriendship(Guid requester, Guid addressee, bool accepted)
    {
        var friendship = Friendship.Create(
            new FriendshipId(Guid.NewGuid()), new UserId(requester), new UserId(addressee)).Value;
        if (accepted)
            friendship.Accept();
        return friendship;
    }

    private async Task SeedLocalPlayersAsync(CancellationToken cancellationToken)
    {
        foreach (var name in new[] { "Anna", "Marek", "Elena" })
            EnsureSuccess(await mediator.Send(
                new CreatePlayerCommand(name, DemoSandbox.DemoUserId), cancellationToken), $"player {name}");

        var players = await mediator.Send(new ListPlayersQuery(DemoSandbox.DemoUserId), cancellationToken);
        _localPlayerIds = players.Value.Select(p => p.Id).ToList();
    }

    private async Task SeedSavedLayoutsAsync(CancellationToken cancellationToken)
    {
        EnsureSuccess(await mediator.Send(new SaveIslandLayoutCommand(
                TwinBaysLayoutId, DemoSandbox.DemoUserId, "Twin Bays", TwinBaysGeometry,
                At(0.55)), cancellationToken),
            "layout Twin Bays");
        EnsureSuccess(await mediator.Send(new SaveIslandLayoutCommand(
                SerpentCoilLayoutId, DemoSandbox.DemoUserId, "Serpent Coil", SerpentCoilGeometry,
                At(0.7)), cancellationToken),
            "layout Serpent Coil");
    }

    // ---------------------------------------------------------------- planning

    /// <summary>One game as the generator intends it, before the command is assembled.</summary>
    private sealed class GamePlan
    {
        public Guid OwnerId = DemoSandbox.DemoUserId;
        public DateTimeOffset StartedAt;
        public required List<GamePlayerDto> Seats;
        public List<GameAdversaryDto> Adversaries = [];
        public string? ScenarioId;
        public required string IslandSetupId;
        public bool Thematic;
        public bool ExtraBoard;
        public string? ExtraBoardId;
        public int DifficultyModifier;
        public string? Note;
        public string? LayoutJson;
        public Guid? SavedLayoutId;
        public bool IsDraft;
        public bool Win;
        public int ScoreModifier;

        /// <summary>Scenario slots are raffled after planning; special shapes opt out.</summary>
        public bool AcceptsScenario = true;
    }

    /// <summary>
    /// The backbone: every adversary at every level, twice, spread so the low levels come
    /// early and the high ones late — a group that climbed, not a spreadsheet that looped.
    /// </summary>
    private List<GamePlan> PlanAdversaryCoverage()
    {
        var plans = new List<GamePlan>();
        foreach (var adversary in GameData.Adversaries)
        {
            foreach (var mode in adversary.Modes)
            {
                for (var repeat = 0; repeat < 2; repeat++)
                {
                    var fraction = Math.Clamp(
                        (mode.Level + repeat * 0.45 + Jitter(0.7)) / 6.9, 0.0, 0.97);
                    var plan = PlanTable(fraction);
                    plan.Adversaries.Add(new GameAdversaryDto(adversary.Id.Value, mode.Level));
                    plans.Add(plan);
                }
            }
        }

        return plans;
    }

    /// <summary>A few favourite foes get extra mid-level bouts so "most faced" has a shape.</summary>
    private List<GamePlan> PlanFavouriteFoeExtras()
    {
        (string Adversary, int Level)[] extras =
        [
            (Adversaries.England.Value, 2), (Adversaries.England.Value, 3),
            (Adversaries.England.Value, 3), (Adversaries.England.Value, 4),
            (Adversaries.Sweden.Value, 2), (Adversaries.Sweden.Value, 3), (Adversaries.Sweden.Value, 4),
            (Adversaries.France.Value, 2), (Adversaries.France.Value, 3),
        ];

        var plans = new List<GamePlan>();
        foreach (var (adversaryId, level) in extras)
        {
            var plan = PlanTable(0.45 + Jitter(0.5));
            plan.Adversaries.Add(new GameAdversaryDto(adversaryId, level));
            plans.Add(plan);
        }

        return plans;
    }

    /// <summary>
    /// Games with no adversary at all: pure scenarios, quiet open-island evenings, thematic
    /// outings and hand-built islands — the shapes an adversary-only generator would miss.
    /// </summary>
    private List<GamePlan> PlanNoAdversaryGames()
    {
        var plans = new List<GamePlan>();

        // Two guaranteed scenario-only games; two more left for the scenario raffle to find.
        var ritual = PlanTable(_rng.NextDouble());
        ritual.ScenarioId = "rituals-of-terror";
        plans.Add(ritual);
        var diversity = PlanTable(_rng.NextDouble());
        diversity.ScenarioId = "a-diversity-of-spirits";
        plans.Add(diversity);
        for (var i = 0; i < 2; i++)
            plans.Add(PlanTable(_rng.NextDouble()));

        for (var i = 0; i < 4; i++)
        {
            var quiet = PlanTable(_rng.NextDouble());
            quiet.AcceptsScenario = false;
            plans.Add(quiet);
        }

        foreach (var players in new[] { 2, 3 })
            plans.Add(PlanTable(_rng.NextDouble(), forcePlayers: players, thematic: true));

        var palm = PlanTable(0.4 + Jitter(0.3), forcePlayers: 2, customGeometry: OpenPalmGeometry);
        palm.Note = "Tried a shape of our own in the playground — the open palm.";
        plans.Add(palm);

        var ring = PlanTable(0.75 + Jitter(0.2), forcePlayers: 4, customGeometry: BrokenRingGeometry);
        ring.Note = "The broken ring. Never again — every land borders every other land.";
        plans.Add(ring);

        return plans;
    }

    /// <summary>The big tables: whole-group evenings a small generator wouldn't roll.</summary>
    private List<GamePlan> PlanEventNights()
    {
        var crab = PlanTable(0.62, forcePlayers: 5);
        crab.Adversaries.Add(new GameAdversaryDto(Adversaries.England.Value, 3));
        crab.Note = "Anniversary game — all five of us against the crown.";
        crab.AcceptsScenario = false;

        var star = PlanTable(0.85, forcePlayers: 6);
        star.Adversaries.Add(new GameAdversaryDto(Adversaries.Sweden.Value, 2));
        star.Note = "Six boards. The table was not big enough. We managed.";
        star.AcceptsScenario = false;

        var thematicSix = PlanTable(0.93, forcePlayers: 6, thematic: true);
        thematicSix.Note = "The whole island, exactly as it is. Took all evening.";

        return [crab, star, thematicSix];
    }

    /// <summary>Games played on the layouts saved in the library, so the two reference each other.</summary>
    private List<GamePlan> PlanSavedLayoutGames()
    {
        var plans = new List<GamePlan>();

        foreach (var fraction in new[] { 0.6, 0.8 })
        {
            var plan = PlanTable(fraction + Jitter(0.05), forcePlayers: 2, customGeometry: TwinBaysGeometry);
            plan.SavedLayoutId = TwinBaysLayoutId;
            plans.Add(plan);
        }

        foreach (var fraction in new[] { 0.72, 0.9 })
        {
            var plan = PlanTable(fraction + Jitter(0.05), forcePlayers: 3, customGeometry: SerpentCoilGeometry);
            plan.SavedLayoutId = SerpentCoilLayoutId;
            plans.Add(plan);
        }

        plans[0].Adversaries.Add(new GameAdversaryDto(Adversaries.Russia.Value, 1));
        plans[2].Adversaries.Add(new GameAdversaryDto(Adversaries.England.Value, 2));
        plans[1].Note = "Twin Bays again — this layout makes the coasts brutal.";

        return plans;
    }

    /// <summary>
    /// The fortnight just gone. Every other planner places its games by a fraction of the whole
    /// window, and the rolls rarely reach the far end of it — so without this the newest finished
    /// game sits weeks back, and a visitor arriving at the dashboard reads "recent games" from
    /// last month and this month's activity as a flat line. Anchored like everything else, so
    /// these move with the template.
    /// </summary>
    private List<GamePlan> PlanRecentNights()
    {
        // The last two weeks of a 900-day window. The drafts sit past the end of this, in the
        // last few days, so the story reads as a group that played and left one game unfinished.
        var plans = new List<GamePlan>();
        foreach (var fraction in new[] { 0.9845, 0.9875, 0.9905, 0.9935, 0.9955 })
            plans.Add(PlanTable(fraction));

        plans[0].Adversaries.Add(new GameAdversaryDto(Adversaries.England.Value, 4));
        plans[2].Adversaries.Add(new GameAdversaryDto(Adversaries.France.Value, 3));
        plans[3].Adversaries.Add(new GameAdversaryDto(Adversaries.Sweden.Value, 3));
        plans[4].Note = "Last Friday. Best game we have had in months.";

        return plans;
    }

    /// <summary>
    /// Raffles the scenario list over the planned games: every scenario at least once, the
    /// group's favourites more than once, some alone and some stacked on an adversary.
    /// </summary>
    private void AttachScenarios(List<GamePlan> plans)
    {
        var queue = GameData.Scenarios.Select(s => s.Id.Value).ToList();
        queue.AddRange([
            "blitz", "blitz", "guard-the-isles-heart", "second-wave", "second-wave", "ward-the-shores",
        ]);

        var candidates = plans.Where(p => p.AcceptsScenario && p.ScenarioId is null).ToList();
        foreach (var scenarioId in queue)
        {
            if (candidates.Count == 0)
                break;
            var plan = candidates[_rng.Next(candidates.Count)];
            plan.ScenarioId = scenarioId;
            candidates.Remove(plan);
        }
    }

    /// <summary>
    /// The guarantee pass: solo evenings for any spirit still played fewer than twice and any
    /// aspect never tried, so "almost everything used" holds no matter how the raffles fell.
    /// </summary>
    private List<GamePlan> PlanCoverageFixups()
    {
        var plans = new List<GamePlan>();

        foreach (var spirit in GameData.Spirits)
        {
            while (_spiritSeatCounts.GetValueOrDefault(spirit.Id.Value) < 2)
                plans.Add(PlanTable(_rng.NextDouble(), forcePlayers: 1, forceSpirit: spirit.Id.Value));
        }

        foreach (var aspect in GameData.Aspects.Where(a => !_aspectsUsed.Contains(a.Id.Value)))
            plans.Add(PlanTable(_rng.NextDouble(), forcePlayers: 1,
                forceSpirit: aspect.SpiritId.Value, forceAspect: aspect.Id.Value));

        return plans;
    }

    /// <summary>
    /// Unfinished business: drafts sitting on the dashboard, started in the last week. Each one
    /// is named after an adversary or a scenario, because the dashboard titles a game by what it
    /// was fought against and these three cards are the first thing a visitor reads — a draft
    /// with neither would sit at the top of the demo as "Standard game".
    /// </summary>
    private List<GamePlan> PlanDrafts()
    {
        var duel = PlanTable(0.999, forcePlayers: 2);
        duel.Adversaries.Add(new GameAdversaryDto(Adversaries.HabsburgMining.Value, 4));
        duel.IsDraft = true;
        duel.Note = "Paused mid-game — invaders about to ravage the east.";

        var soloThematic = PlanTable(0.998, forcePlayers: 1, thematic: true);
        soloThematic.ScenarioId = "rituals-of-terror";
        soloThematic.IsDraft = true;

        var coil = PlanTable(0.997, forcePlayers: 3, customGeometry: SerpentCoilGeometry);
        coil.SavedLayoutId = SerpentCoilLayoutId;
        coil.Adversaries.Add(new GameAdversaryDto(Adversaries.Scotland.Value, 2));
        coil.IsDraft = true;
        coil.Note = "Serpent Coil rematch — to be finished on Friday.";

        return [duel, soloThematic, coil];
    }

    // ------------------------------------------------------------- table maker

    /// <summary>
    /// Lays one table: who sits down, which spirits and boards they take, and on which island.
    /// Everything downstream of "when and against what" lives here so every planner rolls the
    /// same kind of evening.
    /// </summary>
    private GamePlan PlanTable(
        double fraction,
        int? forcePlayers = null,
        bool thematic = false,
        string? customGeometry = null,
        string? forceSpirit = null,
        string? forceAspect = null)
    {
        var playerCount = forcePlayers ?? _rng.Next(100) switch
        {
            < 32 => 1,
            < 66 => 2,
            < 88 => 3,
            _ => 4,
        };

        // Some tables are Petra's: games the demo account is merely seated at, so the games
        // list and the statistics show shared history and not just owned history.
        var petraOwned = forcePlayers is null && customGeometry is null && !thematic
                         && playerCount is >= 2 and <= 3 && _rng.Next(100) < 9;

        var participants = PickParticipants(playerCount, petraOwned);
        var extraBoard = !thematic && customGeometry is null && forcePlayers is null
                         && playerCount <= 2 && _rng.Next(100) < 5;

        var boardCount = playerCount + (extraBoard ? 1 : 0);
        var boards = PickBoards(boardCount, thematic);

        var seats = new List<GamePlayerDto>();
        for (var seat = 0; seat < playerCount; seat++)
        {
            var spirit = seat == 0 && forceSpirit is not null ? forceSpirit : PickSpirit(seats);
            var aspect = seat == 0 && forceAspect is not null ? forceAspect : PickAspect(spirit);
            _spiritSeatCounts[spirit] = _spiritSeatCounts.GetValueOrDefault(spirit) + 1;
            if (aspect is not null)
                _aspectsUsed.Add(aspect);

            var who = participants[seat];
            seats.Add(new GamePlayerDto(spirit, aspect, boards[seat], who.UserId, who.PlayerId));
        }

        var setupId = customGeometry is not null
            ? IslandSetups.CustomIdFor(boardCount)
            : PickSetup(boardCount, thematic);

        return new GamePlan
        {
            OwnerId = petraOwned ? PetraUserId : DemoSandbox.DemoUserId,
            StartedAt = At(fraction),
            Seats = seats,
            IslandSetupId = setupId,
            Thematic = thematic,
            ExtraBoard = extraBoard,
            ExtraBoardId = extraBoard ? boards[playerCount] : null,
            LayoutJson = customGeometry,
            DifficultyModifier = _rng.Next(100) < 5 ? _rng.Next(1, 3) : 0,
            Note = _rng.Next(100) < 16 ? Notes[_rng.Next(Notes.Length)] : null,
            ScoreModifier = _rng.Next(100) < 4 ? _rng.Next(-3, 4) : 0,
            AcceptsScenario = customGeometry is null && !thematic,
        };
    }

    private readonly record struct Participant(Guid? UserId, Guid? PlayerId);

    private List<Participant> PickParticipants(int playerCount, bool petraOwned)
    {
        if (petraOwned)
        {
            // Seat order is owner first, and everyone must be the owner's friend — which for
            // Petra means the other accounts, not the demo account's local players.
            List<Participant> table =
                [new(PetraUserId, null), new(DemoSandbox.DemoUserId, null), new(JonasUserId, null)];
            return table.Take(playerCount).ToList();
        }

        var others = new List<Participant>
        {
            new(PetraUserId, null),
            new(JonasUserId, null),
        };
        others.AddRange(_localPlayerIds.Select(id => new Participant(null, id)));

        var result = new List<Participant> { new(DemoSandbox.DemoUserId, null) };
        while (result.Count < playerCount)
        {
            var pick = others[_rng.Next(others.Count)];
            others.Remove(pick);
            result.Add(pick);
        }

        return result;
    }

    /// <summary>
    /// A seat's spirit: first whatever is still short of its two guaranteed plays, then the
    /// group's favourites, then anyone. Never a spirit already at the table.
    /// </summary>
    private string PickSpirit(List<GamePlayerDto> taken)
    {
        var atTable = taken.Select(s => s.SpiritId).ToHashSet();

        var underplayed = GameData.Spirits
            .Select(s => s.Id.Value)
            .Where(s => !atTable.Contains(s) && _spiritSeatCounts.GetValueOrDefault(s) < 2)
            .ToList();
        if (underplayed.Count > 0 && _rng.Next(100) < 60)
            return underplayed[_rng.Next(underplayed.Count)];

        if (_rng.Next(100) < 55)
        {
            var free = FavouriteSpirits.Where(s => !atTable.Contains(s)).ToList();
            if (free.Count > 0)
                return free[_rng.Next(free.Count)];
        }

        var pool = GameData.Spirits.Select(s => s.Id.Value).Where(s => !atTable.Contains(s)).ToList();
        return pool[_rng.Next(pool.Count)];
    }

    /// <summary>An aspect for the seat: unseen ones first, then an occasional repeat.</summary>
    private string? PickAspect(string spiritId)
    {
        var aspects = GameData.GetAspectsForSpirit(new SpiritId(spiritId));
        if (aspects.Count == 0)
            return null;

        var unseen = aspects.Where(a => !_aspectsUsed.Contains(a.Id.Value)).ToList();
        if (unseen.Count > 0 && _rng.Next(100) < 65)
            return unseen[_rng.Next(unseen.Count)].Id.Value;

        return _rng.Next(100) < 30 ? aspects[_rng.Next(aspects.Count)].Id.Value : null;
    }

    private List<string> PickBoards(int boardCount, bool thematic)
    {
        // On the thematic island a board is a region, not a choice — slot order is fixed.
        if (thematic && ThematicIslandBoards.For(new IslandSetupId($"thematic-{boardCount}p")) is { } regions)
            return regions.Select(r => r.Value).ToList();

        var pool = GameData.Boards.Select(b => b.Id.Value).ToList();
        var picked = new List<string>();
        while (picked.Count < boardCount)
        {
            var board = pool[_rng.Next(pool.Count)];
            pool.Remove(board);
            picked.Add(board);
        }

        return picked;
    }

    private string PickSetup(int boardCount, bool thematic)
    {
        if (thematic)
            return IslandSetups.ThematicFor(boardCount)!.Id.Value;

        var options = GameData.PublishedIslandSetups
            .Where(s => s.NumberOfPlayers == boardCount && !s.IsThematic)
            .ToList();
        return options[_rng.Next(options.Count)].Id.Value;
    }

    // ------------------------------------------------------------ materialising

    private async Task SendGameAsync(GamePlan plan, CancellationToken cancellationToken)
    {
        if (plan.IsDraft)
        {
            EnsureSuccess(await mediator.Send(new DraftGameCommand(
                    plan.OwnerId, plan.StartedAt, plan.IslandSetupId, plan.ExtraBoard, plan.ExtraBoardId,
                    plan.Thematic, plan.DifficultyModifier, plan.Seats, plan.Adversaries, plan.ScenarioId,
                    plan.Note, plan.LayoutJson, plan.SavedLayoutId), cancellationToken),
                $"draft on {plan.IslandSetupId}");
            return;
        }

        var difficulty = EstimateDifficulty(plan);
        var winChance = Math.Clamp(0.88 - 0.058 * difficulty, 0.18, 0.92);
        plan.Win = _rng.NextDouble() < winChance;

        EnsureSuccess(await mediator.Send(new CreateGameCommand(
                plan.OwnerId, plan.StartedAt, plan.IslandSetupId, plan.ExtraBoard, plan.ExtraBoardId,
                plan.Thematic, plan.DifficultyModifier, plan.Seats, plan.Adversaries, plan.ScenarioId,
                MakeResult(plan, difficulty), plan.Note, plan.LayoutJson, plan.SavedLayoutId),
            cancellationToken),
            $"game on {plan.IslandSetupId} vs [{string.Join(", ", plan.Adversaries.Select(a => $"{a.AdversaryId} {a.Level}"))}]");
    }

    /// <summary>
    /// What the setup adds up to, by the same tables the app scores with — so the win chance
    /// (and the plausibility of the numbers below) tracks the difficulty a visitor will see.
    /// </summary>
    private static int EstimateDifficulty(GamePlan plan)
    {
        var adversaries = plan.Adversaries.Sum(a =>
            GameData.Adversaries.First(x => x.Id.Value == a.AdversaryId)
                .Modes.First(m => m.Level == a.Level).Difficulty);
        var scenario = plan.ScenarioId is null
            ? 0
            : GameData.Scenarios.First(s => s.Id.Value == plan.ScenarioId).Difficulty;

        return adversaries + scenario + plan.DifficultyModifier
               + (plan.ExtraBoard ? GameRestrictions.ExtraBoardDifficultyBonus : 0)
               + (plan.Thematic ? GameRestrictions.ThematicMapsDifficultyBonus : 0);
    }

    private GameResultDto MakeResult(GamePlan plan, int difficulty)
    {
        var seats = plan.Seats.Count;
        var minutes = Math.Clamp(
            40 + 27 * seats + 5 * difficulty + _rng.Next(-12, 29), 35, 300);

        // Winning tables end with the island in better shape; terror tends higher on wins
        // because a terror victory is how many hard games are actually won.
        var terrorRoll = _rng.Next(100);
        var terror = plan.Win
            ? terrorRoll switch { < 15 => TerrorLevel.First, < 60 => TerrorLevel.Second, < 90 => TerrorLevel.Third, _ => TerrorLevel.Max }
            : terrorRoll switch { < 45 => TerrorLevel.First, < 85 => TerrorLevel.Second, _ => TerrorLevel.Third };

        return new GameResultDto(
            plan.Win,
            TimeSpan.FromMinutes(minutes),
            Cards: plan.Win ? _rng.Next(7, 15) : _rng.Next(4, 12),
            terror,
            Blight: plan.Win ? _rng.Next(1, 6) : _rng.Next(3, 10),
            Dahan: plan.Win ? _rng.Next(3, 13) : _rng.Next(0, 7),
            ScoreModifier: plan.ScoreModifier);
    }

    // --------------------------------------------------------------- utilities

    private DateTimeOffset At(double fraction)
    {
        var moment = _windowStart + (_windowEnd - _windowStart) * Math.Clamp(fraction, 0, 1);
        // Evenings, like real board game nights — and a stable hour keeps ordering sane.
        return new DateTimeOffset(moment.Year, moment.Month, moment.Day,
            17 + _rng.Next(0, 4), 15 * _rng.Next(0, 4), 0, TimeSpan.Zero);
    }

    private double Jitter(double magnitude) => (_rng.NextDouble() - 0.5) * magnitude;

    private static void EnsureSuccess(Result result, string what)
    {
        if (result.IsFailure)
            throw new InvalidOperationException($"Demo seeding failed on {what}: {result.Error.Message}");
    }
}
