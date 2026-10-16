---
paths:
  - "src/Infrastructure/**"
---

# Infrastructure layer

EF Core on SQL Server for the real database and in-memory SQLite for the demo sandboxes, plus the
seeder. `AppDbContext` implements `Application.Data.IAppDbContext`, the only door the application
layer has to the database.

## Mapping value objects

Every single-value value object is one column, declared once by type in
`AppDbContext.ConfigureConventions`, in the group it belongs to:

```csharp
Map<CustomIslandLayoutId, Guid>();                           // Identities.
Map<SpiritId, string>();                                     // References into the static catalogue.
Map<AdversaryLevel, int>();                                  // Counts and scores.
Map<Nickname, string>().HaveMaxLength(Nickname.MaxLength);   // Text, cut to the length the domain accepts.
```

- A new persisted value object is one `Map<>()` line in the matching group. Never configure the
  same value object property by property in an entity configuration.
- `ValueObjectConverter<TValueObject, TValue>` reads `.Value` and rebuilds through the private
  constructor that takes `TValue`. A value object without that constructor fails at model build,
  which is the intended guard.
- Never map a value object as an owned type: owned types are tracked by identity, and two seats
  sharing one `UserId` instance would read as the value moving between them.
- A value with several parts is a `ComplexProperty` (`GameResult` holding `GameOutcome`); fix the
  column names explicitly when a model re-nesting must not change the schema.
- A collection of ids on an aggregate is a `PrimitiveCollection` through its backing field
  (`UserConfiguration` → `_ownedExpansions`), so a read-side guarantee on the property never leaks
  into storage.

## Entity configurations

- One `internal sealed class <Aggregate>Configuration : IEntityTypeConfiguration<T>` per aggregate
  in `Database/Configurations`, applied by assembly scan.
- `builder.HasKey(x => x.Id);` first. Child entities via `OwnsMany`, table
  `$"{nameof(Game)}_{nameof(GamePlayer)}"`, `WithOwner().HasForeignKey(nameof(GameId))`,
  `HasKey(x => x.Id)`.
- Enums are stored as strings: `.HasConversion<string>().HasMaxLength(20)`.
- Add an index for each query shape the application layer uses, with a comment naming that query
  (`HasIndex(x => x.TargetUserId)` for the merge inbox).
- No foreign keys between aggregates. A reference across aggregates (`Game.CustomLayoutId`) is a
  plain column; guarding the delete is the handler's job, and the comment in the configuration
  says which handler.

## Migrations

- Live in `Database/Migrations`, created with:

  ```powershell
  dotnet ef migrations add <IntentName> --project src/Infrastructure --startup-project src/Infrastructure --context AppDbContext
  ```

  `AppDbContextFactory` is the design-time factory, so no running database is needed. `--context`
  is required because `DemoAppDbContext` is a second context in the assembly; it is never the
  target of a migration (the demo creates its schema with `EnsureCreated`).
- Name the migration for the intent, not the mechanics: `NestGameOutcome`,
  `RemoveRejectedFriendships`, `NormalizeEmailCase`, `MapValueObjectsAsColumns`.
- Read the generated `Up`. A model change that only re-nests or renames in C# must produce no
  schema change. Data fixes are written by hand in the migration with a comment on why.
- Never edit `*.Designer.cs` or `AppDbContextModelSnapshot.cs` by hand. Migrations run at app
  start (`MigrateAsync` in `WebApp/Program.cs`), so a merged migration applies on the next run.

## Both providers must work

Everything the application layer queries runs on SQL Server in production and on SQLite in the
demo. Provider adjustments go in `DemoAppDbContext.ConfigureConventions` and nowhere else (today:
`DateTimeOffset` as binary, so SQLite can order by it). If a query translates on one provider
only, reshape the query in the application layer; do not fork the model.

## The demo sandbox

- `DemoSandboxRegistry` keeps one in-memory SQLite database per visitor, copied from a seeded
  template. `AddInfrastructure` resolves `IAppDbContext` to a sandbox context whenever
  `IDemoSessionLocator.SandboxId` is set; nothing above or below knows the difference.
- `IDemoSessionLocator` lives here, beside the registry that consumes it. The web host implements
  it; the application layer never references it.
- `DemoDataSeeder` writes history **through the application's own commands**
  (`mediator.Send(new CreateGameCommand(...))`), so every seeded difficulty and score is one the
  app's rules produced. Seed a new feature the same way; never insert aggregates directly.
- Fixed identities use the `d3300000-0000-4000-8000-…` GUID range (`DemoSandbox`,
  `DemoDataSeeder`). The timeline is re-anchored to "yesterday" on every template build and the
  rolls are deterministic, so the demo always looks freshly played and always looks the same.
- `AppDbContext` itself is registered by the host (Aspire's `AddSqlServerDbContext`);
  `AddInfrastructure` only decides which context an `IAppDbContext` resolves to.
