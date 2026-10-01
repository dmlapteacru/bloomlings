# Quickstart: validating the Garden look

How to prove the restyle works end to end. Commands run from the repository root. Every CI workflow is manual only
(CLAUDE.md), so run the local checks before pushing.

## Prerequisites

- The .NET 10 SDK (`core/global.json`).
- The Nunito files in `client/Assets/Bloomlings/UI/Fonts/Resources/` ([contracts/fonts.md](contracts/fonts.md)). The
  preview tool and the playtest embed them from there.
- For device checks: an Android phone for the playtest APK, and Unity 6.3 LTS for the game client.

## 1. Engine-free kit

```bash
dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj
```

Expected: the spec 002 kit tests still pass (`DesignTokensTests`, `ShapeLibraryTests`, `ScreenLayoutTests`,
`AssetSlotTests`, `LocalizationTests`), plus the new garden tests:

- **Color sets** ([data-model.md](data-model.md#colorset)):
  - every named set derives `Top`, `Lip` and `Line` from its base;
  - no `Line` is pure black;
  - a white label on each colored `Face` reaches 3:1 against its `Line` (FR-025).
- **Label look:** `TextLook.OnColor` uses the set's `Line`; `TextLook.Plain` has no outline and no extrusion.
- **Type styles:** only `type.badge` stays uppercase (spec Clarifications, 2A).
- **Booster tile states:** the look table of [contracts/booster-tile.md](contracts/booster-tile.md) (charges, price,
  selected overrides, disabled when not usable or not affordable).
- **Shapes and slots:** `deco.leaf`, `deco.flower` and `ui.play` rasterize to non-empty masks; `ui.deco.garden` and
  `ui.play` are registered slots.
- **Layout:** PLAY is about 2.6:1 (`size.play`), card buttons are narrower than PLAY, and all regions stay inside the
  safe area at 16:9, 18:9, 19.5:9, 20:9 and 21:9.
- **Fonts:** both files exist, and their `cmap` covers Basic Latin, Latin-1, Cyrillic, "×", "−" and the no-break
  space (SC-006). `OFL.txt` ships next to them.
- **Unity scripts:** the client compiles against the stubs with the new `UiFonts` and kit code.

## 2. Previews in the Garden look

```bash
dotnet run --project playtest/preview -- --out playtest/preview/out --before specs/002-ux-design-board/preview-board-sheet.png
```

Expected:

- **Images.** Every spec 002 frame at every aspect ratio, now in the Garden look, plus `board-sheet.png`.
- **Checks.** Exit code 0: every drawn shape is a registered slot, no hit targets overlap, no text or target leaves
  the safe area, and every target is at least `size.touch_min`.
- **Font.** The run logs that Nunito was loaded (no DejaVu fallback).
- **Before/after (FR-029).** `before-after.jpg` puts the spec 002 sheet above the new one; a copy is kept at
  `specs/003-cartoon-ui-style/preview-before-after.jpg`.
- **Review (SC-001).** Compare the sheet with the mockup stills in this folder (`garden-direction.jpg`,
  `garden-play-button.png`, `booster-variants.jpg`):
  - PLAY on its plate, with the full-height ▶, leaves and a flower;
  - sentence-case labels with volume;
  - cards in a wooden frame on paper;
  - the volumetric board, cells, pods and slot wells;
  - the booster tiles with the ×N badges and the price tags.

## 3. Asset inventory

```bash
dotnet run --project playtest/preview -- --inventory
git diff --stat specs/002-ux-design-board/asset-inventory.md
```

Expected: the inventory adds exactly `ui.deco.garden`, `ui.play` and the two Nunito files under typography (SC-006),
and the preview run reports "slots used: N of N".

## 4. The playtest still plays correctly

```bash
dotnet run --project playtest/check
```

Expected: 0 failed animator runs, and the meta checks pass. The look is presentation only, so the settled screen still
equals the rules state.

## 5. Core and backend unchanged

```bash
dotnet test core/Bloomlings.sln
node --test backend/tests/*.test.js
```

Expected: all green with the same test counts as before. No rules, content or economy change (FR-003).

## 6. On a phone (manual)

- **Full playtest.** Run Actions → "Android playtest APKs" → Run workflow, `apks: playtest`.
  1. Fresh-launch: Level 1 starts; the board sits in its wooden frame, and the pods and slots have depth.
  2. Win a few levels: Home shows the PLAY button on its plate, with leaves and a flower, and a label in Nunito.
  3. Press and hold PLAY, then release: the button squashes and springs back with one overshoot (FR-017).
  4. Reach the boosters (L3+): check the ×N badges, the price tag with "+", the golden ring while Return or Bloom
     Burst waits for a target, and the grey disabled tile when a booster can do nothing (FR-031).
  5. Pause, jam and win a level: the cards have the paper face, the wooden frame and the red close button.
  6. Look for any box-shaped "missing glyph" in numbers, "×", prices and the level text. Cyrillic is covered by the
     `cmap` test, because only English strings exist today.
- **Level tester.** `apks: tester`. It looks and behaves as before (◀ ▶, free boosters, instant results, the system
  font).
- **Unity client.** Open `client/` in Unity 6.3 LTS (`client/README.md`) and play the same path. Check that the
  labels use Nunito with their outline and extrusion, and that the profiler shows no new material per label
  (R12). Build the Unity APK only when needed (`unity-apk.yml`, which needs the Unity secrets).
