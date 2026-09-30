# Data Model: UX Design Board

**Feature**: `specs/002-ux-design-board` | **Date**: 2026-09-30

This feature adds no saved data and no content data. Everything below is static, engine-free presentation data in
`client/Assets/Bloomlings/UI/Design/`. The full playtest links it and the preview tool compiles it.

## DesignFrame

One of the 17 numbered frames of the board.

| Field | Type | Rule |
|---|---|---|
| `Number` | int | 1–17, unique |
| `Name` | string | as on the board ("Splash", "Home (early levels)", …) |
| `Counterparts` | list of string | the screen, component or state that implements it; at least one (SC-001) |
| `Deviations` | list of string | a recorded difference from the board, and its reason (FR-001) |

The frame list and its counterparts are documented in [`contracts/screen-map.md`](contracts/screen-map.md). The preview
tool renders one image per frame.

## VisualToken

A named value of the visual language (FR-005). The kinds:

- **ColorToken**:
  - fields: `Name` and `Rgba` (bytes, with `Hex` for docs);
  - used through `DesignTokens.Colors.*`;
  - converted to `UnityEngine.Color` in Unity and to `Android.Graphics.Color` in the playtest.
- **RadiusToken**: a corner radius as a fraction of the shape's shorter side (0–0.5), or `Pill` (0.5).
- **TypeToken**:
  - fields: `Name`, `Size` (in reference units of a 1080-wide screen), `Bold`, `Upper`, `Outline` (0 or an outline
    width in reference units);
  - `MinSize` is the smallest size auto-fit may shrink to.
- **SpacingToken**: a gap in reference units.
- **ElevationToken**: a soft shadow, as a y-offset and an alpha, drawn as a darker lower edge or a soft blur.

Validation:

- Every token name is unique.
- Every text color token pairs with the background it is used on at a contrast of at least 4.5:1 for body text and 3:1
  for large text and symbols (tested with `InkContrast`).

The values are in [`contracts/design-tokens.md`](contracts/design-tokens.md).

## Shape

A placeholder vector shape drawn without an asset (FR-002).

| Field | Type | Rule |
|---|---|---|
| `Id` | string | a stable dotted id, equal to the `AssetSlot.Id` it stands in for |
| `Sdf` | function (x, y) → distance | x and y in −1..1, negative inside |

- `ShapeRaster.Mask(id, size)` gives a `size × size` alpha mask. Anti-aliasing is one pixel wide.
- Clients cache masks by `(id, size)`.
- Unknown ids fall back to the "?" shape. A test forbids unknown ids in shipped code.

## AssetSlot (placeholder ↔ future asset)

One needed art or audio asset, and the placeholder standing in for it.

| Field | Type | Rule |
|---|---|---|
| `Id` | string | dotted: `<category>.<name>[.<state>]`, e.g. `booster.shuffle`, `pod.state.locked`, `bg.theme.pond` |
| `Category` | enum | one of the FR-029 categories (below) |
| `Title` | string | what it is, in plain words |
| `Frames` | list of int | the board frames that show it (1–17); may be empty only for audio or non-board uses |
| `UsedIn` | list of string | screens or components where the game shows it |
| `States` | list of string | the states or variants it needs, e.g. `exposed`, `next`, `pressed`, `locked` |
| `SizeClass` | enum | `Icon` (≤ 96 px), `Small` (≤ 192), `Medium` (≤ 512), `Large` (≤ 1024), `Screen` (full screen), `Audio` |
| `Readability` | bool | must stay readable at small sizes and pass spec 001's readability checks |
| `Priority` | enum | `Launch` (needed for launch) or `Later` |
| `Placeholder` | string | how the game draws it today, e.g. "shape `ui.pause`", "procedural backdrop", "synth cue `Click`" |

`Category` values (FR-029):

- `Brand`, `Background`, `Character`, `VariantSymbol`, `BoardTile`, `Special`, `PodSlot`, `Booster`;
- `UiKit`, `Currency`, `CollectionFrame`, `Cosmetic`, `Effect`, `Typography`, `Audio`.

Validation, enforced by tests:

- Every id is unique and matches its category prefix (the table in
  [`contracts/asset-slots.md`](contracts/asset-slots.md)).
- Every shape id used by either client, and every backdrop, sound cue and font role, resolves to a registered slot.
- Every registered slot is used by at least one client (SC-003, both ways).
- `Readability` is true for every `VariantSymbol`, `PodSlot` and `BoardTile` slot.

## AssetInventoryEntry

The document form of an `AssetSlot`: one row of `specs/002-ux-design-board/asset-inventory.md` (FR-029, FR-030).
The preview tool generates the document from the registry; it is not edited by hand.

## ScreenLayout regions

Computed, never stored.

- **Input**: the screen size in pixels and the safe-area insets.
- **Output**: rectangles normalized to 0–1 of the screen.

| Layout | Regions (top to bottom) | Rules |
|---|---|---|
| `Gameplay` | `TopBar`, `Badge` (Hard/Super Hard), `Board`, `Slots`, `Tray`, `Boosters` | 1–3 |
| `Home` | `TopBar` (Petals pill, Settings), `Hero`, `Level`, `Teaser`, `Play`, `Rank`, `Daily`, `Features` | 4 |
| `Card` | `Title`, `Close`, `Body`, `Actions` | 5 |
| `Sheet` | `Grip`, `Title`, `Subtitle`, `Options`, `Primary`, `Secondary` | 6 |

Rules:

1. Regions keep the frame's order and never overlap.
2. The bands (`TopBar`, `Slots`, `Tray`, `Boosters`) keep a minimum height, scaled by width.
3. The board gets the rest and at least 45% of the safe height on 16:9.
4. `Hero` and `Rank` collapse when their feature is locked (frame 2 vs 3).
5. `Card` is centered, 84% of the width, and its height fits its content.
6. `Sheet` sticks to the bottom, over the lower part of the screen, and keeps the board visible (spec 001 FR-027).

Tests check rules 1–6 at 16:9, 18:9, 19.5:9, 20:9 and 21:9 (SC-007).

## HomeLook (derived view model)

Which frame-3 elements Home shows. It is derived from progression, not stored.

| Element | Shown when |
|---|---|
| Petals pill, Settings, Level N, PLAY | always (frame 2) |
| Milestone teaser "N levels to reward" + gift | a next milestone exists |
| Bloomling hero | the Wardrobe is unlocked (roadmap L40); before that, Home shows the frame 2 scene |
| Wardrobe button | the Wardrobe is unlocked |
| Rank row "Rank #N >" | the Leaderboard is unlocked (L10) |
| Daily Challenge card | the Daily Challenge is unlocked (L50) and today's challenge exists |
| Store "+" on the Petals pill | the Store is unlocked (L12) |
| Collection button | at least one picture is in the Collection |
