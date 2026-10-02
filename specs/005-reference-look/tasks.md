# Tasks: Reference Look

**Input**: `spec.md`, `plan.md`, `research.md`, `contracts/look.md`, `pictures.md`

## Phase 1: Setup

- [X] T001 Save the owner's reference as `specs/005-reference-look/reference.jpg` and write spec, research, plan,
  contracts and the picture list
- [X] T002 Change the variant colors in `core/src/Bloomlings.Core/Variants/VariantCatalog.cs` (research D1) and refresh
  `content/readability/pairs-report.json` with the pipeline's `readability` command (66/66 candidates)

## Phase 2: Foundational (blocks every story)

- [ ] T003 Add the material and UI tokens of contracts/look.md §1.2 to `DesignTokens.Colors` (and its `All` map) and
  retune `ButtonPrimary`; add `GardenLook.Blue` (jam), `Orange`, cream `White` in
  `client/Assets/Bloomlings/UI/Design/DesignTokens.cs` and `GardenLook.cs`
- [ ] T004 Add `client/Assets/Bloomlings/UI/Design/UiRaster.cs` (plank, frame, stone, candy tile; deterministic,
  straight alpha) with EditMode tests in `client/Assets/Bloomlings/Tests/EditMode/UiRasterTests.cs`
- [ ] T005 Add the picture primitive: `IPainter.Picture`, `PainterBase`, `playtest/preview/SkiaPainter.cs`,
  `playtest/android/Design/AndroidPainter.cs`; `ProceduralSprites.Picture` in Unity (+ stubs)
- [ ] T006 Redraw the variant symbols and add the lotus, booster icons, `ui.fast`, `ui.back`, ivy and flower cluster
  shapes in `client/Assets/Bloomlings/UI/Design/ShapeLibrary.cs`
- [ ] T007 Restyle and add the kit components in `playtest/design/Kit.cs` (§3) and their twins in
  `client/Assets/Bloomlings/UI/UiKit.cs`
- [ ] T008 Background and logo picture hooks with fallbacks (playtest embeds `Art/Backgrounds`, `Art/Brand`; Unity
  `BackdropView`, Home logo) and the new asset slots in `AssetSlots.cs`
- [ ] T009 A kit sheet preview frame (`playtest/preview/Fixtures.cs`) showing every component like the reference's
  UI strip

## Phase 3: User Story 1 — board and tray (P1)

- [X] T010 [US1] Lawn backdrop scene in `BackdropRaster.cs`
- [X] T011 [US1] Board: candy tiles, stone border, stone arch entries, pale restored ground, obstacles in
  `playtest/design/BoardPainter.cs`
- [ ] T012 [US1] Pods, slots, booster bar, top bar in `PodPainter.cs`, `SlotPainter.cs`, `BoosterBarPainter.cs`,
  `LevelScreen.cs`
- [X] T013 [US1] Unity: `TileView`, `BoardView`, `SpecialView`, `FinishedPictureRenderer`, `SlotRowView`, `PodView`,
  `TrayView`, `BoosterBar`, `GameplayHud`, `BackdropView`

## Phase 4: User Story 2 — buttons, cards, popups (P2)

- [ ] T014 [US2] Jam sheet, pause, settings, store, daily reward, collection, leaderboard, themes in
  `playtest/design/EndCards.cs`, `MenuCards.cs`, `MetaCards.cs`; jam and win strings in `Strings_en.csv`
- [X] T015 [US2] Unity: `JamScreen`, `PauseScreen`, `SettingsScreen`, `StoreScreen`, `DailyRewardPopup`,
  `DailyChallengeScreen`, `CollectionScreen`, `LeaderboardScreen`

## Phase 5: User Story 3 — celebration and meta (P3)

- [ ] T016 [US3] Win and milestone (sign, full-color picture, pedestal, rays, petals, reward pill) in `EndCards.cs`
- [X] T017 [US3] Home and splash (wooden logo, level plaque, Play, pedestal) in `HomeScreen.cs`
- [X] T018 [US3] Unity: `WinScreen`, `MilestoneCard`, `HomeScreen`, `SplashScreen`, `WardrobeScreen`, `ProfileAvatar`

## Phase 6: User Story 4 — the owner's pictures (P4)

- [X] T019 [US4] `pictures.md` with every picture, size, path and slot

## Phase 7: Polish

- [ ] T020 Re-render the 2D characters (`tools/artgen -- build --only 2d`) and pass `-- check`
- [X] T021 Side-by-side review of every preview frame against the reference; fix what differs
  - Playtest and engine-free kit done (art director's review): bigger embossed board beads, pillowy tiles, darker ink and
    `ink.title`, a warm scrim, a sunny lawn, cream booster bezels, bigger pod counts, near-rectangular border stones,
    arches on piers with lawn below, soft win rays, the group picture without its own base (`HomeStage.Celebration`),
    the win card animating while open, one close button per card stack, hats on the heroes' heads, the optional
    celebrating heroes (`char.hero3d.cheer.*`).
  - Unity twins mirrored (`UiKit*`, screens): `Kit.Card`/`Sheet` and the win sign's titles in `ink.title`, the Super
    Hard sign letters darkened, booster bezel lip and line (also the Store's booster tiles), pod tile 62%, count ×1.05
    and the darker sinking pressed pod, border stone radii, outlines and joints (board and win picture), arch piers via
    `EntryArch.Picture` with the foot shadow, the vivid special blocks and their bigger counter, the rays' radial glow,
    the softer ivy, charge pills with the booster icon, one close per card stack (`CardStackMember`), the lotus-first
    daily reward pill, the win top bar fade, confetti only above the win and milestone cards (`UiKit.Confetti`), the
    group on `HomeStage.Celebration` and the level's celebrating hero on the milestone too, hats on the heroes' heads,
    the hero fallback in its family color, the flying pod as its sticker tile, the warm Home/splash/Wardrobe backdrop
    (`HomeStage.Garden`), the wooden logo's mossy band with leaves and pink flowers, and one shared outfit card
    (`UiKit.OutfitCard`, the Store and the Wardrobe). Checked with the client check and the uGUI simulator; the look in
    the Unity Editor and on a device stays with the checklists (`specs/001-core-game-mvp/checklists/`).
- [ ] T022 Regenerate `specs/002-ux-design-board/asset-inventory.md`; update `CLAUDE.md`, `playtest/README.md`,
  `client/README.md`; mark spec 004 FR-008, FR-009, FR-012 replaced
- [ ] T023 Run every suite (core, client check, backend, playtest check, preview, art check, Android type-checks)
