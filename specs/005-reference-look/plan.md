# Implementation Plan: Reference Look

**Branch**: `005-reference-look` (work on `claude/great-darwin-6qrpj8`) | **Date**: 2026-10-02 | **Spec**: `spec.md`

## Summary

Restyle every element of the Unity client and the full playtest after the owner's reference (`reference.jpg`) while
keeping every layout and rule: a saturated variant palette (readability-checked), candy board tiles with embossed
symbols on a stone border and a lawn, wooden pods and cream slots with variant tiles and counts, cream booster tiles
with green badges, glossy buttons in wooden rims, wooden signs, parchment cards, the lotus currency, the win
celebration (sign, full-color picture, pedestal, rays, petals) and the meta screens. Materials are engine-free pictures
from the kit (`UiRaster`) drawn by both builds. The owner's pictures (3D heroes, backgrounds, logo) get slots and
fallbacks (`pictures.md`).

## Technical Context

- **Language**: C# 9 for the client and the kit (`client/`), .NET 10 for the playtest, preview and tools.
- **Kit** (engine-free, linked into the playtest): `client/Assets/Bloomlings/UI/Design/` — `DesignTokens`,
  `GardenLook`, `ShapeLibrary`, `ShapeRaster`, `BackdropRaster`, new `UiRaster`, `AssetSlots`, `ScreenLayout`.
- **Playtest**: `playtest/design/` (screens, `Kit`, `IPainter`, `PainterBase`), `playtest/android/Design/AndroidPainter.cs`,
  `playtest/preview/` (`SkiaPainter`, `Fixtures`, `Checks`, `Inventory`).
- **Unity**: `client/Assets/Bloomlings/UI/UiKit.cs`, `UI/Screens/*`, `UI/Gameplay/BoosterBar.cs`, `Gameplay/Board/*`,
  `Gameplay/Slots/SlotRowView.cs`, `Gameplay/Tray/*`, `Gameplay/Themes/BackdropView.cs`,
  `Art/Procedural/ProceduralSprites.cs`; checked by `client/DotnetCheck` against `UnityStubs.cs`.
- **Core**: only `VariantCatalog` colors change (art placeholders); `content/readability/pairs-report.json` refreshed.
- **Art**: `tools/artgen -- build --only 2d` re-renders the 2D characters (their body colors come from the catalog).
- **Testing**: core, client DotnetCheck, backend, `playtest/check`, `playtest/preview` (26 frames + checks), artgen
  `check`, the Mono.Android type-check projects.

## Constitution Check

- I–IV (rules, determinism, exact matching, solver): untouched; presentation only.
- V (fair monetization): costs and charges unchanged; only their look changes.
- VI/VII (flat 2D, simplicity): gameplay stays 2D (depth drawn in the plane); 3D pictures stay on meta screens;
  layouts unchanged. **Pass.**
- Readability (spec 001 FR-005): every variant pair stays a candidate after the palette change (research D1). **Pass.**

## Phases

1. **Foundation** (`contracts/look.md` §1–3): palette and tokens, color sets, `UiRaster` (plank, frame, stone, tile),
   the picture primitive in `IPainter`/`PainterBase`/`SkiaPainter`/`AndroidPainter` and `ProceduralSprites.Picture`,
   redrawn symbols and new icons in `ShapeLibrary`, the kit components in `Kit` and their `UiKit` twins, background
   picture hooks, new asset slots, a kit sheet preview frame, tests.
2. **Playtest screens** (§4): gameplay (top bar, board, slots, tray, booster bar), popups and cards (jam, pause,
   settings, store, daily, collection, leaderboard, themes), win and milestone, Home and splash.
3. **Unity screens**: the same in the Unity views, through `UiKit`.
4. **Review**: side-by-side review of the preview frames against the reference, fixes.
5. **Polish**: strings, asset inventory, docs (`CLAUDE.md`, READMEs, spec 004 replaced requirements), all checks.

## Project Structure

