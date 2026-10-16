---
paths:
  - "src/WebApp/Styles/**"
  - "src/WebApp/Components/**"
---

# Styling

Tailwind v4, authored in `Styles/site.css` and compiled to `wwwroot/app.css` by the
`BuildTailwind` MSBuild target (`npm run build:css`). The `@source` directive scans `.razor`
**and** `.cs`, so class names assembled in C# (`WinTone`, `TileChrome`, `StatsTheme`) are found —
keep them as whole literals, never concatenated fragments.

## Palette

| Token | Meaning |
|---|---|
| `ink-*` | parchment and the text on it: `ink-50` page, `ink-800` body, `ink-900` headings, `ink-500` captions |
| `deep-*` | the chrome: sidebar, public nav, landing hero. Text on it uses `.on-deep-*`, `.nav-item`, `.link-on-deep`; never `ink-600/700/800` inside `.deep` |
| `accent-*` | jade: growth, spirits, the primary action, a win |
| `ember-*` (and `red-*`) | the invaders: defeat, blight, danger |
| `sun-*` (and `amber-*`) | difficulty, terror, something waiting on you |
| `sky-*` | a request from another player |
| `bg-white` | a card (`#fdfbf6`); there is no pure white in the app |

`red`, `amber` and `sky` are remapped onto the palette in `@theme`, so their semantic uses keep
meaning what they meant. Hex values in C# come only from `StatsTheme`; a new step is added there
the day the first caller needs it, with the token it mirrors in a comment.

## Use the vocabulary before writing utilities

- Layout: `.page-shell`, `.page-head`, `.page-title`, `.page-sub`, `.eyebrow`, `.band-title`,
  `.section-head`, `.section-title`, `.section-title-lg`, `.section-label`, `.section-note`,
  `.section-link`.
- Surfaces: `.surface` with `.surface-head` / `.surface-body` / `.surface-foot`; `.panel`,
  `.panel-chart`, `.card-lift`; `.dialog-head` / `.dialog-foot`; `.picker-filters` /
  `.picker-body` / `.picker-foot`; `.sheet-head` / `.sheet-body` / `.sheet-foot`.
- Text ladder, quiet to read: `.fine-note`, `.section-note` / `.row-sub`, `.block-note`,
  `.body-note`, `.body-copy`. Names: `.row-title`, `.tile-title`, `.card-title`, `.card-title-sm`.
  Figures: `.numeral`, `.numeral-display`, `.micro-label`, `.micro-label-strong`.
- Controls: `.btn` with `.btn-primary` / `-secondary` / `-ghost` / `-soft` / `-danger` /
  `-danger-outline` / `-glass` / `-sm` / `-lg`; `.input`, `.select`, `.textarea`, `.checkbox`,
  `.label`, `.label-hint`, `.field-button`, `.field-menu`; `.focus-ring` (`-sea`, `-on-deep`).
- Tokens: `.badge` + `.badge-neutral/success/danger/info/warn/sm`; `.chip` +
  `.chip-sm/lg/neutral/scenario/island`; `.pill-row`, `.disc`, `.alert` +
  `.alert-success/danger/info/warn`, `.note`.
- Dead ends: `.no-matches` (filters left nothing), `.no-data` (nothing yet), `.no-value`,
  `.no-figure`.

Rules:

- Pick the semantic class first; add Tailwind utilities only for the spacing a particular block
  wants. A margin is the caller's; a face, a colour or a border is the class's.
- A new class goes into the matching `/* ---------- Section ---------- */` of `@layer components`
  with a comment saying what it is for, how it differs from its neighbours, and what it replaced.
  It earns its place at the third use.
- Outcome colours come from `WinTone` (`ForRate`, `ForOutcome`, `BadgeFor`, `FillFor`,
  `DeepPanel`); tile states from `TileChrome`. Don't write a win/loss ternary in markup.
- No `#fff`, no raw hex in `.razor`, no inline `style=` except a value that must be computed (a
  spirit colour via `GameLookups.TintBadgeStyle`, an SVG geometry) — and then through
  `BrowserNumber.ToInvariant` for any number in it.
- Headings are serif by the base layer; `SectionHead` decides `h2`/`h3` through `Level`, `Size`
  picks the register. Don't style a heading element by hand.
- Mobile first: every page works at phone width, dialogs are bottom sheets below `md`, and the
  sidebar collapses to the mobile header.
- Focus rings, hover rises and transitions are part of the class; don't re-add them per element.
- `aria-*` state attributes are rendered as text through `Aria.Flag`; a bare `bool` drops the
  attribute when false.
