# Supported large boards: 24×24, 30×32, 36×36

These are engineering fixtures, not additions to the 2500 unique launch compositions. Their procedural subjects are
used to exercise board capacity, import, generation, solving, packs and rendering. The art direction is separate:
small boards use simple silhouettes; unusual story scenes use 22×28 or large boards.

| Field | Picture | Level | Profile | Base seed | Forced mechanic |
|---|---|---|---|---|---|
| 24×24 | dev_mosaic_flower_24x24 | 5001 | band-mosaic-24x24.json | 1 | — |
| 30×32 | dev_mosaic_balloon_30x32 | 5002 | band-mosaic-30x32.json | 1 | stone |
| 36×36 | dev_mosaic_butterfly_36x36 | 5003 | band-mosaic-36x36.json | 2 | — |

`src/` holds the original text grids and importer sidecars; `lib/` the canonical imported pictures; `validation/` the
solver results. The corresponding definitions are in `content/curated/dev/`. Generation profiles are in
`content/profiles/dev/`. Each fixture is solvable without boosters and has a jam witness. They are separate from
`content/pictures/lib` and the production catalog: normal APK packaging and normal generation do not pick them.
The tester and desktop preview include the extra pictures explicitly. Unity's dev loader resolves this library too.

The same 36×36 picture also has generated Hard and Super Hard regression levels in
`core/tests/Bloomlings.Generator.Tests/Fixtures/extended-36-*.level.json`. The Hard example scores 4437 with peak
buffer 3; Super Hard scores 4572 with peak 4 (thresholds 4090/4490). Both pass release validation, win without boosters
and have jam witnesses. They test balance and source ordering; they are not extra unique compositions.

## Generate on your computer

Use the normal .NET 10 CLI from the repository root. No image-generation service or GPU is needed. For example:

```sh
dotnet run --project core/src/Bloomlings.Pipeline -c Release -- pictures import --src content/pictures/dev/src --out content/work/large-check/library
dotnet run --project core/src/Bloomlings.Pipeline -c Release --no-build -- pictures validate --lib content/work/large-check/library
dotnet run --project core/src/Bloomlings.Pipeline -c Release --no-build -- generate --profile content/profiles/dev/band-mosaic-36x36.json --levels 5003-5003 --seed 2 --class normal --lib content/work/large-check/library --catalog content/work/large-check/empty --curated content/work/large-check/empty --keep content/work/large-check/empty --out content/work/large-check/36x36
dotnet run --project core/src/Bloomlings.Pipeline -c Release --no-build -- validate --defs content/work/large-check/36x36 --lib content/work/large-check/library
dotnet run --project core/src/Bloomlings.Pipeline -c Release --no-build -- score --defs content/work/large-check/36x36 --profiles content/profiles/dev --curated content/work/large-check/empty --lib content/work/large-check/library
dotnet run --project core/src/Bloomlings.Pipeline -c Release --no-build -- review --defs content/work/large-check/36x36 --lib content/work/large-check/library --out content/work/large-check/review
```

For the other sizes, change the profile, level range and seed to the table values. The 30×32 fixture also needs
`--mechanics stone`. Keep the history directory empty. The work output never overwrites production content.

For Hard/Super Hard, use `content/profiles/dev/band-mosaic-36x36-classes.json`, level `5003-5003`, and the same imported
library. Hard uses `--seed 1 --class hard` (candidate 22); Super Hard uses `--seed 2 --class super_hard` (candidate 10).
Use a separate output directory for each class. Large-board tray tuning also checks pressure before returning a
candidate and avoids jumping past the requested class; existing dimensions retain their previous tuning behavior.

To render gameplay at all three phone shapes plus a full 36×36 win and its Collection detail:

```sh
dotnet run --project playtest/preview -c Release -- --frames 50,51,52,94,95 --out content/work/large-check/screens
```

## Limits and balance

Three additional engineering fixtures exercise `gen-1.5.0`: L5004 (visible stacks on 24×24, seed 1, Normal, forced
`counted_stack`), L5005 (a full 3×3 gate on 36×36, seed 2, Normal, forced `gate`) and L5006 (a full 3×3 Fountain on
36×36, seed 2, Normal, forced `fountain`). Their profiles are `band-counted-stack-24x24.json`,
`band-large-gate-36x36.json` and `band-large-fountain-36x36.json` in `content/profiles/dev/`. Use the same dev library
and empty history folders. Validation records are next to the original three. Preview frames 96/97/98/99 cover these
levels, including a real animated count of 5 after the initial 9 and the Fountain condition chip. All six levels remain
outside production packaging.
See `product/16_MECHANICS_AND_BOARD_READABILITY.md` for the full compatibility policy and future feature proposals.

Runtime coordinates are 0–35 on each axis; schemas and JSON readers match. A whole board fits its gameplay area,
without scroll/zoom. The existing visibility rule still hides the next layer above 288 cells; this is independent of
whether the visual tile style draws a symbol. Win and Collection use the same tile style as gameplay.

The standard band profiles accept up to 36 per side. Their count/work/duration ranges remain calibrated at the
legacy 616-cell anchor: `GenerationProfile.ForBoard` scales those ranges only above it. Variant counts, pod size
limits, mechanics unlocks, fairness and five-slot rules remain in effect. Difficulty thresholds retain their previous
interpolation through 616 cells and scale above it; old levels are not rebalanced. Legacy profiles without `readableMechanics` retain `gen-1.3.0` / `gen-1.4.0` reproduction. The standard presets now
enable `gen-1.5.0` readability rules for newly generated content; stored production definitions are unchanged.

The existing 2500 production definitions remain intact. Adding new large story art and regenerating launch content
is a separate production step; the final library still requires 2500 different compositions used once each.
