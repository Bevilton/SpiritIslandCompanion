# Spirit Island Companion — guide for coding agents

A Blazor Server app (.NET 10, Aspire, EF Core, Tailwind v4) for recording Spirit Island board-game
sessions and studying the results. One solution, `SpiritIslandCompanion.slnx`, six projects under
`src/`. There is no test project: a change is verified by building and by exercising it in the
running app, usually through the seeded demo account.

## Layers and the dependency direction

| Project | Holds | References |
|---|---|---|
| `Domain` | Aggregates, value objects, the static game catalogue, `Result`/`Error`, two calculators. No packages. | nothing |
| `Application` | MediatR commands, queries, handlers; FluentValidation validators; pipeline behaviours; `IAppDbContext`. | Domain |
| `Infrastructure` | `AppDbContext`, EF configurations, migrations, the SQLite demo sandbox and its seeder. | Application |
| `WebApp` | Blazor pages and components, OIDC auth, routes, Tailwind styles, presentation-only catalogue data. | Application, Infrastructure, ServiceDefaults |
| `AppHost`, `ServiceDefaults` | Aspire orchestration (SQL Server container) and the standard Aspire defaults. | — |

Dependencies point one way only. Nothing below `WebApp` knows about the signed-in user, HTTP or
presentation: the acting account reaches every command and query as a plain `Guid UserId`, and
display-only catalogue data (colours, monograms, flavour text) lives in `WebApp/Catalogue`, not in
`Domain`. `ICurrentUserService` is a `WebApp.Auth` type; `IDemoSessionLocator` is an
`Infrastructure.Demo` type. Neither may be referenced from `Application`.

## Commands

PowerShell, from the repository root unless stated.

| Task | Command |
|---|---|
| Build everything | `dotnet build SpiritIslandCompanion.slnx` |
| Run the app (Aspire; starts SQL Server in Docker) | `dotnet run --project src/AppHost` |
| Rebuild CSS on save while editing styles | `npm run watch:css` in `src/WebApp` (a normal build regenerates `wwwroot/app.css` by itself) |
| Add a migration | `dotnet ef migrations add <IntentName> --project src/Infrastructure --startup-project src/Infrastructure --context AppDbContext` |
| Regenerate island geometry after touching `tools/island-geometry` | `node tools/island-geometry/build.mjs`, then commit the generated outputs with the change |
| A fully populated account to test against | open `/demo` on the running app; it is an in-memory sandbox and never touches the real database |

## Rules that apply everywhere

1. **Business rules live in the domain.** A value object's `Create` and an aggregate's methods are
   the only place a rule is checked. Application validators check field shape only (required,
   range, length) and bind every rule to a `DomainErrors` entry. Never repeat a domain check in a
   handler or a page.
2. **Failures are `Result`, never exceptions.** Return `Result.Failure(DomainErrors.X.Y)`; the UI
   shows `Error.Message` through `FormFeedback`. Exceptions are for bugs and infrastructure faults.
3. **Every user-facing error is a `DomainErrors` entry**: code `"Aggregate.Reason"`, message a plain
   sentence ready to show, any limit in it interpolated from the constant that enforces it.
4. **A command is the unit of work.** Handlers never call `SaveChangesAsync`; `SaveChangesBehaviour`
   saves on success and clears the tracker after every command. Queries read with `AsNoTracking()`.
5. **Ownership is checked in the query**, with the owner predicate in the `Where`, and anyone else
   gets `NotFound`. A stranger never learns that a row exists.
6. **Time comes from `TimeProvider`** (`clock.GetUtcNow()`), never from `DateTimeOffset.UtcNow`,
   outside the demo registry.
7. **A value object is one column**, mapped once by type in `AppDbContext.ConfigureConventions`. A
   new persisted value object gets a `Map<>()` line there, not per-property configuration.
8. **Every EF query must translate on SQL Server and on SQLite** (the demo). No filter inside a
   collection navigation that needs `APPLY`; flatten with `SelectMany` and filter afterwards.
9. **One spelling per thing.** Routes in `AppRoutes` or the `*Link` builders, claim names in
   `AppClaims`/`DemoClaims`, shared wording in `Copy`, colours in `site.css`/`StatsTheme`/`WinTone`,
   numbers headed into markup through `BrowserNumber.ToInvariant`. When a third call site repeats a
   line, extract it and name it.
10. **Do not reintroduce what the refactor removed**: domain events and `INotification` handlers,
    an `ErrorType` enum, interfaces with one implementation (`IResult`, `IAggregateRoot`),
    presentation data in `Domain`, `ICurrentUserService` or demo-session concerns in `Application`,
    an application-side factory that re-checks what `GameSetup.Create` already guarantees.
