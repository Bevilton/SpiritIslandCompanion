---
paths:
  - "src/Domain/**"
---

# Domain layer

`Domain` has no package references and knows nothing about EF Core, MediatR, HTTP or the UI. It
holds the aggregates, their value objects, the static game catalogue, `Result`/`Error`, and two
static calculators.

## Folder and namespace layout

```
Models/<Aggregate>/          <Aggregate>.cs plus Entities/, ValueObjects/, Enums/
                             — everything in namespace Domain.Models.<Aggregate>
Models/Static/<Kind>/        the catalogue record and its id (namespace Domain.Models.Static)
Models/Static/Data/          the catalogue contents and GameData (namespace Domain.Models.Static.Data)
Errors/DomainErrors.cs       every Error, nested in a static class per aggregate
Results/                     Result, Result<T>, Error, ValidationResult
Services/                    DifficultyCalculator, ScoreCalculator
Primitives/                  Entity, AggregateRoot, Identifier, ValueObject
```

Aggregates: `Game`, `User`, `Player` (a local player), `Friendship`, `PlayerMergeRequest`,
`CustomIslandLayout`. Catalogue kinds: `Spirit`, `Aspect`, `Adversary`, `Scenario`, `Board`,
`IslandSetup`, `Expansion`.

## Value objects

The single-value template. Copy it exactly; the EF converter depends on the shape.

```csharp
public sealed record PlayerName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; private init; }

    private PlayerName(string value)
    {
        Value = value;
    }

    public static Result<PlayerName> Create(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result.Failure<PlayerName>(DomainErrors.Player.NameRequired);
        if (trimmed.Length > MaxLength)
            return Result.Failure<PlayerName>(DomainErrors.Player.NameTooLong);

        return new PlayerName(trimmed);
    }
}
```

- `sealed record … : ValueObject`, one `Value` with `private init`, a **private constructor taking
  exactly the `Value` type** (`ValueObjectConverter` finds it by reflection), and a static `Create`
  returning `Result<T>` through `DomainErrors`.
- Numeric ranges come from `GameRestrictions`; a text limit is a `public const int MaxLength` on
  the value object itself. Text is trimmed; `Email` is also lower-cased.
- A value only a domain service produces (`Difficulty`, `Score`) has an `internal` constructor and
  no `Create`: the calculator clamps it, so there is no failure to report and no factory to report
  it through.
- An optional value gets `CreateOptional(string?)` returning `Result<T?>` (see `GameNote`) so the
  null branch is written once.
- Identifier: `public sealed record GameId(Guid Value) : Identifier(Value);` — one line, no check.
  Catalogue id: `public sealed record SpiritId(string Value) : ValueObject;`
- Multi-part values (`GameOutcome`, `GameResult`, `IslandChoice`) are records too; a persisted one
  carries the EF empty-constructor block shown below.
- A persisted value object also needs a `Map<>()` line in `AppDbContext.ConfigureConventions`.

## Aggregates and entities

```csharp
public class Player : AggregateRoot<PlayerId>
{
    public PlayerName Name { get; private set; }      // changes later → private set
    public UserId CreatedBy { get; private init; }    // fixed at creation → private init

    private Player(PlayerId id, PlayerName name, UserId createdBy) : base(id)
    {
        Name = name;
        CreatedBy = createdBy;
    }

    public static Player Create(PlayerId id, PlayerName name, UserId createdBy) =>
        new(id, name, createdBy);

    public void Rename(PlayerName name) => Name = name;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private Player()
    {
    }
#pragma warning restore
}
```

- Constructor private; creation through `static Create(...)`, which returns the aggregate, or
  `Result<T>` when creation itself can be refused (`Friendship.Create` refuses befriending
  yourself).
- Behaviour is a method named for what it does at the table: `Complete`, `Accept`,
  `HandSeatsToUser`, `UnseatLocalPlayer`. It returns `Result` when a rule can refuse it and `void`
  when nothing can. Its summary says who may call it and under which condition.
- Collections: a private `List<T> _items = [];` exposed as `IReadOnlyCollection<T>` via
  `.AsReadOnly()`, mutated only inside the aggregate.
- Child entities (`GamePlayer`, `PlayedAdversary`) are `Entity<TId>` with a private constructor,
  static factories named for the case (`ForUser`, `ForLocalPlayer`), ids minted inside with
  `new XId(Guid.NewGuid())`, and `internal` mutators so only the root changes them.
- The EF empty-constructor block is copied verbatim onto every persisted class: aggregate, child
  entity, complex value object.
- Timestamps are passed in (`DateTimeOffset createdAt`); the domain never reads a clock.
- Arguments that belong together travel as one record (`IslandChoice`) rather than as loose
  parameters that can be set one without the other.
- `GameSetup` is a validated argument object: it exists only through its own `Create`, is not part
  of any aggregate, and `Game.Create` takes it instead of re-checking the table. Use the same shape
  for any future "this input has passed every rule" guarantee.
- No domain events, no `INotification`, no `RaiseDomainEvent`. A cross-aggregate effect is
  orchestrated by the application handler that owns the use case (`RemoveFriendHandler` calls
  `SharedGameSplitter`).

## Errors and results

- `Error` is `sealed record Error(string Code, string Message)`. There is no error-type enum; the
  code and the message say what kind of failure it is.
- Every error is `public static Error Name { get; } = new("Aggregate.Name", "Sentence.");` inside
  `DomainErrors.<Aggregate>`. The message is shown to the user unchanged, and a limit in it is
  interpolated from the constant that enforces it (`$"… at most {PlayerName.MaxLength} characters."`).
- Domain methods return `Result` / `Result<T>`. `Result<T>` converts implicitly from `T`, so
  `return new PlayerName(trimmed);` is the success path. Inside `Game` write `Results.Result`,
  because the aggregate has a `Result` property.
- `ValidationResult` is produced only by the application pipeline; the domain never builds one.

## The static catalogue

- A kind is a positional record: `public sealed record Spirit(SpiritId Id, string Name, ExpansionId ExpansionId);`
  Only what the rules need lives here: name, expansion, difficulty, levels, board count, the
  thematic and custom flags. Colours, monograms and flavour text belong in `WebApp/Catalogue`.
- Contents are `public static IReadOnlyList<T> All { get; } = [ … ];` in `Static/Data/<Kinds>.cs`,
  with well-known ids as `public static readonly XId Name = new("kebab-id");`
  (`Expansions.BaseGame`, `Spirits.LightningsSwiftStrike`). `GameData` is the facade everything
  else reads.
- Ids are lower-kebab strings (`"branch-and-claw"`, `"thematic-4p"`, `"board-a"`) and are persisted
  on games; never rename one.
- The domain does not narrow a catalogue to what a user owns. That is a presentation question,
  answered by `OwnedCatalogue` in `WebApp`.

## Domain services

Static classes with two entry points: `Calculate(...)` takes value objects and returns one, and
`Compute(...)` works on raw ints for UI previews over unsaved form state. Weights are
`public const int`s named so a UI breakdown can quote them instead of repeating them.
