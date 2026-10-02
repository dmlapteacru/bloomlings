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
- **Testing**: core, client DotnetCheck, backend, `playtest/check`, `playtest/preview` (24 frames + checks), artgen
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
