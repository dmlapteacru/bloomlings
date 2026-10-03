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
    daily reward pill, the win top bar fade (reverted in the review round below: Pause stays usable over the win card), confetti only above the win and milestone cards (`UiKit.Confetti`), the
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
- [X] T023 Run every suite (core, client check, backend, playtest check, preview, art check, Android type-checks)
  - On the merged branch after the review round: core 400, client check 243, backend 5, playtest check (282 runs),
    preview 26 frames "checks: OK", art check 58 pictures, both Mono.Android type-checks 0 errors.

## Review round (2026-10-02)

Three reviewers (regressions, performance, conventions) checked the finished tasks; this round fixes what they
confirmed:
- Regressions (code): in Unity the jam sheet covered the Pause card (their order); Pause could not be reached over the
  win card in either build, so Home, Restart and Settings were gone there (FR-002: Pause stays usable, look.md §4.4);
  the Unity board could keep the previous level's restored ground; the Unity Wardrobe did not refresh after a cosmetic
  bought from it in the Store (it now listens to `WardrobeService.Changed`). In the playtest Pause now also takes taps
  through the win card's scrim (before spec 005 the playtest's scrim blocked it; Unity always allowed it).
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

## Owner review (2026-10-02)

The owner asked for the reference's layouts on gameplay, the jam, the win and Home, a centered jam card, a more colorful
gameplay, gem board icons, and his own booster icons and leaves (spec.md FR-020 to FR-027).
- [X] T024 Foundation the screens build on: the reference layouts as engine-free regions (`ScreenLayout.ReferenceGameplay`,
  `JamCard`, `WinScreen`, `ReferenceHome`, `ReferenceWardrobe`, `PodDeck`, which `PodChip` replaced in T031; look.md §6
  with the numbers fixed there) with
  `ReferenceLayoutTests`; the board's gem icons (`ShapeLibrary.GemSymbol`, §3.1.2); the owner's icon and leaf pictures in
  both builds (`Art/Icons/`, `Art/Decor/`, `OwnerPictures`, `Kit.OwnerPicture`, `OwnerArt.Icon`/`Decor`/`Show`, mirroring
  through `PushSquash(-1, 1)` and a negative `localScale`, §3.10); the lush gameplay garden (`foliage.*` tokens) and the
  win's garden (`BackdropScene.Win`, `bg.win`, §4.2); grass cells for the picture's background (`UiRaster.Grass`,
  `tile.grass`, §4.1); the slots, the inventory and the docs.

## Owner delivery: animated heroes and layered Home (2026-10-02)

The owner sent the Home picture in layers and the four heroes as animated FBX models, with the clips to use (spec.md
FR-028, research D18 and D19, contracts/look.md §3.12, §6.3 and §6.4, pictures.md A10 and B1).
- [X] T025 `tools/heroanim`: the offline bake of the four FBX heroes into 12 fps frames (`bake.mjs`, `page.html`,
  `serve.mjs`, `png8.mjs`, `heroes.json`; 288 palette PNG files in `Art/Heroes/Resources/HeroMotion/` with their
  `manifest.json`), the Home layers (`layers.mjs`: the garden as JPEG, the cropped fountain layers, the lotus cut-out,
  one shadow, the petals), `check.mjs`; the kit's `HeroMotion`, `HeroMotionPlayer`, `HomeLayers` and `HomeMotion`
  with the generated `HeroMotionData.cs` and `HomeLayersData.cs` and `HeroMotionTests`; the source record
  (`tools/heroanim/SOURCE.md`) and the notices rows (done in a0c7f4a)
- [X] T026 Playtest hosts (`playtest/design`, both painters, the preview): the layered Home and the splash with the
  animated heroes (layer order, shadows, the drifting petals, `HomeMotion` from the splash on, hero taps cut clear of
  every Home control, outfits once the Wardrobe is open), the win's and the milestone's animated hero, continuous redraw
  while they move, the frames decoded on first use into a cache bounded by bytes, the preview's frame check
