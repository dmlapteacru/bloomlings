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
- [X] T043 The owner's new Twig (`Twig.glb`, after two FBX files without colors; research D23): the bake reads `.glb`
  models, hides non-skinned meshes, keeps the clips' own spans and bakes a third clip (`win`); `twig.glb` replaces
  `twig.fbx` (72 idle, 36 reaction, 72 win frames). The kit's `MotionClip.Win`, `HasWin` and
  `HeroMotionPlayer.Celebrate`, played by `EndCards` and `HeroMotionView`; the frame sets (`HeroFrameSet`,
  `HeroBitmaps`, `HeroFrameCheck`) hold it. Twig's place on Home (0.815, 0.31) and `HatLift` 0; the tests and docs.
- [X] T044 Twig's brightness (the owner's choice "H"): the bake's per-hero `light` and `color` grade
  (`tools/heroanim/page.html`, `bake.mjs`), Twig re-baked with them; the docs; the playtest APK built.

## Owner batch: no arch, the Petals pill (2026-10-03)

See spec.md (the clarification "the owner's batch"); the clearing pace and the side by side taps are spec 001's
(research R4, amendment of 2026-10-03).
- [X] T045 No arch under the board: `BoardLayout` without arches (`Fit` takes an optional max border width,
  `Door` / `DoorOf` on the border beside the entry cell), the plain `0.04W` entry strip in `ReferenceGameplay`
  (`entrySides` removed), `BoardPainter` and Unity's `BoardView` draw no arch and start the walkers at the door;
  `Kit.StoneArch`, `UiKit.StoneArch`, `ProceduralSprites.Arch`, `UiRaster.Arch`, `GardenLook.ArchOpening` and their
  tests removed; the slots `board.arch` and `tile.entry` retired, the inventory regenerated; the layout tests.
- [X] T046 The Petals pill fits its amount (the kit's `PetalsPillParts`, `Kit.PetalsPill`, `UiKit.PetalsPill` with
  `align`, `KitText.Measure` of any text): the lotus inside its left end, the amount right after it; Unity lays it out
  on every `Show`; the test `ThePetalsPill_FitsItsAmount_TheLotusInside_TheAmountRightAfterIt`.
- [X] T047 The docs (contracts/look.md §3, §5, §6.1, §6.4; FR-011, FR-020; CLAUDE.md); all checks; the playtest APK.

## Owner delivery: Sprig's Blender model; Twig and Sprig take turns celebrating (2026-10-03)

See spec.md (the clarification "Sprig's Blender model") and research D24.
- [X] T048 `tools/heroanim`: `sprig.glb` (`Sprig_Complete.glb`) replaces `sprig.fbx` (`SOURCE.md`); the bake takes a list
  of win clips (`win`, `win2`), a ground point at the model's base (`ground: "model"`) and a top point off a bone
  (`topOffset`); Sprig re-baked (96 idle, 72 wave, 48 celebrate, 72 clap frames), `HeroMotionData.cs` and the manifests
  regenerated; `check.mjs` passes.
- [X] T049 The kit's `MotionClip.Win2`, `HeroMotion.WinClip`, `HeroMotionPlayer.Celebrate(…, turn)` and
  `CharacterArt.Celebrants` / `CelebrantOf` / `CelebrationTurn` (replacing `Celebrant`); the playtest's `EndCards` and
  Unity's `WinScreen`, `MilestoneCard`, `HeroPictures`, `HeroMotionView` and `HeroFrames` pass the level's celebrant and
  turn; the tests (`HeroMotionTests`, `HeroFramesTests`, `WardrobeLayoutTests`).
- [X] T050 The docs (FR-028, SC-006, SC-007, pictures.md, contracts/look.md, research D24, CLAUDE.md); all checks; the
  playtest APK.

## Owner review: Twig and Sprig toned down (2026-10-04)

See spec.md (the clarification "Twig and Sprig toned down") and research D25.
- [X] T051 `heroes.json`: Twig's softer light and grade and Sprig's grade (variant "B"), both re-baked; `SOURCE.md`, the
  docs; all checks; the playtest APK.

## Owner notes: the Wardrobe's header on one line; the Store as a page (2026-10-04)

See spec.md (the clarification "the owner's notes: the Wardrobe's header on one line; the Store as a page", FR-025,
FR-029) and contracts/look.md §6.4, §6.5, §6.6. Presentation only (FR-002).
- [X] T052 Home's one Wardrobe button: the profile avatar first in the left column with a green shirt badge at its
  bottom right (`Kit.IconBadge`, `UiKit.IconBadge`), the shirt button removed, the profile badge at the avatar's bottom
  left (`ProfileAvatar`); the Wardrobe demo points at the avatar (Unity `HomeScreen.DemoTarget`); look.md §6.4.
- [X] T053 The kit's page header: `PageHeader` and `ScreenLayout.PageHeader` (the back button, the banner 0.1 W tall and
  the Petals pill's box on the back button's middle line, the banner's ivy clusters 0.005 W clear of both), used by
  `ReferenceWardrobe`; the sign leaves' boxes in the kit (`GardenLook.IvyBox`, `FlowerBox`, `SignExtent`, `IvyReach`),
  drawn by both builds' wooden signs (Unity's ivy moves to the playtest's place); `Kit.PageHeader` and its twin
  `UiKit.PageHeader` / `PageHeaderView`, used by both Wardrobes; the test
  `ThePageHeader_PutsBackBannerAndPetalsOnOneLine_AndTheBannersLeavesTouchNeither`.
