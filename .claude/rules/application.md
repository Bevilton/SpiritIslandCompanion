---
paths:
  - "src/Application/**"
---

# Application layer

MediatR requests, FluentValidation validators, and handlers that load aggregates, call their
methods and add or remove them through `IAppDbContext`. One folder per feature area under
`Features/`: `Friendships`, `Games`, `IslandLayouts`, `People`, `PlayerMerges`, `Players`,
`Statistics`, `Users`.

## One file per operation

`Features/<Area>/<VerbNoun>.cs` holds, in this order: the request record, its response record(s),
the validator, the handler. Nothing else goes in the file.

```csharp
/// <summary>Only the game's owner can delete it — for anyone else it isn't found.</summary>
public sealed record DeleteGameCommand(Guid GameId, Guid UserId) : ICommand;

internal sealed class DeleteGameHandler(IAppDbContext db) : ICommandHandler<DeleteGameCommand>
{
    public async Task<Result> Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        var ownerId = new UserId(request.UserId);
        var game = await db.Games
            .FirstOrDefaultAsync(
                g => g.Id == new GameId(request.GameId) && g.OwnerId == ownerId,
                cancellationToken);

        if (game is null)
            return Result.Failure(DomainErrors.Game.NotFound);

        db.Games.Remove(game);
        return Result.Success();
    }
}
```

Naming:

- Commands: `<Verb><Noun>Command : ICommand` (changes state, reports success only) or
  `: ICommand<TResponse>` — rare, only when the caller needs something back (`SyncUserCommand` →
  `SyncUserResponse`). Name the command for the one thing it does: `SetNickname`,
  `SetOwnedExpansions`, `WithdrawMergeRequest`; never `UpdateUser`.
- Queries: `Get<Noun>Query : IQuery<Get<Noun>Response>` for one thing,
  `List<Nouns>Query : IQuery<List<List<Nouns>Response>>` for many. A query changes nothing.
- Handlers: `<Request>Handler`, `internal sealed`, primary constructor, implementing
  `ICommandHandler<…>` or `IQueryHandler<…>`.
- Validators: `<Request>Validator`, `internal sealed`, discovered by assembly scan.
- Response and DTO records are `public sealed record`s of primitives (`Guid`, `string`, `int`,
  `bool?`, `TimeSpan`, `DateTimeOffset`, `List<T>`) and the enums the UI shares (`TerrorLevel`,
  `PlayerKind`, `PlayerMergeStatus`). Never a value object or an aggregate. Shapes shared by several
  operations live in `Features/<Area>/Dtos/`.
- The acting account is always a `Guid UserId` on the request. Nothing here reads the HTTP
  context, a principal or a claim.
- The request's `<summary>` says what it does and who may do it; `<param>` docs explain any field
  whose meaning is not obvious from its name (`ExtraBoardId`, `SavedLayoutId`).

## Validators check shape only

```csharp
RuleFor(x => x.Name)
    .NotEmpty().WithDomainError(DomainErrors.IslandLayout.NameRequired)
    .MaximumTrimmedLength(IslandLayoutName.MaxLength).WithDomainError(DomainErrors.IslandLayout.NameTooLong);
```

- Every rule ends with `.WithDomainError(DomainErrors.X.Y)`. Never `.WithMessage("…")`.
- Ranges and lengths come from `GameRestrictions` and the value object's `MaxLength`. Use
  `MaximumTrimmedLength` for names so the validator and the value object agree.
- A validator checks required, range, length and mutual exclusion — what can be pinned to a field.
  Whether an id exists in the catalogue, whether the table is legal, whether this user may act:
  those belong to the domain or to the handler.
- Rules for a sub-DTO live in a `<Dto>Validator` reused with
  `.SetValidator(new …()).When(x => x.Dto is not null)`.
- Field errors reach the UI by FluentValidation property path (`Players[0].SpiritId`); use
  `.OverridePropertyName("AssignedTo")` for a rule that spans fields.

## Handlers

- Build value objects with `X.Create(...)` and on failure return `Result.Failure<T>(x.Error)` at
  once. Build ids with `new UserId(request.UserId)`.
- Put the ownership predicate in the query and answer `NotFound` for anything the user may not
  touch. Don't load first and check ownership afterwards when the predicate can do it.
- Call the aggregate's method and return its `Result` directly
  (`return game.Complete(outcome.Value, note.Value);`). Don't pre-check what the aggregate checks;
  a comment may point at the method that owns the rule.
- `db.X.Add(...)`, `db.X.Remove(...)`, mutate. **Never** call `SaveChangesAsync`:
  `SaveChangesBehaviour` saves after a successful command and clears the tracker after every one.
- Reads use `.AsNoTracking()` — every query handler, and any lookup a command only reads. Track
  only what the command changes.
- New ids: `new GameId(Guid.NewGuid())` in the handler. The command carries the id only when the
  UI must reference the row before a round trip, and then the handler refuses an id that exists
  under another owner (`SaveIslandLayout`).
- Time: inject `TimeProvider clock` and call `clock.GetUtcNow()`.
- Pass `cancellationToken` to every EF call.
- Resolve names and counts in bulk with `Contains` over a list of ids (value-object ids translate
  to `IN`), never one query per row. Count in the database when the number is all that is needed.
- Keep every query translatable on SQL Server and SQLite: no filter inside a collection navigation
  that needs `APPLY`; `SelectMany` the seats out flat and filter afterwards (`MergeRequestInbox`).

## Shared pieces inside a feature

| Pattern | Shape | Example |
|---|---|---|
| Query shapes several readers share | `internal static class <Aggregate>Queries`, `IQueryable<T>` extensions | `GameQueries.InvolvingUser`, `FriendshipQueries.Between`, `PlayerMergeRequestQueries.Pending` |
| Projections to response records | `internal static class <Aggregate>Mappings`, `ToResponse(this …)` | `GameMappings` |
| Stateless DTO → domain builders | `internal static class <Thing>Factory` returning `Result<T>` | `GameOutcomeFactory` |
| Builders that need the database | `internal sealed class` with a primary constructor, `AddScoped` in `AddApplication` | `GameSetupFactory`, `SeatNameResolver`, `MergeRequestInbox` |
| Helpers that keep state for one operation | the same, but `AddTransient`, with a comment saying why | `SharedGameSplitter` |

Register a new helper in `ServiceExtensions.AddApplication` with a one-line comment on its
lifetime. No interface for these: a handler takes the concrete class.

## Statistics

- Count through `Tally.Of(...)`: "completed" is wins plus losses, a rate is over completed games
  only, and anything that reports a rate also reports the count it was taken over.
- Per-subject rows implement `IPlayTally` (counts) or `IWinRateTally` (counts plus the
  server-computed rate). Breakdown rows under a parent (`AspectStats`, `AdversaryLevelStats`)
  carry counts only; the rate there is the reader's to derive.
- Every reader of a user's history answers over `GameQueries.InvolvingUser`, so the list, the
  statistics and the setup facts cannot drift apart.

## Pipeline

Logging → Validation → SaveChanges, outermost first, registered in `AddApplication`. A new
behaviour is added with a comment placing it in that order and saying why it sits there.