- [X] T027 Unity hosts: `HomeLayersView` and `HeroMotionView` (Home and the splash sharing one motion, the touch boxes
  under the screen's controls, outfits), the win's and the milestone's animated hero (`HeroPictures`), `HeroFrames`
  (frames loaded one at a time, unloaded when no view holds the family), `Editor/HeroMotionImporter`, the stage choice
  (`HeroPictures.StageOf`, `HomeStage.ShowsHeroes`), the client check's stubs
- [X] T028 Asset slots and inventory: `bg.home.fountain_back`, `bg.home.lotus`, `bg.home.fountain_front`,
  `bg.home.shadow`, `bg.home.petals` and `char.hero3d.motion.{family}` in `AssetSlots`, the changed `bg.home`,
  `brand.splash_art`, `char.hero.home` and `char.hero3d.cheer.*` rows; regenerate
  `specs/002-ux-design-board/asset-inventory.md`
- [X] T029 Docs: spec.md (clarification, FR-024 amended, FR-028, SC-006 to SC-009), research D18 and D19, look.md
  §3.12, §4.4, §4.5, §5, §6.3 and §6.4, pictures.md (A10, B1's layers, H), this section, plan.md's addendum,
  `CLAUDE.md`, `playtest/README.md`, `client/README.md`, the originality checklist (the Meshy plan), `tools/heroanim`'s
  README and source record
- [X] T030 Run every suite (core, client check, backend, playtest check, preview, art check, `tools/heroanim` check,
  Android type-checks) and review the layered Home, the splash and the win in the preview at 16:9, 19.5:9 and 21:9;
  move the progressed Home's rank pill out of the right column, where it covers Drop's head (look.md §6.4)
  - The rank pill now lies in the top row between Settings and the Petals pill (`ReferenceHomeRegions.Rank`, both
    builds); both builds decide the layered Home by one kit rule (`HomeLayers.IsLayered`); the heroes' pose lookups no
    longer allocate (`HeroMotion`'s clip index and frame names, `CharacterArt.FamilyName`).

## Owner review: the tray's pods in columns (2026-10-03)

The owner made the tray's layout a gameplay rule: a stack's pods go one after another and are never drawn on each
other, with three or four rows visible and the boxes resized to fit. See spec.md (the clarification of 2026-10-03,
FR-021 amended, SC-010), research D20 and contracts/look.md §3.7, §4.1 and §6.1.
- [X] T031 Kit layout and tests (done in ff782ed):
  - `ReferenceGameplayRegions` gets `Columns`, `PodRows` (4 from `FourRowsAspect` = 1.95, else 3), `FrontHeight`,
    `QueueHeight`, `PodGap`, `Shows`, `Pod` and `Chip`.
  - `PodChip` replaces `PodDeck`: the frame, the inner panel, the square tile at the left, the count at the right and
    the "+N" disc.
  - The slot row, the booster row and the separators shrink to `0.16W`, `0.18W` and `0.03W`.
  - `ReferenceLayoutTests` check the grid, the exposed pods' touch boxes and the board's third.
- [X] T032 Playtest (`playtest/design`, the preview):
  - `PodPainter.DrawColumns` and `Kit.Pod` draw the columns: the exposed pod bright and alone taking taps, the
    waiting pods muted, "+N" on the last shown pod and the emptied stack's well.
  - The pods slide a row when one leaves or Return puts one back (`TrayMotion`); flights leave from and return to
    the pods' tiles.
  - The asset slots `pod.card`, `pod.deck` and `pod.count`, the regenerated inventory and the preview's component
    sheet follow.
- [X] T033 Unity (`client/Assets/Bloomlings`):
  - `GameplayHud.PodGrid`, `TrayView`, `PodView` and `UiKit.GridPod` (`UiKitTray.cs`, replacing `UiKitDeck.cs`)
    lay out and draw the grid with the same recipe.
  - The slide (`PodView.SlideSeconds`) and the touch boxes match the playtest.
  - The client check's tests and stubs follow.
- [X] T034 Docs:
  - spec.md: the clarification, FR-013, FR-020 and FR-021 amended, an edge case, the pod chip and SC-010.
  - research D20, look.md §3.1, §3.7, §4.1, §6 and §6.1, plan.md's addendum and this section.
  - spec 003 FR-022a's note, `CLAUDE.md`, `playtest/README.md` and `client/README.md`.
- [X] T035 Run every suite (core, client check, backend, playtest check, preview, art check, `tools/heroanim` check,
  Android type-checks).
  - Review the gameplay frames in the preview at 16:9, 19.5:9 and 21:9: three and four rows; two to six stacks; deep,
    mystery, locked and connected stacks.
  - Build the playtest APKs (a manual `android-apk.yml` run).

## Owner review: the pod's look "E" (2026-10-03)

The owner asked for smaller digits and the focus on the icon, chose "E" from six mock-ups. See spec.md (the
clarification "E", FR-013, FR-021), research D21 and contracts/look.md §3.7, §4.1 and §6.1.
- [X] T036 Kit (`ReferenceLayout.cs`): `PodChip` narrows the frame to `Aspect` = 1.3 of its height in the middle of its
  place, centers `Tile` (1.04 of the panel) and `Icon` (4% larger each side) on the panel, puts `Count` at the panel's
  bottom right corner (36% of the height, `CountLook`: white outline) and `Badge` at the frame's top left;
  `ReferenceLayoutTests` check the parts.
- [X] T037 Playtest: `Kit.Pod` draws the owner's icon alone (0.7 when waiting; the sticker tile for a mystery pod or a
  missing picture, the padlock when locked) and `Kit.PodCount`; the link bars and rings go by the frames, the ring on
  the top right corner.
- [X] T038 Unity: `GridPodView` gets the icon image and the outlined count at the same places; `TrayView` puts the
  links on the frames and the ring on the top right corner.
- [X] T039 Docs (spec.md, research D21, look.md, plan.md, `CLAUDE.md`, the READMEs, the asset slots and the inventory)
  and every suite; the preview's frames 7, 8, 12 and 25 reviewed at 16:9, 19.5:9 and 21:9; the playtest APK built.

## Owner delivery: the heroes at 60 fps (2026-10-03)

The owner sent the four FBX heroes again, exported at 60 fps, to replace the others. See spec.md (the clarification
"the owner's 60 fps models", SC-006, SC-007) and research D22.
- [X] T040 `tools/heroanim`: the models replaced (`SOURCE.md` with the new files and hashes); `page.html` finds a clip by
  its Meshy id at the end of its name and plays it at the table's length (`heroes.json` `idleSeconds`,
  `reactSeconds`); the bake at 24 fps: 576 frames, the regenerated `HeroMotionData.cs` and manifests.
- [X] T041 The hosts: the playtest's frame cache at 60 MiB (`PainterBase.HeroFrameCacheBytes`), the frame counts in
  `HeroFramesTests` and the comments; every suite; the playtest APK built.

## Owner review: Twig celebrates; Twig's new model (2026-10-03)

See spec.md (the clarification "Twig celebrates") and research D23.
- [X] T042 The kit's `CharacterArt.Celebrant` (Twig) replaces the level's main family on the win and the milestone in
  both builds (`EndCards`, `WinScreen`, `MilestoneCard`; `Visuals.MainFamily` and `HeroPictures.MainFamily` removed),
  with its test and the docs.
- [ ] T043 The owner's new `twig.fbx`: waits for its colors (research D23). Then the bake takes its rig, clips and lengths
  (idle `Twig_Breathing`, reaction `Twig_SmallBounce`, the win's `Twig_WinCheer`) and the hosts play the win clip.
