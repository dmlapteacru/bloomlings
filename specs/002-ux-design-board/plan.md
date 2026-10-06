# Implementation Plan: UX Design Board — the game's visual design without art assets

**Branch**: `002-ux-design-board` (work lands on `claude/great-darwin-6qrpj8`) | **Date**: 2026-09-30 |
**Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-ux-design-board/spec.md`

## Summary

Apply the 17-frame design board to the Unity game client and the full playtest APK, with no art or audio asset files.
Then deliver a complete asset inventory.

The approach:

- **A shared design kit.** Everything that defines the look is one engine-free kit in
  `client/Assets/Bloomlings/UI/Design/`, shared by both clients the way save, progression and economy already are:
  - tokens;
  - vector shapes and their rasterizer;
  - screen layouts;
  - number format;
  - the asset slot registry.
- **Unity client.** It restyles its code-built uGUI screens with the kit.
- **Full playtest.** It gets new engine-free screens drawn through a small painter interface. The level tester keeps
  its current minimal view.
- **Preview tool.** A new console tool renders those same playtest screens to PNG with SkiaSharp. The design can then
  be reviewed against the board and checked for overlaps across aspect ratios without a device. The tool also
  generates the asset inventory from the registry, so the list always matches the placeholders.

## Technical Context

**Language/Version**:
- C# 9–latest;
- the Unity client on Unity 6.3 LTS (.NET Standard 2.1 profile);
- the playtest on .NET 10 for Android (`net10.0-android`, API 26+);
- the preview tool on .NET 10.

**Primary Dependencies**:
- Unity uGUI and TextMeshPro (existing);
- `Android.Graphics` (existing);
- SkiaSharp 3.119 with `SkiaSharp.NativeAssets.Linux.NoDependencies`, for the preview tool only. It never ships in an
  APK.

**Storage**: N/A. No save or content changes.

**Testing**:
- `client/DotnetCheck`: the EditMode tests under .NET against the Unity stubs, plus the new design-kit tests.
- `playtest/check`: the animator and meta checks.
- `playtest/preview`: render checks — slots, overlaps, safe area, touch size.
- `core` and `backend` suites: unchanged, and must stay green.

**Target Platform**: Android and iOS phones in portrait (the Unity client); Android phones (the playtest APK).

**Project Type**: Mobile game (client presentation layer plus tools).

**Performance Goals**:
- 30+ fps on the reference low-end device, with no hitch over 100 ms during a level (SC-005).
- Shapes and backdrops are rasterized once per size and cached.
- No per-frame allocations in the level screen's draw path.

**Constraints**:
- No art or audio asset files (FR-002).
- No rule, content, economy or roadmap change (FR-004).
- Offline first.
- CI stays manual only: no new workflow triggers.
- The tester APK keeps its look (FR-003).
- Unity cannot be run here, so Unity code is compiled against the stubs only.

**Scale/Scope**:
- 17 frames;
- about 14 Unity screens and components restyled, and 2 new ones (splash, milestone card);
- about 16 playtest screens and painters;
- about 60 shapes;
- about 150 asset slots.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| I. Colony Flow structure, Bloomlings identity | The board is Bloomlings' own design (its own wordmark, families and garden). The core loop and Home → Play → Level → Next flow are unchanged. No Colony Flow name, art or UI graphics are used. | PASS |
| II. Exact matching, unambiguous board | Tiles keep variant symbols (FR-011). The variant colors are unchanged. The symbol ink follows `InkContrast`. Every state is shown by shape or symbol as well as color (FR-026). Tests keep the 3:1 contrast. | PASS |
| III. Deterministic simulation | Presentation only. The screens read `LevelView` and events, and motion scales with 2× only. No rules code changes (core suites unchanged). | PASS |
| IV. Validated, fair content | No content change. | PASS (n/a) |
| V. Difficulty from decisions | No gameplay change. The HARD and SUPER HARD badges only label existing classes. | PASS (n/a) |
| VI. Fair monetization | The "×2 reward" and "Get +N" buttons are optional, player-started rewarded ads. The Free rescue stays once per attempt. No interstitial is added. Store contents are unchanged. | PASS |
| VII. Simplicity, readability, offline-first | The splash adds no tap, and there is no level map. The Collection is never a level selector. The board keeps most of the screen (layout rule: at least 45% of the safe height). The Leaderboard shows offline data with a notice. | PASS |
| Workflow gates | Spec → plan → tasks → implement. CI stays manual only. There are no rules changes, so no golden updates are needed. | PASS |

Post-design re-check (after `research.md`, `data-model.md` and `contracts/`): still PASS. The painter interface and the
preview project are justified below.

## Project Structure

### Documentation (this feature)

```text
specs/002-ux-design-board/
├── spec.md                 # the feature spec (with the owner's answers)
├── ux-design-board.webp    # the reference board (17 frames)
├── plan.md                 # this file
├── research.md             # decisions R1–R12
├── data-model.md           # frames, tokens, shapes, asset slots, layouts, HomeLook
├── quickstart.md           # validation commands and phone checks
├── contracts/
│   ├── design-tokens.md    # colors, radii, type, spacing, elevation, motion
│   ├── asset-slots.md      # slot ids, categories, rules, inventory format
│   ├── screen-map.md       # frame → Unity / playtest implementation, deviations
│   └── painter.md          # the playtest painter interface
├── asset-inventory.md      # generated after implementation (User Story 4)
├── checklists/requirements.md
└── tasks.md                # /speckit-tasks
```

### Source Code (repository root)

```text
client/Assets/Bloomlings/
├── UI/Design/                     # NEW, engine-free, linked into the playtest
│   ├── Rgba.cs                    # color value + hex, mix, lighten/darken, contrast
│   ├── DesignTokens.cs            # contracts/design-tokens.md
│   ├── ShapeLibrary.cs            # SDF shapes by slot id (moved from ProceduralSprites + new UI shapes)
│   ├── ShapeRaster.cs             # alpha masks
│   ├── ScreenLayout.cs            # Gameplay / Home / Card / Sheet regions
│   ├── HomeLook.cs                # which frame-3 elements are shown
│   ├── NumberText.cs              # "1 240"
│   └── AssetSlots.cs              # the registry (contracts/asset-slots.md)
├── Art/Procedural/ProceduralSprites.cs   # now rasterizes ShapeLibrary masks
├── UI/UiTheme.cs, UI/UiFactory.cs        # tokens → Unity; board components (pill, round button, primary/secondary, card, sheet, badge)
├── UI/Screens/*.cs                       # restyled; SplashScreen.cs and MilestoneCard.cs new
├── UI/Gameplay/BoosterBar.cs             # frame 14
├── Gameplay/Tray/PodView.cs, TrayView.cs # frame 12
├── Gameplay/Slots/SlotRowView.cs         # frame 13
├── Gameplay/Board/TileView.cs, SpecialView.cs, BoardView.cs  # raised tiles, garden objects
├── Gameplay/Themes/BackdropView.cs       # NEW procedural garden backdrop (Unity)
├── Meta/DailyReward/DailyRewardPopup.cs  # frame 4
└── Tests/EditMode/Design*Tests.cs        # NEW design-kit tests (run by client/DotnetCheck)

playtest/
├── design/                       # NEW engine-free designed screens (full playtest + preview)
│   ├── IPainter.cs               # contracts/painter.md
│   ├── Kit.cs                    # board components drawn with tokens
│   ├── Backdrop.cs               # procedural garden backdrop
│   ├── PodPainter.cs, SlotPainter.cs, TilePainter.cs, BoosterBarPainter.cs
│   ├── SplashScreen.cs, HomeScreen.cs, LevelScreen.cs
│   ├── PauseCard.cs, JamSheet.cs, WinCard.cs, MilestoneCard.cs
│   ├── DailyRewardCard.cs, LeaderboardCard.cs, CollectionCard.cs, StoreCard.cs
│   └── DesignApp.cs              # screen stack, navigation, meta calls (engine-free)
├── android/
│   ├── Design/                   # NEW, full playtest only (the tester compiles android/*.cs only)
│   │   ├── AndroidPainter.cs
│   │   └── DesignView.cs         # the Android View hosting DesignApp; touch, insets, sound
│   ├── TesterView.cs             # was GameView.cs: the tester's minimal view, meta branches removed
│   ├── MainActivity.cs           # picks TesterView or DesignView by flavor
│   └── PlaytestMeta.cs           # + Daily Reward, Collection, Wardrobe, Store purchases with Petals
├── preview/                      # NEW console tool: SkiaPainter, frame fixtures, PNGs, inventory
│   ├── Bloomlings.Playtest.Preview.csproj
│   ├── SkiaPainter.cs
│   ├── Frames.cs                 # a scripted state for each board frame 1–17
│   └── Program.cs
└── Playtest.Shared.props         # links UI/Design/*.cs and the meta services
```

**Structure Decision**:
- The kit sits in the Unity client tree because the product client owns presentation. The playtest links it, as it
  does the other engine-free client files.
- The playtest's designed screens live in `playtest/design/`, outside `playtest/android/`. The full playtest and the
  preview tool compile them; the tester does not.
- No project is added to `core/Bloomlings.sln`: presentation never joins the rules solution.

## Complexity Tracking

| Deviation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| A third playtest project (`playtest/preview`) with a SkiaSharp dependency | The design must be reviewable against the board and checked for overlaps on several aspect ratios. Neither the Android SDK nor Unity runs where the work is done. | Checking only on a phone after a manual CI build is slow, costs Actions minutes, and cannot run in tests. |
| A painter interface in the playtest instead of drawing on `Android.Graphics.Canvas` directly | It lets the same screen code run in the APK and in the preview tool. | Direct canvas code cannot be rendered or tested without Android. |
| The designed screens exist twice (Unity uGUI and playtest painter) | The owner asked for the design in both builds (FR-003). The tokens, shapes, layouts and registry are shared, so only the thin drawing code is duplicated. | Moving the playtest to Unity needs the Unity secrets for every build. Moving the product to the painter would drop Unity (a locked decision). |