- [X] T054 The Store page's regions: `ReferenceStoreRegions` and `ScreenLayout.ReferenceStore` (the header, the panel to
  the screen's bottom, the tabs, Unity's offline line, the list with rows from 0.15 W to 0.19 W tall a page at a time,
  the cosmetics' family tabs, lighter panel and outfit cards as many rows of three as fit, the footer with the page
  arrows); the test `TheStorePage_KeepsItsRegionsInOrder_AndEveryTargetReachable`.
- [X] T055 The playtest's Store page: `StoreScreen` (the Store card's code moved out of `MetaCards`), `Screen.Store` with
  `DesignApp.OpenStore`, `CloseStore` and `StoreReturn` (back to Home or the Wardrobe), `Overlay.Store` removed; Home's
  Store button and Petals "+" and the Wardrobe's Petals "+" open it; the system back (`DesignApp.Back`,
  `DesignView.Back`, `MainActivity`'s back callback and `OnBackPressed`) closes the Store page and the Wardrobe; the
  preview's frames 17 and 26 open it through those entry points and check its back.
- [X] T056 Unity's Store page: `StoreScreen` a full-screen page over the Wardrobe's garden on the kit's regions (the
  page header, the panel, the tabs, the offline line, rows filling the page with the page arrows, the cosmetics' outfit
  cards), opened by `HomeController`'s unchanged wiring over Home or the Wardrobe; its back hides it.
- [X] T057 The docs (contracts/look.md §3.2, §3.4, §3.5, §4.3, §4.6, §5, §6.5, the new §6.6; spec.md FR-017, FR-025, the
  new FR-029, the acceptance scenarios; pictures.md B7; the asset slots `bg.wardrobe` and `ui.card` and the inventory;
  `playtest/README.md`); all checks; the full preview reviewed at 16:9, 19.5:9 and 21:9.

## Owner delivery: the calm backgrounds (2026-10-04)

See spec.md (the clarification "the owner's calm backgrounds") and pictures.md B1–B5, B7, B8. Pictures only (FR-002).
- [X] T058 The owner's calm pack replaces the first pack's pictures: Home's back layer (`home.jpg` rebuilt by
  `tools/heroanim/layers.mjs` over the unchanged fountain layers, `layers.json`), the win (`win.jpg`), the Wardrobe
  (`wardrobe.jpg`) and the four gameplay themes, all JPEG q90; `OwnerPictures.WinStageShare` 0.6 for the new win
  stage; the source records (`client/THIRD_PARTY_NOTICES.md`, `tools/artgen/models/owner-pictures.md`,
  `tools/heroanim/SOURCE.md`).
- [X] T059 The bottom menu's five icons (`Art/Icons/Resources/Icons/nav-*.png`, trimmed and fitted to 512 × 512 with a
  20 px margin) and their source records; `node tools/heroanim/check.mjs`; the preview's frames 2, 7, 15 and 27 reviewed.

## Owner request: the bottom menu, wooden (2026-10-04)

See spec.md (the clarification "the owner's bottom menu, wooden", FR-030, the amended FR-017, FR-024, FR-025, FR-029,
SC-011) and contracts/look.md §6.4 to §6.7. Presentation and navigation only (FR-002).
- [X] T060 The kit's bottom menu (`BottomNav.cs`): `NavPlace`, `BottomNav` (the order, `Places(HomeLook)`, keys, slots,
  the stand-in glyphs), `BottomNavRegions` (the bar, the plank, the places, the icons, the touch boxes, the medallion and
  its disc, the top) and `ScreenLayout.BottomNav` / `BottomNavTop`; the tests
  `TheBottomMenu_SpreadsItsPlacesInOrder_AndRaisesTheActiveOne` and `TheBottomMenu_ShowsAPlaceOnlyOnceItsFeatureIsUnlocked`.
- [X] T061 The menu's pictures (`NavRaster.cs`, `UiRaster` now partial): `UiRaster.NavBar` (the warm brown plank with its
  grain, grooves, and vines with leaves and white flowers at both ends, from `BottomNavRegions.Shape`) and
  `UiRaster.NavMedallion` (the honey disc in its darker rim with leaves and two flowers); the slots `ui.nav.bar`,
  `ui.nav.medallion` and `icon.nav.*` (the new `icon.` prefix), `OwnerPictures.NavIcon` and its slots; the test
  `TheBottomMenusWood_CoversItsPlank_AndTheMedallionIsADisc`.
- [X] T062 The playtest: `Kit.BottomNav` and `Kit.NavIcon` (`KitNav.cs`), `DesignApp.Navigate` and `ActivePlace`; the
  menu on Home (`HomeScreen.Nav`), the Store page and the Wardrobe; Home without its Store, Wardrobe (the avatar) and
  Collection side buttons and the rank pill, the heroes' taps clear of the menu; the dev row moved into the Settings
  card opened from Home (`MenuCards.Settings`, `HomeScreen.DevReserve` 0); the preview's frames 17 and 27 walk the
  menu's places.
