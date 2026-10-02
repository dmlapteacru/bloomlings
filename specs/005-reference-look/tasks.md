# Tasks: Reference Look

**Input**: `spec.md`, `plan.md`, `research.md`, `contracts/look.md`, `pictures.md`

## Phase 1: Setup

- [X] T001 Save the owner's reference as `specs/005-reference-look/reference.jpg` and write spec, research, plan,
  contracts and the picture list
- [X] T002 Change the variant colors in `core/src/Bloomlings.Core/Variants/VariantCatalog.cs` (research D1) and refresh
  `content/readability/pairs-report.json` with the pipeline's `readability` command (66/66 candidates)

## Phase 2: Foundational (blocks every story)

- [X] T003 Add the material and UI tokens of contracts/look.md §1.2 to `DesignTokens.Colors` (and its `All` map) and
  retune `ButtonPrimary`; add `GardenLook.Blue` (jam), `Orange`, cream `White` in
  `client/Assets/Bloomlings/UI/Design/DesignTokens.cs` and `GardenLook.cs`
- [X] T004 Add `client/Assets/Bloomlings/UI/Design/UiRaster.cs` (plank, frame, stone, candy tile; deterministic,
  straight alpha) with EditMode tests in `client/Assets/Bloomlings/Tests/EditMode/UiRasterTests.cs`
- [X] T005 Add the picture primitive: `IPainter.Picture`, `PainterBase`, `playtest/preview/SkiaPainter.cs`,
  `playtest/android/Design/AndroidPainter.cs`; `ProceduralSprites.Picture` in Unity (+ stubs)
- [X] T006 Redraw the variant symbols and add the lotus, booster icons, `ui.fast`, `ui.back`, ivy and flower cluster
  shapes in `client/Assets/Bloomlings/UI/Design/ShapeLibrary.cs`
- [X] T007 Restyle and add the kit components in `playtest/design/Kit.cs` (§3) and their twins in
  `client/Assets/Bloomlings/UI/UiKit.cs`
- [X] T008 Background and logo picture hooks with fallbacks (playtest embeds `Art/Backgrounds`, `Art/Brand`; Unity
  `BackdropView`, Home logo) and the new asset slots in `AssetSlots.cs`
- [X] T009 A kit sheet preview frame (`playtest/preview/Fixtures.cs`) showing every component like the reference's
  UI strip

## Phase 3: User Story 1 — board and tray (P1)

- [X] T010 [US1] Lawn backdrop scene in `BackdropRaster.cs`
- [X] T011 [US1] Board: candy tiles, stone border, stone arch entries, pale restored ground, obstacles in
  `playtest/design/BoardPainter.cs`
- [X] T012 [US1] Pods, slots, booster bar, top bar in `PodPainter.cs`, `SlotPainter.cs`, `BoosterBarPainter.cs`,
  `LevelScreen.cs`
- [X] T013 [US1] Unity: `TileView`, `BoardView`, `SpecialView`, `FinishedPictureRenderer`, `SlotRowView`, `PodView`,
  `TrayView`, `BoosterBar`, `GameplayHud`, `BackdropView`

## Phase 4: User Story 2 — buttons, cards, popups (P2)

- [X] T014 [US2] Jam sheet, pause, settings, store, daily reward, collection, leaderboard, themes in
  `playtest/design/EndCards.cs`, `MenuCards.cs`, `MetaCards.cs`; jam and win strings in `Strings_en.csv`
- [X] T015 [US2] Unity: `JamScreen`, `PauseScreen`, `SettingsScreen`, `StoreScreen`, `DailyRewardPopup`,
  `DailyChallengeScreen`, `CollectionScreen`, `LeaderboardScreen`

## Phase 5: User Story 3 — celebration and meta (P3)

- [X] T016 [US3] Win and milestone (sign, full-color picture, pedestal, rays, petals, reward pill) in `EndCards.cs`
- [X] T017 [US3] Home and splash (wooden logo, level plaque, Play, pedestal) in `HomeScreen.cs`
- [X] T018 [US3] Unity: `WinScreen`, `MilestoneCard`, `HomeScreen`, `SplashScreen`, `WardrobeScreen`, `ProfileAvatar`

## Phase 6: User Story 4 — the owner's pictures (P4)

- [X] T019 [US4] `pictures.md` with every picture, size, path and slot

## Phase 7: Polish

- [X] T020 Re-render the 2D characters (`tools/artgen -- build --only 2d`) and pass `-- check`
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
- [X] T022 Regenerate `specs/002-ux-design-board/asset-inventory.md`; update `CLAUDE.md`, `playtest/README.md`,
  `client/README.md`; mark spec 004 FR-008, FR-009, FR-012 replaced
  - Also: spec 003 FR-006, FR-009 (titles), FR-013, FR-014, FR-015, FR-022, FR-023 and FR-031 marked as amended (looks
    only); the spec 002 token contracts carry the current greens and the new tokens; `pictures.md` names every slot
    (`bg.wardrobe` and `brand.tagline` registered) and the owner-picture flow of `tools/artgen` (`adopt`, the `owner`
    flag in `manifest.json`).
- [ ] T023 Run every suite (core, client check, backend, playtest check, preview, art check, Android type-checks)
  - Left for the final run after the review round below, on the merged branch.

## Review round (2026-10-02)

Three reviewers (regressions, performance, conventions) checked the finished tasks; this round fixes what they
confirmed:
- Regressions (code): in Unity the jam sheet covered the Pause card (their order); Pause could not be reached over the
  win card in either build, so Home, Restart and Settings were gone there (FR-002: Pause stays usable, look.md §4.4);
  the Unity board could keep the previous level's restored ground.
- Performance (code): the full-resolution readable ground texture, the Collection thumbnails and the 1024 px light-ray
  picture built on Unity's main thread, and Unity's unbounded picture cache; in the playtest, the win card redrawing for
  as long as it is open (it now rests after a celebration of about 8 s), the ivy and flower clusters rasterized as
  separate masks (now baked), the lawn backdrop rendered on the UI thread, the picture cache dropping everything at
  once (now least recently used first), the allocations of every picture draw, and fades and flights that animated
  picture sizes instead of transforms.
- Conventions (docs, registry, strings): the link palette and the win layout sizes as tokens (`state.link_2`/`_3`,
  `size.win_*`); `bg.wardrobe` and `brand.tagline` registered; owner pictures in the artgen folder (`adopt`);
  the task marks, the spec 003 and 004 amendment marks, the token contracts, the look contract's numbers (§3.7, §5),
  `CLAUDE.md` and both READMEs; four unused strings removed (`jam.free_rescue`, `jam.rescue`, `wardrobe.none`,
  `wardrobe.tab_bloomlings`), and `LocalizationTests` now also checks keys chosen by a condition, the family-built keys
  and the playtest's keys.