```text
specs/005-reference-look/
├── spec.md, plan.md, research.md, tasks.md, pictures.md, reference.jpg
└── contracts/look.md
```

## Addendum: the owner's animated heroes and layered Home (2026-10-02)

The owner delivered the Home picture in layers and the four heroes as animated FBX models (spec.md FR-028). The models
are rendered offline into flat frames, so the game holds no 3D model, scene or camera (constitution VII: pre-rendered
3D as flat pictures on meta screens only). **Pass.** Both builds play the frames through the engine-free kit.

```text
tools/heroanim/                      # Node 22, not in the solution (README.md, SOURCE.md)
├── bake.mjs, page.html, serve.mjs, png8.mjs, heroes.json   # three.js in headless Chromium → 12 fps palette PNG frames
├── layers.mjs                       # the owner's Home layers: crops, the lotus cut-out, one shadow, the JPEG garden
├── check.mjs, manifest.json, layers.json                   # hashes of every output; must pass before committing them
└── models/{sprig,bloom,drop,twig}.fbx                      # the owner's models
client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion/   # {family}-{idle|react}-{NN}.png (288) + manifest.json
client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/  # home.jpg + the home-*.png layers
client/Assets/Bloomlings/UI/Design/
├── HeroMotion.cs, HeroMotionData.cs (generated)            # frames, cell, head points, hats, HeroMotionPlayer
└── HomeLayers.cs, HomeLayersData.cs (generated)            # layer boxes, placement, shadows, petals, HomeMotion
client/Assets/Bloomlings/Tests/EditMode/HeroMotionTests.cs
```

The hosts: the playtest's Home, splash and end cards with a bounded frame cache in both painters; Unity's
`HomeLayersView`, `HeroMotionView`, `HeroFrames` and `Editor/HeroMotionImporter` (tasks.md T025–T030).

## Addendum: the tray's pods in columns (2026-10-03)

The owner made the tray's layout a gameplay rule. A stack's pods go one after another and are never drawn on each
other, three or four rows are visible, and the boxes are resized to fit (spec.md FR-021 amended, SC-010; research
D20). The kit lays the grid out and both builds draw it from those regions. This is presentation only (FR-002): the
core's events drive the slides, and no rule, event or tap outcome changes. **Pass** (constitution I–VII unchanged; the
exposed pod alone stays selectable, spec 001 FR-011).

```text
client/Assets/Bloomlings/UI/Design/ReferenceLayout.cs  # Columns, PodRows, Pod, Chip, Shows; PodChip
client/Assets/Bloomlings/Tests/EditMode/ReferenceLayoutTests.cs
playtest/design/PodPainter.cs, KitGarden.cs, LevelScreen.cs # the columns, Kit.Pod, TrayMotion
client/Assets/Bloomlings/Gameplay/Tray/TrayView.cs, PodView.cs  # the Unity grid and its slides
client/Assets/Bloomlings/UI/UiKitTray.cs, Screens/GameplayHud.cs  # UiKit.TrayPanel, GridPod; GameplayHud.PodGrid
```

Tasks T031–T035.

## Addendum: the pod's look "E" (2026-10-03)

The owner chose the pod's look from six mock-ups (spec.md, the clarification "E"; research D21): the owner's icon
first, over the middle of a 1.3:1 frame centered in its place, the count small in a white outline at the bottom right
corner, the "+N" disc at the top left and the link ring at the top right. The kit's `PodChip` places the parts and
both builds draw them. Presentation only (FR-002). **Pass** (constitution I–VII unchanged).

```text
client/Assets/Bloomlings/UI/Design/ReferenceLayout.cs  # PodChip: Aspect, Icon, Tile, Count, Badge, CountLook
client/Assets/Bloomlings/Tests/EditMode/ReferenceLayoutTests.cs
playtest/design/KitGarden.cs, PodPainter.cs            # Kit.Pod, PodCount; the links on the frames
client/Assets/Bloomlings/UI/UiKitTray.cs, Gameplay/Tray/TrayView.cs  # GridPodView's icon and count; the links
```