- [X] T063 Unity: `UiKit.BottomNav` / `BottomNavView` (`UiKitNav.cs`) in `HomeScreen`, `StoreScreen` and
  `WardrobeScreen`; `HomeController.Navigate` (the Store page, the Wardrobe, Home, the Leaderboard and Collection cards
  over Home) and the places from the unlocks; Home without its Store, Collection and avatar buttons and the rank pill
  (`HomeModel` without the rank text and the avatar, `HomeFeatureActions` the Daily Challenge only); the Leaderboard,
  Store and Wardrobe demos point at the menu's places (`HomeScreen.DemoTarget`).
- [X] T064 The layouts above the menu: `ReferenceHome` bottom up from the menu's top (Play 0.15 H unless the plaque
  would rise above 60% of H; `Wardrobe`, `Collection`, `Store` and `Rank` removed, `NavTop` added), `ReferenceWardrobe`
  on the page's height ending at the menu, `ReferenceStore`'s list ending 0.02 W over it (rows from 0.135 W, their type
  sized for 0.15 W, `RowTypeShare`); the tests `Home_FollowsTheReference_AndKeepsEveryButtonReachable`,
  `TheWardrobe_FollowsTheReference_AndKeepsEveryButtonReachable` and `TheStorePage_KeepsItsRegionsInOrder_AndEveryTargetReachable`.
- [X] T065 The docs (contracts/look.md §4.5, §4.6, §5, §6, §6.4 to §6.6 and the new §6.7; spec.md; pictures.md D9–D13;
  `playtest/README.md`; contracts/asset-slots.md of spec 002) and the regenerated inventory; all checks; the preview's
  frames reviewed at 16:9, 19.5:9 and 21:9.

## Owner review: the bottom menu without its end vines, larger icons (2026-10-04)

See spec.md (the clarification "the owner's bottom menu, wooden", its last question, and FR-030) and
contracts/look.md §6.7. Presentation only (FR-002).
- [X] T066 The kit's menu geometry (`BottomNav.cs`): the plank `0.14W` (`PlankShare`), the places sharing `0.04W` to
  `0.96W` (`SpanStart`, `SpanEnd`), each icon the plank's full height (`IconShare` 1, no wider than its column), the
  medallion `0.2W` rising `0.03W` (`MedallionShare`, `RiseShare`) with its icon `0.86` of the disc
  (`MedallionIconShare`), its size on a phone without a bottom inset keeping its disc on the screen; the bar's box from
  the plank's top (`DecorShare` removed), the menu's top unchanged at `0.17W` over the safe bottom; the test
  `TheBottomMenu_SpreadsItsPlacesInOrder_AndRaisesTheActiveOne` updated.
- [X] T067 The bar's picture without its end vines (`NavRaster.cs`: the vines' stem, tendril, leaves and flowers
  removed; the medallion keeps its own); the test `TheBottomMenusWood_CoversItsPlank_AndTheMedallionIsADisc` (nothing
  beside the plank's ends); the slot rows `ui.nav.bar` and `ui.nav.medallion` and the inventory; contracts/look.md
  §6.7, spec.md FR-030; all checks; the preview's frames 2, 3, 17 and 27 reviewed.

## Owner review: every menu place always shown, locked ones say their level (2026-10-04)

See spec.md (the clarification "the owner's bottom menu, wooden", its last question, FR-030, acceptance scenario 6 and
SC-011) and contracts/look.md §6.7. Presentation and navigation only (FR-002): rules, the economy and unlocks stay.
- [X] T068 The kit's menu rules (`BottomNav.cs`): the five places always shown (`Order`; `Places` and `Shows` removed),
  `IsOpen(place, HomeLook)` (Home always, the others as they showed before), `UnlockLevel(place, levelOf)` from the
  build's roadmap (`HomeLook.StoreUnlock` 12, `WardrobeUnlock` 40, `LeaderboardUnlock` 10; `CollectionLevel` 2; Home 1),
  the padlock badge's box `LockBox` (0.34 of the icon at its lower right, inside the plank's band) and `LockDisc`;
  `HomeLook.All`; the slots `ui.nav.lock` and `ui.locked.notice`; the keys `locked.message` and `locked.hint`.
- [X] T069 The locked notice's layout (`LockedNotice.cs`): `LockedNoticeRegions` (the icon 0.4 of the area's width,
  smaller on a short area, the badge, the message and the hint lines, `CardContent`) from `ScreenLayout.LockedNotice`,
  and `LockedPageRegions` from `ScreenLayout.LockedPage` (the Store page's panel without tabs and status, the notice in
  the list's box); the tests `TheBottomMenu_ShowsEveryPlaceAlways_AndKnowsWhichAreOpenAndFromWhichLevel` (it replaces
  `TheBottomMenu_ShowsAPlaceOnlyOnceItsFeatureIsUnlocked`) and `TheLockedNotice_KeepsItsPartsInOrder_InsideItsArea_OnEveryPhone`.
