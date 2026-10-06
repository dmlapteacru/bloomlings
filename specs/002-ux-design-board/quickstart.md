# Quickstart: validating the UX Design Board

How to prove the design works end to end. Commands run from the repository root. Every CI workflow is manual only
(CLAUDE.md), so run the local checks before pushing.

## Prerequisites

- The .NET 10 SDK (`core/global.json`).
- For the preview tool: the SkiaSharp NuGet packages, restored automatically, and a system sans-serif font (DejaVu
  Sans on Linux).
- For device checks: an Android phone for the playtest APK, and Unity 6.3 LTS for the game client.

## 1. Engine-free design kit

```bash
dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj
```

Expected, among the client tests:

- `DesignTokensTests`:
  - every token name is unique;
  - text and background pairs reach 4.5:1 (body) or 3:1 (large text, symbols).
- `ShapeLibraryTests`:
  - every shape id rasterizes to a non-empty mask;
  - every variant symbol differs from every other (distinct masks);
  - unknown ids are reported.
- `ScreenLayoutTests`:
  - the Gameplay, Home, Card and Sheet regions keep their order;
  - regions do not overlap and stay inside the safe area at 16:9, 18:9, 19.5:9, 20:9 and 21:9;
  - the board keeps at least 45% of the safe height on 16:9.
- `NumberTextTests`: "1 240", "12 345", "5 000", "123 456".
- `AssetSlotTests`:
  - ids are unique and match their category prefixes;
  - every shape the Unity client references is registered;
  - readability flags are set for symbols, pods, slots and tiles.
- The existing `LocalizationTests` pass with the new keys, and no UI literals appear.

## 2. Previews of all 17 frames

```bash
dotnet run --project playtest/preview -- --out playtest/preview/out
```

Expected:

- **Images.** One PNG per frame and aspect ratio in `playtest/preview/out/`, for example
  `07-gameplay-normal-19.5x9.png`, plus `board-sheet.png` with all frames side by side.
- **Checks.** Exit code 0, with these checks passing:
  - every drawn shape is a registered slot;
  - no hit targets overlap;
  - no text or target leaves the safe area;
  - every target is at least `size.touch_min`.
- **Review (SC-001).** Open `board-sheet.png` next to `specs/002-ux-design-board/ux-design-board.webp`. A copy of the
  latest sheet is kept at `specs/002-ux-design-board/preview-board-sheet.png`. Each frame
  should match in layout, element order and hierarchy, apart from the deviations recorded in
  [`contracts/screen-map.md`](contracts/screen-map.md).

## 3. Asset inventory

```bash
dotnet run --project playtest/preview -- --inventory
git diff --exit-code specs/002-ux-design-board/asset-inventory.md
```

Expected:

- The inventory is regenerated with no difference from the committed one.
- The preview run reports "slots used: N of N" (SC-003).

## 4. The playtest still plays correctly

```bash
dotnet run --project playtest/check
```

Expected:

- 0 failed animator runs.
- The meta checks pass: progression, Petals, milestones, Daily Reward and Collection.
- The designed screens are presentation only, so the settled screen still equals the rules state.

## 5. Core and backend unchanged

```bash
dotnet test core/Bloomlings.sln
node --test backend/tests/*.test.js
```

Expected: all green, with the same test counts as before this feature. No rules, content or economy change (FR-004).

## 6. On a phone (manual)

- **Full playtest.** Run Actions → "Android playtest APKs" → Run workflow, `apks: playtest`.
  1. Install the APK and fresh-launch it: the splash appears, then Level 1 starts directly.
  2. Win a few levels: Home appears in the frame 2 look.
  3. Use the tester controls on Home to jump past L10, L40 and L50: Home shows the rank row, the hero and Wardrobe, and
     the Daily Challenge card (frame 3).
  4. Pause, jam and win a level, and reach L25: check the pause card, the jam sheet, the win card and the milestone
     card against frames 10, 11, 15 and 16.
- **Level tester.** `apks: tester`. It looks and behaves as before (◀ ▶, free boosters, instant results).
- **Unity client.** Open `client/` in Unity 6.3 LTS (`client/README.md`) and play the same path. Build the Unity APK
  only when needed (`unity-apk.yml`, which needs the Unity secrets).
