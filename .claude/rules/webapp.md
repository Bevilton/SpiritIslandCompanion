---
paths:
  - "src/WebApp/**"
---

# WebApp (Blazor Server)

Interactive-server Blazor, one circuit per visit. Pages live in `Components/Pages`; shared
components in `Components/Shared` (generic), `Shared/Games`, `Shared/Games/Filtering`,
`Shared/Stats`, `Shared/People`, `Shared/Views`. Host concerns sit beside `Program.cs`: `Auth/`,
`Demo/`, `State/`, `Validation/`, `Catalogue/`, `AppRoutes.cs`.

`_Imports.razor` already imports every `Application.Features.*` and `Domain.Models.*` namespace and
every `WebApp.*` folder. Don't add `@using` to a page; the two deliberate exceptions (ApexCharts,
static `RenderMode`) are explained there.

## Routes and links

- Signed-in pages are `/app/...` with `@attribute [Authorize]`; public pages drop the `/app` and
  use `@layout PublicLayout`.
- `@page` stays a literal. Every href is built from a constant: `AppRoutes.*` for plain addresses,
  `GamesLink` / `StatsLink` / `PeopleLink` for addresses with a query. A new page adds its constant
  to `AppRoutes` (or a `*Link` builder beside the page when it has query state) and, if it belongs
  in the sidebar, an entry in `NavMenu.Sections`.
- Page state worth sharing is URL state: read it with
  `[SupplyParameterFromQuery(Name = GamesLink.XKey)]` and write it back through the same builder,
  so the pills and the address bar cannot disagree. The free-text search box is not URL state.

## Page skeleton

```razor
@page "/app/settings"
@attribute [Authorize]
@inject IMediator Mediator
@inject ICurrentUserService CurrentUser

<AppPageTitle Title="Settings"/>

<PageHeader Eyebrow="Account" Title="Settings">
    <Sub>Configure your collection and preferences.</Sub>
    <Actions>…</Actions>
</PageHeader>

<FormMessages Feedback="_form"/>

@if (_loading) { <LoadingSpinner/> }
else if (_user is null) { <NotFound>Couldn't load your account right now.</NotFound> }
else { … }

@code {
    private bool _loading = true;
    /// Field errors, the general alert and the failed-attempt count in one place — see FormFeedback.
    private readonly FormFeedback _form = new();

    protected override async Task OnInitializedAsync()
    {
        if (CurrentUser.UserId is not { } userId) { _loading = false; return; }
        var result = await Mediator.Send(new GetUserQuery(userId));
        if (result.IsSuccess) _user = result.Value;
        _loading = false;
    }

    private async Task SaveNickname()
    {
        var result = await Mediator.Send(new SetNicknameCommand(CurrentUser.RequireUserId(), _nickname));
        if (!_form.Apply(result, "Name saved.")) return;
        await UserState.ReloadAsync();   // only after user-scoped state (name, expansions) changed
    }
}
```

- Reads use `CurrentUser.UserId is { } uid` (a page can render before the state resolves);
  commands behind `[Authorize]` use `CurrentUser.RequireUserId()`. Never `UserId!.Value`.
- Every command result goes through `_form.Apply(result, "Saved.")`. It routes validation errors
  to `FormErrorSummary`/`FieldError`, any other failure to `Alert`, and returns success. Pages
  render `<FormMessages Feedback="_form"/>`; dialogs render `<Alert>` alone.
- Field errors: `<FieldError For="Players[0].SpiritId" Errors="@_form.Errors"/>` with the
  FluentValidation property path. `_form.Resolve("Field")` when the user fixes a field,
  `_form.Clear()` when a dialog opens afresh.
- Pages never catch exceptions around `Mediator.Send`; a failure is a `Result`.
- A busy flag (`_saving`) holds the button closed while a command is in flight; `SubmitField`
  and `ActionGlyph` already do this for the single-field forms.
- User-scoped, change-rare state (owned expansions, display name) comes from `CurrentUserState`
  and reaches components as the cascade named `CurrentUserState.OwnedExpansionsCascade`. Everything
  else is queried per page. Null in that cascade means "show everything".

## Components

- A component file opens with an `@* ... *@` block saying what it is, which surfaces share it,
  and the decision behind anything non-obvious. A C# helper (`Copy`, `WinTone`, `GameLookups`)
  says the same in its `<summary>`.
- Parameters: `[Parameter, EditorRequired]` for anything the component cannot render without; a
  `///` summary on every parameter that is not obvious; `RenderFragment` slots named for their
  place (`Sub`, `Actions`, `Trailing`, `TitleSuffix`); a `Class` parameter for the caller's
  spacing. A component never assigns to its own parameter.
- Extract a component at the third repetition (`PageHeader`, `SectionHead`, `SubmitField`,
  `EmptyState` all came from that rule). What genuinely differs becomes a parameter; the surface
  around it stays with the caller.
- Families share a base class: reference sheets inherit `DetailSheetBase`, setup pickers
  `PickerModalBase`, catalogue views `CatalogueViewBase`, the signed-in reference pages
  `CatalogueReferencePage`. A new member of a family inherits the base rather than copying a
  sibling, and a base seals lifecycle methods it owns (`CatalogueViewBase.OnParametersSet`).
- Dialogs are `ModalShell` plus `DialogHeader` / `PickerModalHeader` / `DetailModalHeader`. The
  host owns the open flag; stacking is `z-40` (seat dialog) < `z-50` (pickers) < `z-[60]` (sheets).
- Catalogue lookups by string id go through `GameLookups` (indexed); never scan `GameData` inside
  a render loop. Presentation data for a catalogue entry (`SpiritDetails`, `BoardDetails`, …)
  lives in `WebApp/Catalogue`.
- Numbers written into attributes, CSS or SVG use `value.ToInvariant("0")`; shares come from
  `Percent.Of`; plurals and shared sentences from `Copy`; `aria-expanded`-style flags from
  `Aria.Flag`; a recorded game's figures from `GameFormat`, a statistic's from `StatsFormat`.
- Tallies over a loaded history go through `SetupInsights`, so "a draft counts as played but not
  as a result" is decided once.
- JS interop is confined to the island playground (`wwwroot/js/island-playground.js`).
  `island-geometry.js`, the layout SVGs and `IslandBoardArt.g.cs` are generated by
  `tools/island-geometry/build.mjs` and never edited by hand.
- Icons: add a `case` to `Icon.razor` (heroicons outline, 24×24) rather than inlining SVG.
- Dead ends have one voice each: `<EmptyState>` for an empty page, `<NotFound>` for a missing
  subject, `.no-matches` for a list the filters emptied, `.no-data` for a panel with nothing yet.

## Auth and demo

- The account id is the `AppClaims.UserId` claim, written at sign-in by `UserSyncOidcEvents` and
  read by `ICurrentUserService`. Never read a claim by literal name; add a constant to `AppClaims`
  or `DemoClaims`.
- A manually created scope that queries on behalf of a demo visitor calls
  `IDemoSessionLocator.PinToSandbox(...)` first (`CurrentUserState` shows the pattern); the sign-in
  flow calls `PinToRealDatabase()`.
- `Application` and `Infrastructure` never reference `ICurrentUserService`, `HttpContext` or a
  claim; the account id is passed in as a `Guid`.
- Redirect targets that come from a query string go through `AuthEndpoints.LocalOrHome`-style
  checks: only a local path is ever followed.