- [X] T070 The playtest: `Kit.BottomNav` with the look (a locked place's icon with `Kit.NavLock`, the outfit cards'
  `Kit.LockBadge`), `Kit.LockedNotice` (`KitNav.cs`), `Kit.Panel` (the Wardrobe's lighter panel, `KitMeta.cs`),
  `DesignApp.PlaceOpen` and `UnlockLevel` (from `PlaytestMeta.Progression.Roadmap`), `HomeScreen.Nav` with all five
  places; the locked Store page (`StoreScreen.Locked`), the locked Wardrobe (`WardrobeScreen.Locked`) and the locked
  Leaderboard and Collection cards (`MetaCards.LockedCard`).
- [X] T071 Unity: `UiKit.LockBadge`, `BottomNavView.Show(active, look)` with a badge per locked place,
  `UiKit.LockedNotice` / `LockedNoticeView` (`UiKitNav.cs`); `StoreScreen.ShowLocked`, `WardrobeScreen.ShowLocked`,
  `LeaderboardScreen.ShowLocked`, `CollectionScreen.ShowLocked`; `HomeController.Navigate` opening the locked page or
  card with the level from `ProgressionService.Roadmap` (`NavLook`, `UnlockLevel`), without `store_open` for the locked
  Store page; the L10, L12 and L40 Home demos still point at their places.
- [X] T072 The preview: frame 2 (Level 5 with its won pictures) checks the five places and which are locked; the new
  extras 29 (the locked Store page at Level 5), 30 (the locked Wardrobe at Level 15, its Shop and Petals "+" opening
  the Store page) and 31 (the locked Collection and Leaderboard cards on a new profile) check what each notice says;
  frames 2 and 29–31 reviewed at 16:9, 19.5:9 and 21:9; the regenerated inventory.
- [X] T073 The docs: spec.md (the clarification, FR-030, scenario 6, SC-011), contracts/look.md §5, §6.5, §6.6 and
  §6.7 (the places always shown, the lock badge, the notice's recipe and regions, each screen when locked, the fixed
  numbers), spec 002's edge case, `CLAUDE.md`, `playtest/README.md` and `client/README.md`; all checks.
- [X] T074 After the review of the merged work: Unity's Petals pill (`UiKit.PetalsPill`) takes a tap only while its "+"
  shows (its `Button` interactable with the Store's unlock), as the playtest's; the playtest's Home look opens the
  Collection from Level 2 while it is empty (`HomeScreen.Look`; the dev row's skips collect no pictures);
  contracts/look.md §6.7; all checks and the type-checks.

## Owner review: every menu place a page (2026-10-04)

See spec.md (the clarification "the owner's bottom menu, wooden", its last question: "All the menu's places must be a
separate page. Not popups."; FR-029, FR-030, acceptance scenarios 4 and 6 and SC-011) and contracts/look.md §4.3,
§4.6, §5, §6.5 to §6.7 and the new §6.8 and §6.9. Presentation and navigation only (FR-002): the data, rules, unlocks
and analytics events stay.
- [X] T075 The kit's page layouts (`MenuPages.cs`, partial `ScreenLayout`), both built on `ScreenLayout.LockedPage` so
  the four pages share their header, panel and area: `ReferenceLeaderboard` → `ReferenceLeaderboardRegions` (the rows'
  box, the status line, Refresh, `Empty`; `Row(line, lines)` from `0.11W` to `0.13W`, `LinesFitting` at least
  `MinLines` 8, `LinesShown`, `FirstLine` keeping the player's row in view; `Parts(row)` → `LeaderboardRowParts`) and
  `ReferenceCollection` → `ReferenceCollectionRegions` (the count, the grid with `Cell`, `CellSize`, `RowsFitting`,
  `PerPage` and `Pages`, the Store page's footer and page arrows, the detail's `Picture`, `Name` and `Level`);
  `LockedNoticeRegions.CardContent` removed with the locked cards; the wording of `BottomNav`, `HomeLook` and
  `LockedPageRegions`.
- [X] T076 The tests (`ReferenceLayoutTests`): `TheLeaderboardPage_KeepsItsRegionsInOrder_AndEveryTargetReachable`
  and `TheCollectionPage_KeepsItsRegionsInOrder_AndEveryTargetReachable` on every phone of `Phones()`;
  `TheBottomMenu_SpreadsItsPlacesInOrder_AndRaisesTheActiveOne` and
  `TheBottomMenu_ShowsEveryPlaceAlways_AndKnowsWhichAreOpenAndFromWhichLevel` with the Leaderboard and the Collection
  as the active place too; the page header and the locked notice tests with the two pages.
- [X] T077 The playtest: `Screen.Leaderboard` and `Screen.Collection`, `DesignApp.ActivePlace`, `Navigate` (straight to
  each place's page), `OpenLeaderboard`, `CloseLeaderboard`, `OpenCollection`, `CollectionBack` (the detail, then
  Home), `CollectionPage`, the system `Back` on both pages and `StoreReturn` to the page that opened the Store;
  `LeaderboardScreen.cs` (the offline rows, the gap, "You", the offline line and Refresh, locked before L10) and
  `CollectionScreen.cs` (the count, the frames newest first a page at a time, the detail, locked before the first
  picture); `MetaCards.Leaderboard`, `Collection`, `Detail` and `LockedCard` and `Overlay.Leaderboard` and
  `Overlay.Collection` removed.
- [X] T078 Unity: `LeaderboardScreen` and `CollectionScreen` as full-screen pages (the Wardrobe's garden, `UiKit.PageHeader`
  with the Petals pill following the economy, the parchment panel, `BottomNavView` with their place raised, the
  locked notice; `LeaderboardScreen.ShowsRanks`; the Collection's detail on the page, its back returning to the
  grid); `HomeController.Navigate` showing the place's page (or Home) and hiding the others, `StoreFrom` for the menu's
  Shop and every page's Petals "+" (`store_open` from `home`, `wardrobe`, `leaderboard` or `collection`), and
  `leaderboard_view` and `collection_open` sent on open as before; the L10 Home demo still points at the menu's
  Leaderboard place.
- [X] T079 The preview: frames 5 (the Leaderboard page: back, system back, page to page, its Shop and the Store's back
  returning there, Refresh), 6 (the Collection page with 87 pictures: the page arrows, the detail, back and system back
  to the grid, a second back to Home), 20 (a picture's detail on the page) and 31 (the locked Collection and
  Leaderboard pages) show the pages; 27 walks the Wardrobe, the Leaderboard and the Collection pages; the slot rows
  (`bg.wardrobe`, `ui.card`, `ui.row`, `ui.back`, `ui.sign.ivy`, `ui.button.round`, `ui.button.secondary`,
  `ui.restart`, `ui.chevron`, `mat.parchment`, `collection.frame`, `collection.detail_frame`, `ui.locked.notice` and
  the menu's) and the regenerated inventory; frames 5, 6, 20 and 31 reviewed at 16:9, 19.5:9 and 21:9, and 17, 27 and
  29 for regressions.
- [X] T080 The docs: spec.md (the clarification, FR-029, FR-030, scenarios 4 and 6, SC-011, the key entities),
  contracts/look.md (§4.3, §4.6, §5, §6, §6.5 to §6.7, the new §6.8 and §6.9 with their fixed numbers), spec 002's FR-007
  and edge case, `CLAUDE.md`, `playtest/README.md` and `client/README.md`; all checks.

## Owner request: a Settings switch for Home's falling petals (2026-10-04)

See spec.md (the clarification "the owner's Settings switch for Home's falling petals") and contracts/look.md §4.3.
Presentation only (FR-002).
- [X] T081 The save: `SettingsData.HomePetals` (on by default, copied by `PlayerSave` like the other settings), the
  serializer's optional `settings.homePetals` (absent: on); spec 001's `player-save.schema.json` and data model; the
  tests `SavedData_RoundTrips` and `ASaveWithoutTheHomePetalsSwitch_ShowsThePetals`.
- [X] T082 Settings' fifth switch "Falling petals" (`settings.petals`) in both builds (`MenuCards.Settings`,
  `SettingsScreen`); Home without petals while it is off: the playtest's `HomeScreen` (the layered Home's petals, not
  the splash's, and the stand-in's falling petals), Unity's `HomeLayersView.PetalsOn` read every frame through
  `HomeStageView.PetalsOn` from `HomeScreen.Create` (`HomeController`); all checks and the type-checks; the preview's
  Settings (frame 19) reviewed at 16:9, 19.5:9 and 21:9.

## Owner choice: the pages fill tall phones (2026-10-04)

See spec.md (the clarification "the owner's bottom menu, wooden", its question on tall phones, and FR-030) and
contracts/look.md §6.8 and §6.9. Presentation only (FR-002).
- [X] T083 The Leaderboard's rows fill the panel ("B"): the playtest's `LeaderboardScreen.PlaceholderRanks(r)` as many
  placeholder ranks as fit (`LinesFitting` − 2); Unity's `LeaderboardClient.Neighbours` five (the player's five
  neighbours a side and the top eleven), enough for eleven lines.
- [X] T084 The Collection shows as many rows as fit ("A"): `ReferenceCollectionRegions.Side` (down to `MinSideShare`
  0.84 of the full frame when one more row then fits above the footer, the frames centered across the grid) and the
  footer right under a page's last row; `TheCollectionPage_KeepsItsRegionsInOrder_AndEveryTargetReachable` updated;
  contracts/look.md §6.8, §6.9 and spec.md; all checks; frames 5 and 6 reviewed at 16:9, 19.5:9 and 21:9.

## Owner request: Home's header row (2026-10-04)

See spec.md (the clarification "the owner's Home header", FR-017, FR-024, FR-028, scenario 2, SC-009) and
contracts/look.md §3.4, §4.5 and §6.4. Presentation only (FR-002).
- [X] T085 The kit's header row: `ReferenceHomeRegions.Avatar` (`0.13W`, Settings' mirror at `0.04W` from the safe right
  edge), the Petals box `0.44W × 0.105W` (`PetalsWidthShare`, `PetalsHeightShare`) centered on the safe area and on
  Settings' middle line, `Header` (the row, first of `Ordered`; the logo picture's top from its bottom), the Avatar in
  `Buttons`; `PetalsPillParts.Span` and `GardenLook.PillDecorationBoxes` (the pill's flowered corners, 1.2 of its
  height); `CharacterArt.ProfileHero` (Bloom, the avatar's hero in both builds; Unity's `ProfileAvatar.HeroFamily`);
  `Home_FollowsTheReference_AndKeepsEveryButtonReachable` updated (one row, the pill centered and clear of both, its
  flowers between them, the logo's letters under the row, every target reachable and clear of the others).
- [X] T086 The playtest's Home: the centered pill with its flowered corners (`Kit.PetalsPill(align: 0.5f, decorate:
  true)`, `Kit.Decoration` on the kit's boxes; the reward's sparkles on its lotus), the avatar restored from before the
  bottom menu without its shirt badge (`HomeScreen.Avatar`: the hero in its outfit once the Wardrobe is open, the
  profile frame and badge), its tap the click and the toast "Profile coming soon" (since T115: the profile page) (`DesignApp.OpenProfile`,
  `home.profile_soon`), the heroes' taps cut clear of its touch box (`UiBoxes`); the preview's frame 2 taps it and
  frame 17 taps the centered pill.
- [X] T087 Unity's Home: `UiKit.PetalsPill(align, decorate)` with the decoration pictures (`DecorationImages`, shared
  with `UiKit.Decoration`), the avatar's button (`PressMotion`, the click, `HomeFeatureActions.OnProfile`, none yet:
  Unity's Home has no toast) holding a `ProfileAvatar` built after the stage, `HomeModel.Profile` and `AvatarOutfit`
  from `HomeController` (the marker left out); the slot rows `ui.button.round`, `ui.pill.petals`, `ui.deco.garden`,
  `cosmetic.frame` and `cosmetic.badge` and the regenerated inventory.
- [X] T088 The docs: spec.md, contracts/look.md (§6.4's row, sizes and fixed numbers on 1080 × 2340), `playtest/README.md`
  and `client/README.md`; all checks; frames 2, 3, 28 and 31 reviewed at 16:9, 19.5:9 and 21:9.

## Owner note: the backgrounds at 70% of the heroes' saturation (2026-10-04)

See spec.md (the clarification "the owner's saturation note: the backgrounds at 70%", FR-031) and contracts/look.md
§1.4. Presentation only (FR-002).
- [X] T089 The full ladder (backgrounds 60%, UI 70%) built, shown and reverted at the owner's word; the
  backgrounds alone tried at 80% and 70% of the heroes' mean saturation and shown side by side with the current look.
- [X] T090 The owner's choice, 70%: `tools/heroanim/saturation.mjs` (the backgrounds only, idempotent,
  `saturation.json`) and `layers.mjs` (the layered Home as one scene, `layers.json` `saturation`); the backgrounds
  re-encoded once from the owner's PNG files; the source records (`tools/artgen/models/owner-pictures.md`,
  `tools/heroanim/SOURCE.md`, `README.md`, pictures.md); all checks.

## Owner note: the pages' titles in the middle (2026-10-04)

See spec.md (the clarification "the owner's note: the pages' titles in the middle", FR-025) and contracts/look.md
§6.5. Presentation only (FR-002).
- [X] T091 `ScreenLayout.PageHeader` centers the banner on the safe area's middle, as wide as the Petals pill's side
  allows (its leaves `0.005W` clear); the page header's Petals box `0.24W × 0.068W`; the banner's ivy at
  `PageHeader.IvyScale` (0.8) through `GardenLook.IvyBox` / `SignExtent` / `IvyReachAt` and the signs' `ivyScale`
  (`Kit.WoodSign`, `UiKit.WoodSign`, `UiKit.SignLetterRoom`) in both builds; the header test (banner in the middle,
  leaves clear, the title's room); previews of the Wardrobe, Store, Leaderboard, Collection and locked pages; all checks.

## Owner pack: Home's promo scenes, Daily and No Ads (2026-10-04)

See spec.md (the clarification "the owner's Home promo scenes", FR-032, FR-033), contracts/look.md §6.4.1 and
pictures.md D14–D22. Spec 001 FR-054 and FR-058 carry the owner's amendment (Remove Ads offered on Home from L1).
- [X] T092 The pack analysed (layers, guide, sizes, composition) and previewed as GIFs over Home before any code; three
  rounds of the owner's notes (the push and the lotus, no badge, sparkles last, a slower stamp, equal sizes, the sign in
  front of Sprig) and answers (the Daily Reward; No Ads from L1 with its own card; the album's colour kept).
- [X] T093 Kit: `HomePromo` (scene boxes, the schedule, every layer's pose per frame, the plaques), the scene boxes in
  `ReferenceHomeRegions` (the Daily Challenge under the Daily scene), the nine pictures fitted into the Decor folder with
  their record and notices, the slots `ui.promo.no_ads` and `ui.promo.daily`, the strings, the importer's mipmaps, tests.
- [X] T094 [P] Unity: `HomePromoView` on Home (No Ads until Remove Ads is owned, the Daily from its unlock), the
  taps, the Remove Ads card buying the existing product and restoring.
- [X] T095 [P] Playtest: the scenes on Home, the taps, the Remove Ads overlay (purchases off: "Unavailable"), the
  preview frame.
- [X] T096 Docs (spec 005, spec 001 amendment, look.md, pictures.md, CLAUDE.md), the asset inventory, previews, all
  checks, the APK.

## Owner delivery: the four heroes in one file, Heroes.glb (2026-10-04)

See spec.md (the clarification "the owner's Heroes.glb"), research D26 and `tools/heroanim/SOURCE.md`.
- [X] T097 The single exports checked (`twig2.glb` without clips, `sprig3.glb` with a blank texture, `drop2.glb` with a
  still breathing, `bloom3.glb`, an empty `bloom4.glb`) and `Heroes.glb` (one rig, material and texture per hero, every
  clip on its own rig).
- [X] T098 The bake reads one hero out of a file of several (`mesh`), adds a breath (`breathe`, Drop) and stand-in clips
  (`synth`, Twig); all four baked from `models/heroes.glb`, graded (the heroes' mean saturation kept), the old models
  removed; Home's Bloom and Drop placements; the hero tests (1.5 s reactions, 732 frames); SOURCE.md, README, CLAUDE.md.
- [ ] T099 Twig's own clips (an idle, a reaction, the win) and Drop's breathing and celebration from the owner; then
  re-bake without the stand-ins.


## Owner request: a visible Garden Entry and guided spotlights (2026-10-05)

See spec.md (the clarification "the owner's visible Garden Entry and guided spotlights"), FR-034, FR-035, research D27
and contracts/look.md §3.6, §6.10.
- [X] T100 The cause found on Level 5's data (the only entry under the logs, the wood pods deepest), three entry looks
  and four ways to draw attention rendered over the level before any code; the owner chose "B + 1a + 1b".
- [X] T101 Core: `LevelView.ReachableTargets()` (hints only; the rules keep their own reachability), with a test.
- [X] T102 Kit: `UiRaster.EntryArch` and `BoardLayout.Arch` / `ArchOf` / `ArchBounds` (every entry, turned to its
  side); `GuideTour` (the steps of Level 1, the blocked entry, the boosters, Return when a pod waits);
  `Spotlight` and `UiRaster.SpotlightScrim` (holes, bubble, hand, ring); slots `board.entry.arch` and `ui.spotlight`;
  strings; `GuideTourTests`.
- [X] T103 [P] Playtest: the arches on the board; `GuidePainter` and the guide in `LevelScreen` (forced steps, the free
  demo use kept out of the clean-clear count, the booster cards and the first-tap hint replaced); `IPainter.TapPoint`;
  preview frames 33–38 and frame 21.
- [X] T104 [P] Unity: the arches in `BoardView`; `GuideOverlay` (scrim picture, raycast filter letting taps through
  only inside the holes, bubble, ring, hand); the guide in `GameplayController` (the same steps, the free use, `source`
  `demo`, Return's start once the board has settled); `BoosterDemos` and the first-tap card removed.
- [X] T105 Docs (spec 005, spec 001 FR-042 and FR-048 amendments, analytics contract, look.md, research D27, CLAUDE.md),
  the asset inventory, all checks, the APK.
- [ ] T106 Unity Editor: play Levels 1–9 on a device and check the spotlight's taps (the raycast filter) and looks match
  the playtest's.

## Owner note: the celebrating hero looks aside (2026-10-05)

- [X] T107 The cause found (the bake's per-hero `yaw` for Home's fountain, shown on the win too); Twig and Sprig baked a
  second time facing the player for the win and the milestone (`winYaw`, `winidle`, one palette per set);
  `MotionClip.WinIdle`, `HeroMotion.HasFront`, `HeroMotionPlayer` `front`, `HeroPose.FromClip`; both builds' win and
  milestone; the hero tests (900 frames); SOURCE.md, README, research D28, CLAUDE.md; the APK.

## The owner's Home tuning (2026-10-05, FR-036)

- [X] T108 Home variations rendered (24, contact sheets), then a constructor page of the game's own layers with
  sliders and presets; the owner's settings read back from it (research D29).
- [X] T109 Kit: `HomeLayers.Stage` (0.9) and `HeroScale` (1.05), Twig at 0.85; Home's regions (no logo, the promo
  scenes from 17.5% at ×1.05, the sun 0.086 W under the Daily Reward's, Play 0.68 W × 0.12 H, the plaque 0.4 W × 0.068 H,
  the teaser row down to the menu's top); `HomePromo` plates and shadows, `UiRaster.SilhouetteShadow` and `RoundShadow`;
  the layout and promo tests.
- [X] T110 Playtest: Home without its logo, the stage, the plates and the soft shadows (`IPainter.SpriteAlpha` in both
  painters); Unity: `HomeScreen` without its logo, `HomeLayersView` on the stage, `HomePromoView` plates and shadows
  (`OwnerArt.DecorAlpha`).
- [X] T111 The garden blurred in `home.jpg` (`layers.mjs`), the heroes' Home frames finished (`post.mjs`, `heroes.json`
  `home`) and re-baked; the falling petals off by default (`settings.homePetalsOn`, both schema copies).
- [ ] T112 Unity Editor: check Home's plates, shadows and the blurred garden on a device against the playtest.

## The owner's profile page and avatars (2026-10-05, FR-037)

- [X] T113 Data: `AvatarCatalog` (14 avatars, tiers), `ProfileService` (owned, chosen, bought with Petals, the name,
  the short ID, the joining day), `ProfileEditor` (the card's tabs, picks, Save or Buy); the save's `profile` section
  and `cosmetics.equipped.profile.avatar` (both schema copies, the merge); Remote Config `economy.price.avatar*`;
  `ProfileServiceTests`.
- [X] T114 Pictures: the owner's 14 avatars at 384 × 384 JPEG in `Art/Avatars/Resources/Avatars/`, the notices and the
  source record, pictures.md I; `OwnerPictures.AvatarFolder`, the slots `ui.avatar`, `ui.achievement`, `ui.edit` (the
  pencil shape); `ScreenLayout.ReferenceProfile` and `ProfileEdit`, `ProfileLayoutTests`; the strings.
- [X] T115 Playtest: `ProfileScreen` (the page and the card), `Kit.Avatar` and `Kit.AvatarPicture` (`IPainter.PushClipRound`
  in both painters), the avatars embedded, Home's avatar and the own leaderboard row, the Android text dialog
  (`TextPrompt`); preview frames 39–41, frame 2 opening the page.
- [X] T116 Unity: `ProfileScreen`, `ProfileEditCard` (`TMP_InputField` for the name), `ProfileAvatar` with the round
  picture (`OwnerArt.Avatar`, a mask), Home's `OnProfile`, the Wardrobe's and the leaderboard's own avatar; the avatars
  imported with mipmaps (`OwnerIconImporter`).
- [ ] T117 Unity Editor: open the profile page and the card on a device; type a name with the system keyboard; buy an
  avatar; check the round masks and the Wardrobe's profile tab.
- [X] T118 The owner: confirm the avatars' tiers (which picture costs 300, 600 or 1200) and how the avatars were made
  (the source record's tool line); name the achievements. *(2026-10-06: "Choose yourself … ChatGPT made them": the
  tiers kept, ChatGPT recorded, the three achievements named and shown, `Achievements`, `AchievementsTests`.)*

## The owner's clearing styles (2026-10-06, FR-038)

- [X] T119 Kit: `ClearStyles` (the seven styles, the free pair by level, the bought ones' ids, one trip time and each
  style's legs, the line gap), `ClearTrip` (a walker's leg at a moment), `ClearLook` (each style's walkers, restores
  and flights as a list of drawn items in cell units, tokens and slots only), `FxList` (the items with their composed
  turn and squash), `ClearPreview` (the Store card's small board, scheduled as the board is); `ClearStylesTests`.
- [X] T120 Timelines, both builds (`LevelAnimator`, `TimelinePlayer`): the style's trip time, the line from each arch,
  rounds not waiting for each other, a cell crossable once its tile is gone, the waves' clamp 1.2–40 s, the backlog
  speed-up at 60 s; the walkers carry their pod; `EventTimelineTests` and `playtest/check`.
- [X] T121 Meta: `ClearingService` (owned, chosen, bought for Petals from L40, the style of a level), the save's
  `clear.<name>` and `cosmetics.equipped.board.clearing` (both schema copies, the merge), Remote Config
  `economy.price.clearing`, the strings, `cosmetic_equip` with `board`; `ClearingServiceTests`.
- [X] T122 Playtest: the board draws `ClearLook` (walkers over the tiles, flights over the tray and slots, tiles hidden
  while a style holds them, Blossom's swaying neighbours), the Store's Animations tab with the cards and their live
  previews; preview frames 42 and 43.
- [X] T123 Unity: `ClearFxView` (pooled figures and images drawing `ClearLook`'s list on the board and above the
  slots), `TileView` hiding held tiles, the Store's Animations tab with `ClearPreviewView` cards; the controller picks
  the level's style.
- [ ] T124 Unity Editor: play a level in each style on a device; check the previews' masks and the padlock before L40.

## The owner's loading screen: the lotus (2026-10-06, FR-039)

- [X] T125 GIFs: four loading-screen variants rendered from the game's pictures (`tools/loading-gifs`); the owner chose
  2 · Lotus.
- [X] T126 Kit: `LotusIris` (the layout, the splash's and the transition's poses, the splash's progress, the ring's
  petals, the hole's box and the cover's panels), `UiRaster.IrisHole` and `IrisGlow`; the slot `ui.lotus_iris`, the
  splash's slots `brand.splash_art` and `bg.splash` as the lotus loader; the string `splash.loading`; `LotusIrisTests`.
- [X] T127 Playtest: `LotusPainter`; the splash is the lotus loader and opens on the first screen through the iris;
  the win's Next plays the iris (`DesignApp.NextLevel`); preview frames 1 and 44–47.
- [X] T128 Unity: `LotusIrisView`, `SplashScreen` (its ring follows `Boot`'s loading), `LevelTransition` on the Boot
  object (the interstitial over the closed cover, `GameFlow.PostWinTransition`).
- [ ] T129 Unity Editor: launch on a device (the splash's ring and the iris opening on Level 1 and on Home); win a level
  with and without an interstitial due; check the rim, the cover's seams and that no tap goes through.

## The owner removes Home's falling petals (2026-10-06)

- [X] T130 Both builds: Home's falling petals gone (the layered Home's petals layer, `HomeLayers.PetalsAt` and its
  constants, the drawn stand-in's falling petals on Home), Settings' "Falling petals" switch and `settings.petals`; the
  save no longer writes `homePetalsOn` (older saves' `homePetals` and `homePetalsOn` are read and ignored; both schema
  copies, spec 001's data model); `TheRemovedPetalsSwitch_IsNoLongerWritten_ButOlderSavesStillLoad`.
- [X] T131 Pictures: `home-petals.png`, the slot `bg.home.petals` and its notice removed; `tools/heroanim/layers.mjs` no
  longer reads `05_home_petals_overlay.png` (`layers.json`, `HomeLayersData.cs`; `check.mjs` passes).
