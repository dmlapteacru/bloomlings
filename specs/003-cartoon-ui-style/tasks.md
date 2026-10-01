---
description: "Tasks for spec 003: the Garden look in the Unity client and the full playtest"
---

# Tasks: Cartoon UI Style — game-like garden buttons and a volumetric 2D board

**Input**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: The spec asks for tested contrast (FR-025), readability (FR-024), layout checks (FR-026) and font coverage
(SC-006), so this list includes kit tests. They run under `client/DotnetCheck` and the preview tool.

**Organization**: Tasks are grouped by user story. Paths are from the repository root. `K/` means
`client/Assets/Bloomlings/UI/Design/`, `U/` means `client/Assets/Bloomlings/`, `P/` means `playtest/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 buttons and controls, US2 cards, badges and rewards, US3 gameplay pieces.

---

## Phase 1: Setup

**Purpose**: The font files and their wiring into every build that draws the Garden look.

- [ ] T001 Copy `Nunito_800ExtraBold.ttf` and `Nunito_600SemiBold.ttf` from `@expo-google-fonts/nunito` 0.4.2 to
  `U/UI/Fonts/Resources/Nunito-ExtraBold.ttf` and `Nunito-SemiBold.ttf`, unmodified, with the OFL text of
  `@fontsource/nunito` 5.3.0 as `U/UI/Fonts/Resources/OFL.txt` (contracts/fonts.md "Files")
- [ ] T002 [P] Embed both font files in `P/preview/Bloomlings.Playtest.Preview.csproj` and
  `P/android/Bloomlings.Playtest.Android.csproj` as `fonts/Nunito-ExtraBold.ttf` and `fonts/Nunito-SemiBold.ttf`
  (full playtest only; `P/tester` unchanged)

---

## Phase 2: Foundational (blocking)

**Purpose**: The shared engine-free kit values and the painter and font plumbing every story draws with.

- [ ] T003 Add the Garden tokens to `K/DesignTokens.cs` (contracts/garden-tokens.md): the `garden.*` colors, the
  geometry (`outline_width` 3/2, `plate_inset` 11/8/5, `plate_depth` 6/3, `lip` 13/10/6, `highlight_alpha` 0.5,
  `highlight_height` 0.36, `frame_width`, `frame_depth`, `cell_lip` 14, `cell_highlight_alpha` 0.4, `pod_lip` 20,
  `deco_size` 0.75, `decorations` true), the sizes (`size.play` 540×204, `size.card_primary` 620×140,
  `size.card_secondary_width` 580, `size.booster_tile` 152×156), the label look (`outline_em` 0.036, `extrude_em`
  0.09, `shadow_alpha` 0.3), and register every new color in `Colors.All`
- [ ] T004 Set `Upper = false` on `type.title_caps`, `type.level_home`, `type.level_pill`, `type.button_large`,
  `type.button` and `type.button_secondary` in `K/DesignTokens.cs`; `type.badge` stays uppercase (spec 2A)
- [ ] T005 Update the motion tokens in `K/DesignTokens.cs`: `motion.press` 0.06 s down and 0.36 s spring back with
  one overshoot, plus `motion.breathe` (scale ≤ 1.04, period ≥ 1.2 s) and `motion.count_up` (FR-017 to FR-020)
- [ ] T006 Create `K/GardenLook.cs` (data-model.md): `ColorSet` (Face, Top = lighten 0.18, Lip = darken 0.25,
  Line = darken 0.42, a `Line` override for cream and white), the named sets `Garden.Sets` (green, cream, white, blue,
  dark, red, purple, the four boosters), `TextLook` (`OnColor(set)`, `Plain(color)`), the press curve
  (`PressDepth(seconds since down/up)` with one overshoot), the breathe curve, `BoosterTileLook` derived from
  Charges/Price/Selected/Usable/Affordable (Selected overrides; Disabled when `!Usable` or no charges and
  `!Affordable`), and `Decoration` placement (3 leaves at −40°, 15°, 70°, 1 flower, `deco_size` × button height,
  at the top-left and bottom-right corners)
- [ ] T007 [P] Add `deco.leaf`, `deco.flower` and `ui.play` shapes to `K/ShapeLibrary.cs`
- [ ] T008 [P] Register `ui.deco.garden` and `ui.play` in `K/AssetSlots.cs`, and switch `font.display` and
  `font.body` to the bundled Nunito placeholder (a new `PlaceholderKind.BundledFont`)
- [ ] T009 [P] Make `ScreenLayout.Home` use `size.play` for the PLAY box in `K/ScreenLayout.cs`, and add
  `ScreenLayout.CardButton(body, top, primary)` for the narrower centered card buttons (`size.card_primary`,
  `size.card_secondary_width`)
- [ ] T010 Add the optional `TextLook? look` to `Text` and `TextLeft` in `P/design/IPainter.cs` and
  `P/design/PainterBase.cs`, plus `Now` (seconds) and the release memory (`Released(box)` → seconds since a finger
  lifted inside the box) so screens can spring back (contracts/painter-text.md)
- [ ] T011 Load Nunito and draw the label look in `P/preview/SkiaPainter.cs`: `SKTypeface.FromStream` from the
  embedded fonts with the DejaVu fallback; shadow → extrusion (n = 4) → outline → gradient fill
- [ ] T012 Load Nunito and draw the label look in `P/android/Design/AndroidPainter.cs`: copy the embedded fonts to
  `CacheDir`, `Typeface.CreateFromFile`, system fallback logged once; the same drawing order with `BlurMaskFilter`
  and a `LinearGradient` shader
- [ ] T013 Feed `Now` and the finger release to the painter in `P/android/Design/DesignView.cs` and
  `P/preview/Program.cs`
- [ ] T014 Extend `client/DotnetCheck/UnityStubs.cs` with `Font`, `Resources.Load<T>`, `TMP_FontAsset.CreateFontAsset`,
  `TextMeshProUGUI.font`/`fontSharedMaterial`/`enableVertexGradient`/`colorGradient`, `VertexGradient`, `Material`
  keywords and floats, and `Shader.PropertyToID` as needed
- [ ] T015 Create `U/UI/UiFonts.cs`: `Display` and `Body` font assets from `Resources.Load<Font>`, created once;
  `Material(font, look)` shared per (font, look) with outline and a hard underlay for the extrusion
  (contracts/fonts.md "Materials"); `null` keeps the default font
- [ ] T016 Make `UiKit.Style`/`Label` in `U/UI/UiKit.cs` pick the font by `TypeStyle.Bold`, drop `UpperCase` for
  sentence-case styles, and apply a `TextLook` (vertex gradient + shared material) when given
- [ ] T017 [P] Add `U/Tests/EditMode/GardenLookTests.cs`: every set derives its shades; no `Line` is black; a white
  and `label_fill_bottom` label on each colored `Face` reaches 3:1 against its `Line` (FR-025); `Plain` has no
  outline or extrusion; only `type.badge` is uppercase; the booster look table; the press curve overshoots once and
  settles; the breathe stays ≤ 4% with a period ≥ 1.2 s
- [ ] T018 [P] Add `U/Tests/EditMode/GardenFontTests.cs`: both files exist with `OFL.txt`, and their `cmap` covers
  Basic Latin, Latin-1, Cyrillic, "×", "−" and the no-break space (SC-006); every character of `Strings_en.csv`
  is covered
- [ ] T019 [P] Extend `ShapeLibraryTests`, `AssetSlotTests` and `ScreenLayoutTests` in `U/Tests/EditMode/` for the new
  shapes, slots, the PLAY ratio (about 2.6:1) and the card button widths at 16:9 to 21:9

**Checkpoint**: `dotnet test client/DotnetCheck/...` passes and the preview renders with Nunito.

---

## Phase 3: User Story 1 — Buttons and controls feel like a game (P1) 🎯 MVP

**Goal**: Every button, pill, tab and toggle is a raised button on a cream plate, with volumetric sentence-case labels,
the approved press and spring-back, and the decorated PLAY with its full-height ▶.

**Independent Test**: Preview frames 2, 3, 4 and 17 and compare with `garden-direction.jpg` and
`garden-play-button.png`; on a phone, press and release PLAY, a secondary button and a round button.

- [ ] T020 [US1] Add `Kit.Plate` and the garden `Kit.Raised(p, box, set, size, press)` in `P/design/Kit.cs`: the
  plate (gradient, outline, depth, shadow), then the button (lip, face gradient Top → Face, outline in `Line`,
  highlight band), sinking into the lip by the press depth (FR-006, FR-007, FR-017)
- [ ] T021 [US1] Restyle `Kit.PrimaryButton`, `SecondaryButton`, `RoundButton`, `DarkPill`, `LevelPill` and
  `PetalsPill` (with its round green "+" on a plate) in `P/design/Kit.cs`: label looks (`OnColor` on colored faces,
  `Plain` on cream and white), disabled = greyed, no highlight, no press (FR-012, FR-013)
- [ ] T022 [US1] Restyle `Kit.Tabs` (selected raised on a plate, others sunk) and `Kit.Toggle` (outlined track,
  raised knob) in `P/design/Kit.cs` (FR-016)
- [ ] T023 [US1] Add `Kit.Decoration(p, box)` (leaves and a flower at two corners, `ui.deco.garden`, switchable,
  never a hit target) and the PLAY label with the `ui.play` triangle as tall as the letters in `P/design/Kit.cs`
  (FR-010, FR-011a)
- [ ] T024 [US1] Draw PLAY at `size.play` with its decoration and ▶ in `P/design/HomeScreen.cs`
- [ ] T025 [US1] Build the plate and raised layers in `U/UI/UiKit.cs` (`Plate`, `RaisedButton` for pills and circles:
  plate, depth, shadow, lip, face, outline, highlight; at most 7 images), and restyle `PrimaryButton`,
  `SecondaryButton`, `RoundIconButton`, `DarkPill`, `LevelPill` and `PetalsPill` with label looks
- [ ] T026 [US1] Replace `PressMotion` in `U/UI/UiKit.cs` with the garden press: the face sinks into its lip on down,
  springs back with one overshoot on up, unscaled time, never delaying the click (FR-017, FR-021)
- [ ] T027 [US1] Add `UiKit.Decoration` and the PLAY ▶ label in `U/UI/UiKit.cs`, and use them for PLAY at `size.play`
  in `U/UI/Screens/HomeScreen.cs`
- [ ] T028 [US1] Restyle the Settings toggles and the Store and Wardrobe tabs in `U/UI/Screens/SettingsScreen.cs`,
  `StoreScreen.cs` and `WardrobeScreen.cs` through new `UiKit.Tabs`/`UiKit.Toggle` helpers

**Checkpoint**: US1 is visible in the preview frames 2–4 and 17 and in both clients' Home.

---

## Phase 4: User Story 2 — Popups, badges and rewards pop like a game (P2)

**Goal**: Cards and the sheet in a wooden frame on paper with a header band and a red close, sticker badges, one
breathing primary button, and the Petals count-up with a sparkle burst.

**Independent Test**: Preview frames 4, 5, 6, 10, 11, 15, 16 and 17; on a phone, open pause, jam, win, milestone and
the Daily Reward, and watch them open and close.

- [ ] T029 [US2] Restyle `Kit.Card` and `Kit.Sheet` in `P/design/Kit.cs`: paper (`paper_top` → `paper_bottom`) in a
  wooden frame with depth and shadow, the header band as a raised button on a plate with the title in the `OnColor`
  look, the close as a red round button on a plate; the sheet settles with one small bounce (FR-015, FR-018)
- [ ] T030 [US2] Restyle `Kit.Badge` (raised pill on a plate), `Kit.CountBadge` (dark brown disc, cream ring, brown
  outline) and add `Kit.PriceTag` in `P/design/Kit.cs`; restyle `Kit.Row` (outlined panel, raised own row) and
  `Kit.Toast` (FR-014, FR-016)
- [ ] T031 [US2] Use `ScreenLayout.CardButton` for the card buttons in `P/design/EndCards.cs`, `MenuCards.cs` and
  `MetaCards.cs`, with the decoration on the primary button of pause, win, milestone and Daily Reward (FR-011, FR-011a)
- [ ] T032 [US2] Add the breathe to PLAY, CLAIM and CONTINUE while they wait (one per screen) in
  `P/design/HomeScreen.cs`, `MetaCards.cs` and `EndCards.cs` (FR-019)
- [ ] T033 [US2] Count the win Petals up from 0 and burst sparkles on the win and on the Daily Reward claim, never
  blocking NEXT, in `P/design/EndCards.cs` and `P/design/MetaCards.cs` (FR-020)
- [ ] T034 [US2] Restyle `UiKit.Card`, `UiKit.Sheet`, `UiKit.Badge` and `UiKit.CountBadge` in `U/UI/UiKit.cs` with the
  same recipe; `PopMotion` keeps one overshoot and settles within 0.35 s, `SheetMotion` gains the small bounce
- [ ] T035 [US2] Add `BreatheMotion` and `CountUp` to `U/UI/UiKit.cs` and use them in `U/UI/Screens/HomeScreen.cs`,
  `WinScreen.cs`, `MilestoneCard.cs` and the Daily Reward card; narrow card buttons in `WinScreen.cs`,
  `PauseScreen.cs`, `MilestoneCard.cs`, `JamScreen.cs` with `ScreenLayout.CardButton`
- [ ] T036 [US2] Restyle the HARD and SUPER HARD stickers in `U/UI/Screens/DifficultyBanner.cs` and
  `P/design/LevelScreen.cs`

**Checkpoint**: every card and the sheet show the full recipe in the preview and both clients.

---

## Phase 5: User Story 3 — Volumetric gameplay pieces and board, still readable (P3)

**Goal**: The board in a wooden frame with volumetric cells, volumetric pods and sunk slot wells, and the booster tiles,
with every spec 002 state still readable.

**Independent Test**: Preview frames 7–9 and 12–14 and compare with `booster-variants.jpg`; the readability and layout
checks still pass.

- [ ] T037 [US3] Draw the board's wooden frame (`frame_width`, `frame_depth`) and volumetric cells (lip
  `cell_lip`, highlight `cell_highlight_alpha`, 2-unit outline in the cell's `Line`, the symbol on the face above the
  lip) in `P/design/BoardPainter.cs` and `P/design/Visuals.cs` (FR-023, FR-024)
- [ ] T038 [US3] Draw volumetric pods (lip `pod_lip`, bevel, highlight, outline) and stacked layers in
  `P/design/PodPainter.cs`, keeping every frame 12 state (FR-022)
- [ ] T039 [US3] Draw the slot row in a wooden frame with sunk wells (`well`, `well_edge`) and volumetric slot pods in
  `P/design/SlotPainter.cs`, keeping every frame 13 state and the red dashed danger frame (FR-022)
- [ ] T040 [US3] Replace the round booster buttons with booster tiles in `P/design/BoosterBarPainter.cs`
  (contracts/booster-tile.md): plate, tile, highlight, icon, ×N badge or price tag with "+", selected glow and ring
  with a 1.2 s pulse, disabled grey, squash on press; keep the jam sheet and Store users working (FR-031)
- [ ] T041 [US3] Volumetric cells and the board frame in `U/Gameplay/Board/TileView.cs` and `BoardView.cs`
- [ ] T042 [US3] Volumetric pods in `U/Gameplay/Tray/PodView.cs` and `TrayView.cs`; sunk wells and the slot frame in
  `U/Gameplay/Slots/SlotRowView.cs`
- [ ] T043 [US3] Booster tiles with every state in `U/UI/Gameplay/BoosterBar.cs`, driven by `BoosterTileLook`

**Checkpoint**: frames 7–9 and 12–14 show the Garden pieces; readability and layout checks pass.

---

## Phase 6: Polish and cross-cutting

- [ ] T044 [P] Regenerate `specs/002-ux-design-board/asset-inventory.md` (`playtest/preview -- --inventory`) and note
  the Garden recipe on each UI entry (FR-030)
- [ ] T045 [P] Add a before/after sheet to the preview tool (`--before <png>`), and commit
  `specs/003-cartoon-ui-style/preview-before-after.png` from the spec 002 sheet and the new one (FR-029)
- [ ] T046 [P] Update `CLAUDE.md`, `playtest/README.md`, `client/README.md` and
  `specs/002-ux-design-board/contracts/design-tokens.md` (sentence case, the Garden tokens, the font)
- [ ] T047 Run the quickstart checks 1–5 (`client/DotnetCheck`, `playtest/preview`, `--inventory`, `playtest/check`,
  `core`, `backend`) and type-check the full playtest and the tester against `Mono.Android`

---

## Dependencies and execution order

- **Setup (T001–T002)** → **Foundational (T003–T019)** → the stories.
- **US1** needs T003–T016. **US2** builds on US1's `Kit.Raised` and `Plate` (T020, T025). **US3** needs T006 and
  T020 (the booster tile uses the plate and the press).
- Within Foundational: T003–T006 first; T007–T009 in parallel; T010 before T011–T013; T014 before T015–T016; tests
  T017–T019 in parallel once their code exists.
- Polish runs last.

## Parallel examples

- T007, T008 and T009 touch different kit files.
- T011 (Skia) and T012 (Android) share only the contract.
- In US3, the playtest painters (T037–T040) and the Unity views (T041–T043) are independent.

## Implementation strategy

1. Setup and Foundational, then **US1** (the MVP: every button in the Garden look). Check the preview.
2. **US2** (cards, badges, motion), then **US3** (gameplay pieces, booster tiles).
3. Polish: inventory, before/after sheet, docs, every local check, then one push.