11. **Generated files are never hand-edited**: `wwwroot/js/island-geometry.js`,
    `wwwroot/img/layouts/generated/*.svg`, `Components/Shared/IslandBoardArt.g.cs`,
    `wwwroot/app.css`, migration `*.Designer.cs` files and `AppDbContextModelSnapshot.cs`.
12. **No secrets in the repo.** The SQL password is an Aspire user secret and the real OIDC values
    live in the gitignored `appsettings.Development.json`; the tracked `appsettings.json` holds
    placeholders only. Never write a credential into tracked config, code or a commit.

## How code is written

- C# 13 on .NET 10: file-scoped namespaces, primary constructors, collection expressions `[]`,
  `is { }` / `is not { }` patterns, `sealed` on every class and record that is not a base,
  `internal` for anything only its own assembly uses (handlers, validators, feature helpers).
- A single-statement `if` that returns may omit braces; everything else follows `.editorconfig`
  (Allman braces, 4 spaces, CRLF, `var` everywhere).
- Namespaces follow the aggregate, not the subfolder: `Domain.Models.Game` covers
  `Models/Game/{Entities,ValueObjects,Enums}`; the catalogue is `Domain.Models.Static` and its
  contents `Domain.Models.Static.Data`.
- Names say what a thing is to the player: `HandSeatsToUser`, `AwaitingAnswerFrom`,
  `InvolvingUser`, `NoMatches`. Not `ProcessMerge`, `ValidateState`, `GetData`, `Helper`.
- Records for requests, responses and DTOs carry primitives and shared enums only, never a value
  object or an aggregate.

## Comments and documentation

Comments here record *why the code is shaped as it is*, in full sentences, and are the main place
design decisions live. Match that register:

- Every public type, every command or query record, every non-trivial member and every Razor
  component has a `<summary>` (C#) or an opening `@* ... *@` block (Razor) saying what the thing
  *is*, which rule it guards or which layer owns the decision, and what would go wrong without it.
  Parameters that are not self-explanatory get `<param>` docs.
- An inline `//` comment goes before a decision a reader would question (an ordering, a deliberate
  omission, a provider quirk, a lifetime), not on code that reads on its own.
- When you extract a shared piece, say what it keeps consistent and where it was repeated before.
- Do not restate the code, leave TODOs, or keep commented-out code. A one-line value object or
  identifier that follows the template needs no comment at all.

The register, from `Friendship.AwaitingAnswerFrom`: "Whether the request is still waiting on
`responder` to answer — the one condition under which it can be accepted, or declined. Declining
keeps nothing: a request turned down is not a record of anything…"

## Where to look before changing something

| Doing | Read |
|---|---|
| Anything under `src/Domain` | `.claude/rules/domain.md` |
| A command, query, validator or handler | `.claude/rules/application.md` |
| Persistence, migrations, the demo sandbox | `.claude/rules/infrastructure.md` |
| A page, component, link, form or auth | `.claude/rules/webapp.md` |
| Tailwind, `site.css`, colours, typography | `.claude/rules/styling.md` |

Claude Code loads these automatically when a matching file is opened; other tools should read them
from the paths above before editing that layer.

## Common tasks

- **New value object**: domain record from the template → its `DomainErrors` entries → `Map<>()` in
  `AppDbContext.ConfigureConventions` → migration if persisted.
- **New operation**: one file `Features/<Area>/<VerbNoun>.cs` with request, response, validator,
  handler → register any new helper in `ServiceExtensions.AddApplication` → call it from the page
  through `_form.Apply(result, "…")`.
- **New page**: constant in `AppRoutes` (or a `*Link` builder when it has query state) → literal
  `@page` → `AppPageTitle` + `PageHeader` + `FormMessages` skeleton → entry in `NavMenu.Sections`
  if it belongs in the sidebar.
- **New shared component**: right folder under `Components/Shared` → opening `@* *@` block →
  `[Parameter, EditorRequired]` on what it cannot render without → inherit the family base class
  (`DetailSheetBase`, `PickerModalBase`, `CatalogueViewBase`) if it is a fifth of a kind.
- **Schema change**: change the model → `dotnet ef migrations add <Intent>` → read the generated
  `Up` and confirm it does exactly the intent → the demo (SQLite) still seeds.

## Definition of done

- `dotnet build SpiritIslandCompanion.slnx` succeeds with no new warnings.
- A schema change ships with its migration, named for the intent (`NestGameOutcome`,
  `RemoveRejectedFriendships`), and `DemoDataSeeder` still runs against SQLite.
- Anything a user can see was checked in the running app, normally through `/demo`.
- A commit message is one short lowercase line in the style of the history
  (`reworked app and domain logic`, `unified spirit detail card`).
