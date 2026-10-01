# Implementation Plan: Cartoon UI Style — game-like garden buttons and a volumetric 2D board

**Branch**: `003-cartoon-ui-style` (work lands on `claude/great-darwin-6qrpj8`) | **Date**: 2026-10-01 |
**Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-cartoon-ui-style/spec.md`

## Summary

Restyle the Unity game client and the full playtest APK in the "Garden" direction the owner chose on the mockup.
The level tester APK keeps its minimal look. The result looks like this:

- **Buttons:**
  - each button is a flat cream plate with a thin brown outline;
  - a raised button with its own outline, lip and highlight lies on the plate;
  - main buttons are shorter and taller, and carry leaves and white flowers.
- **Labels:** sentence case in Nunito, with a little volume (gradient fill, thin outline, short extrusion).
- **Gameplay:** a volumetric 2D board, cells, pods and slots.
- **Boosters:** booster tiles with every state.

The approach extends the spec 002 kit rather than replacing it:

- **The shared kit.** All new values live in the engine-free kit in `client/Assets/Bloomlings/UI/Design/`:
  - the garden recipe tokens;
  - the color sets;
  - the label look;
  - the decoration shapes;
  - the booster tile states;
  - one new asset slot.

  The Unity client and the playtest draw from the same values.
- **The font.** Nunito (OFL) is the only new file: SemiBold for body text, ExtraBold for display text. It is loaded
  from the same files by Unity (a runtime TextMeshPro font asset), by the playtest (an Android `Typeface`) and by the
  preview tool (a Skia typeface).
- **The painter.** The playtest painter interface gains a label look (gradient fill, outline, extrusion), so labels
  can have volume. The Kit primitives (`Raised`, buttons, pills, cards) gain the plate layer.
- **Verification.** It relies on the preview tool and the tests:
  - the existing preview tool renders every frame in the new look and still checks slots, touch targets, overlaps and
    the safe area;
  - the new kit tests cover the label contrast (FR-025), the color sets, the booster states and the font files.

## Technical Context

**Language/Version**:
- C# 9 to latest;
- the Unity client on Unity 6.3 LTS (.NET Standard 2.1 profile);
- the playtest on .NET 10 for Android (`net10.0-android`, API 26+);
- the preview tool on .NET 10.

**Primary Dependencies**:
- the existing ones: Unity uGUI and TextMeshPro, `Android.Graphics`, and SkiaSharp 3.119 (preview only);
- new: the Nunito font files (SIL OFL 1.1), taken from the npm package `@expo-google-fonts/nunito` 0.4.2, with the
  package's own OFL text (`LICENSE_FONT`).

**Storage**: N/A. No save, content or economy change.

**Testing**:
- `client/DotnetCheck` with new kit tests;
- `playtest/check` (animator and meta);
- `playtest/preview` render checks;
- the `core` and `backend` suites, unchanged and still green.

**Target Platform**: Android and iOS phones in portrait (the Unity client); Android phones (the playtest APK).

**Project Type**: Mobile game (client presentation layer plus tools).

**Performance Goals**:
- 30+ fps on the reference low-end device, with no hitch over 100 ms (SC-005).
- Every TextMeshPro label look shares one material per font and look; there are no per-label material instances.
- The painter caches the font and the gradient shaders.

**Constraints**:
- No art or audio files; the font is the only new file (FR-001).
- No rule, content, economy or roadmap change (FR-003).
- Offline: the font ships inside both apps.
- CI stays manual only.
- The tester APK is unchanged.
- Unity cannot be run here, so the Unity code is compiled against the stubs only.

**Scale/Scope**:
- about 12 kit primitives restyled in each client;
- 4 gameplay views (board, tiles, pods, slots) and the booster bar;
- about 14 Unity screens touched through the kit;
- 2 font files;
- 3 new shapes (leaf, flower, play arrow);
- 1 new asset slot.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| I. Colony Flow structure, Bloomlings identity | The look is the owner's own garden reference: plates, leaves, flowers, cream and wood. Only genre conventions (chunky raised buttons) are shared. No Colony Flow name, art, icon or layout is used. The core loop and flow are unchanged. | PASS |
| II. Exact matching, unambiguous board | Cells keep their variant colors and symbols. The lip, bevel and highlight never cover a symbol (FR-023). The 3:1 symbol contrast and the colorblind checks stay in the tests. Every booster state reads by shape as well as color (FR-031). | PASS |
| III. Deterministic simulation | Presentation only. No rules code changes; the core suites and the golden replays are unchanged. | PASS |
| IV. Validated, fair content | No content change. | PASS (n/a) |
| V. Difficulty from decisions | No gameplay change. | PASS (n/a) |
| VI. Fair monetization | Booster prices and the "+" only show existing Petal purchases. No new purchase, ad or pressure element. | PASS |
| VII. Simplicity, readability, offline-first | Strictly 2D: depth is drawn in the plane (spec Clarifications, Q2 of 2026-09-30). The 3D options were rejected. Decorations appear on main buttons only, and one idle loop per screen (FR-019). The font is bundled, so offline still works. | PASS |
| Workflow gates | Spec → plan → tasks → implement. CI stays manual. No rules change, so the goldens are untouched. | PASS |

Post-design re-check (after `research.md`, `data-model.md` and `contracts/`): still PASS. The only deviations are the
font file, which the owner approved, and the painter label-look parameter. Both are justified below.

## Project Structure

### Documentation (this feature)

```text
specs/003-cartoon-ui-style/
├── spec.md                 # the feature spec (with the owner's answers)
├── plan.md                 # this file
├── research.md             # decisions R1–R12
├── data-model.md           # recipe tokens, color sets, label look, booster tile, decoration, font
├── quickstart.md           # validation commands and phone checks
├── contracts/
│   ├── garden-tokens.md    # the new token names and values
│   ├── painter-text.md     # the painter's label look (delta to spec 002 contracts/painter.md)
│   ├── booster-tile.md     # booster states → drawing, both clients
│   └── fonts.md            # files, license, loading in Unity / Android / Skia, fallback
├── *.jpg, *.png            # the mockup stills the owner reviewed
├── checklists/requirements.md
└── tasks.md                # /speckit-tasks
```

### Source Code (repository root)

```text
client/Assets/Bloomlings/
├── UI/Design/                       # engine-free kit, linked into the playtest
│   ├── DesignTokens.cs              # + Garden (plate, frame, lip, highlight, decoration), sentence-case type styles,
│   │                                #   ColorSet sets, TextLook looks, booster tile sizes
│   ├── GardenLook.cs                # NEW: ColorSet, TextLook, BoosterTileState, decoration placement (engine-free)
│   ├── ShapeLibrary.cs              # + ui.deco.garden (leaves and flower, by part), ui.play
│   ├── ScreenLayout.cs              # main-button proportions (PLAY 540×204), card buttons narrower
│   └── AssetSlots.cs                # + ui.deco.garden (FR-030)
├── UI/Fonts/Resources/              # NEW: Nunito-SemiBold.ttf, Nunito-ExtraBold.ttf, OFL.txt
├── UI/UiFonts.cs                    # NEW: runtime TMP font assets and shared label materials
├── UI/UiKit.cs, UI/UiTheme.cs       # plate + raised button, volumetric labels, cards, tabs, toggles, decoration
├── UI/Gameplay/BoosterBar.cs        # booster tiles (FR-031)
├── Gameplay/Board/TileView.cs, BoardView.cs     # volumetric 2D cells, wooden board frame
├── Gameplay/Tray/PodView.cs, TrayView.cs        # volumetric pods
├── Gameplay/Slots/SlotRowView.cs                # sunk wells, volumetric slot pods
├── UI/Screens/*.cs                  # HomeScreen PLAY, card buttons (proportions), titles
└── Tests/EditMode/Garden*Tests.cs   # NEW kit tests

client/DotnetCheck/UnityStubs.cs     # + Font, Resources.Load<Font>, TMP_FontAsset, VertexGradient, Material keywords

playtest/
├── design/
│   ├── IPainter.cs                  # Text / TextLeft gain an optional TextLook (contracts/painter-text.md)
│   ├── PainterBase.cs               # font loading hook
│   ├── Kit.cs                       # Plate, Raised (garden), buttons, pills, badges, cards, tabs, toggles, Decoration
│   ├── BoardPainter.cs              # volumetric cells, wooden frame
│   ├── PodPainter.cs, SlotPainter.cs
│   ├── BoosterBarPainter.cs         # booster tiles
│   └── HomeScreen.cs, MenuCards.cs, EndCards.cs, MetaCards.cs, LevelScreen.cs   # proportions, titles
├── android/Design/AndroidPainter.cs # Nunito typeface, label look (gradient, outline, extrusion)
├── android/*.csproj                 # embeds the two font files (full playtest only)
└── preview/SkiaPainter.cs, *.csproj # Nunito typeface, label look; embeds the font files
```

**Structure Decision**:
- Everything stays inside the spec 002 structure.
- The font sits in the Unity tree because the product client owns it; the playtest and the preview embed the same
  files.
- No new project.

## Complexity Tracking

| Deviation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| One font file family (Nunito, 2 weights) despite "no assets" | The owner chose it (spec Clarifications 2026-10-01, Q 1A). The reference look needs rounded letters, with Cyrillic. | The system font loses the look; Fredoka has no Cyrillic. |
| `IPainter.Text` gains an optional label look | Labels on colored faces need a gradient fill, an outline and an extrusion (FR-009). Only the painter can draw these efficiently on each host. | Drawing the same text several times from the kit cannot do a gradient fill, and would multiply the draw calls. |
| Up to 7 layered images per Unity button | The plate, the outline, the lip, the face and the highlight are separate shapes in the reference. | One baked sprite per button size would be an art asset, and would not follow the tokens. |
