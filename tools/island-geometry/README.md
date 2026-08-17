# Island geometry

Every picture of an island in the app is drawn from one traced board and one list of
placements, and both live here. This is the source for them; what the app ships is generated.

Nothing in the build runs this. `dotnet build` won't, there is no CI, and the folder sits
outside `src/` so it never reaches the server. It is run by hand, on the rare occasions the
board or a layout changes, and **its outputs are committed** — so a change here is only half
done until the generated files are regenerated and committed alongside it.

```bash
node tools/island-geometry/build.mjs
```

## Inputs

| file | what it is | edit it when |
|---|---|---|
| `board.svg` | the traced outline of one physical board — a 60°/120° rhombus, three sides cut to interlock and the fourth the ocean coast | the trace itself is wrong |
| `layouts.json` | the 25 published layouts, as board placements in the island playground's own save format | a published layout is wrong, or a new one is published |

To correct a layout, build it in the playground, save it, and paste the arrangement into
`layouts.json` — the placements come from the same code that loads them back, so what you saw
on screen is what ships. Don't try to solve for it by hand.

## Outputs

All three are generated. None of them should ever be hand-edited; the next run overwrites them.

| file | who reads it |
|---|---|
| `WebApp/wwwroot/js/island-geometry.js` | the island playground — `BOARD` (outline, seams, contacts) and `LAYOUTS` |
| `WebApp/wwwroot/img/layouts/generated/*.svg` | 25 thumbnails, one per published layout, shown by `IslandSetupDiagram` |
| `WebApp/Components/Shared/IslandBoardArt.g.cs` | `IslandLayoutDiagram`, which draws a shape the player built and no thumbnail exists for |

The last two are why the `ART` constants in the script matter: a published layout and a
player's own shape are drawn by different code, on different sides of the wire, and must come
out looking like the same island. Change a colour or the margin and all 26 of those files
change together.

## Where the rest of it is written down

The script's header comment carries the geometry itself — the corner indices and why getting
them right is the whole game, the measured interlock tolerances per side, why the coast is
deliberately not an interlock, and why the two 60° rotate buttons are enough to reach every
join. Read that before changing any tunable. It is not repeated here, so it can't rot here.
