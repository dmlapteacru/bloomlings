---
description: "Task list for spec 002: the UX design board without art assets, and the asset inventory"
---

# Tasks: UX Design Board — the game's visual design without art assets

**Input**: Design documents from `specs/002-ux-design-board/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Included. The plan and quickstart name tests:
- design-kit tests in `client/DotnetCheck`;
- render checks in `playtest/preview`;
- the existing suites, which must stay green.

**Organization**: One phase per user story:
- US1 (P1): gameplay;
- US2 (P2): flow screens;
- US3 (P3): meta screens;
- US4 (P2): the asset inventory.

US4 runs last although it is P2, because it inventories the placeholders of US1–US3.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1–US4, for the user story phases only

## Path conventions

- The Unity client lives under `client/Assets/Bloomlings/`. Its engine-free design kit is in `UI/Design/`.
- The full playtest's engine-free screens are in `playtest/design/`. Its Android host is in `playtest/android/Design/`.
- The preview tool is in `playtest/preview/`.

---

## Phase 1: Setup

**Purpose**: The folders, projects and links the design kit and the designed playtest need.

- [ ] T001 Create `playtest/preview/Bloomlings.Playtest.Preview.csproj` (net10.0 console).
  - References: SkiaSharp 3.119.1, SkiaSharp.NativeAssets.Linux.NoDependencies 3.119.1, and the core and content
    projects.
  - Compile items: `playtest/design/*.cs`, `client/Assets/Bloomlings/UI/Design/*.cs`, and the engine-free client
    files the playtest already links.
  - Add `playtest/preview/bin`, `obj` and `out` to `.gitignore`.
- [ ] T002 Link `client/Assets/Bloomlings/UI/Design/*.cs` into `playtest/Playtest.Shared.props`.
  - Add `playtest/design/*.cs` to the full playtest only, in `playtest/android/Bloomlings.Playtest.Android.csproj`.
  - Link the meta services the designed screens call: `CollectionService`, `DailyRewardService` and `WardrobeService`.
  - Keep the tester's compile list (`playtest/tester/*.csproj`, `../android/*.cs`) free of `design/`.
- [ ] T003 Rename `playtest/android/GameView.cs` to `playtest/android/TesterView.cs`.
  - Remove its non-tester branches: Home, progression, demos and meta.
  - Keep the tester's look and quick loop: ◀ ▶, free boosters, instant results.
  - In `playtest/android/MainActivity.cs`, pick `TesterView` under `PLAYTEST_TESTER` and `DesignView` otherwise.

---

## Phase 2: Foundational (blocking prerequisites)

**Purpose**: The shared design kit and the two drawing hosts. Every story needs them.

**⚠️ CRITICAL**: No user story work starts before this phase is done.

- [ ] T004 [P] Create `client/Assets/Bloomlings/UI/Design/Rgba.cs`, an engine-free color value with:
  - `FromHex` and `Hex`;
  - `Mix`, `Lighten`, `Darken` and `WithAlpha`;
  - relative luminance and contrast ratio, with the same formula as `InkContrast`.
- [ ] T005 Create `client/Assets/Bloomlings/UI/Design/DesignTokens.cs`.
  - Its content is every token of `contracts/design-tokens.md`: colors, radii, type (size, bold, upper, outline, min),
    spacing and sizes, elevation, motion.
  - Theme tinting: `Backdrop(themeBackgroundHex, themeAccentHex)` mixes the backdrop tokens 35% toward the theme.
  - Variant looks: `TileTop`, `TileEdge` and `PodCard` from a variant's color.
- [ ] T006 Create `client/Assets/Bloomlings/UI/Design/ShapeLibrary.cs`.
  - Move every SDF of `Art/Procedural/ProceduralSprites.cs` into it, using `System.MathF`: primitives, variant icons,
    silhouettes, accessories, specials, boosters, UI glyphs.
  - Key each shape by its asset slot id (`symbol.leaf`, `booster.shuffle`, `special.gate`, `ui.lock`, …).
  - Add the board's new UI shapes:
    - `ui.pause`, `ui.home`, `ui.restart`, `ui.play`, `ui.close`, `ui.chevron`, `ui.plus`;
    - `ui.gift`, `ui.trophy`, `ui.medal`, `ui.ad`, `ui.shirt`, `ui.grid`, `ui.sun`, `ui.check`;
    - `currency.petal` (five petals, and a center mask), `char.face` (eyes and smile);
    - `tile.stone`, `brand.wordmark_leaf`.
- [ ] T007 Create `client/Assets/Bloomlings/UI/Design/ShapeRaster.cs`.
  - `Mask(id, size)` returns a `byte[]` alpha mask with one-pixel anti-aliasing, using the same sampling as today's
    `ProceduralSprites.Get`.
- [ ] T008 Make `client/Assets/Bloomlings/Art/Procedural/ProceduralSprites.cs` a thin Unity wrapper.
  - Its sprites come from `ShapeRaster.Mask`, with the same cache and the same public members, so callers are
    unchanged.
  - Add `ProceduralSprites.Shape(id)` for the new shapes.
- [ ] T009 [P] Create `client/Assets/Bloomlings/UI/Design/NumberText.cs`. `Group(int)` inserts a no-break space every
  three digits: "1 240", "12 345".
- [ ] T010 Create `client/Assets/Bloomlings/UI/Design/ScreenLayout.cs` (data-model "ScreenLayout regions").
  - `Gameplay(width, height, insets, hasBadge)` returns TopBar, Badge, Board, Slots, Tray and Boosters.
  - `Home(width, height, insets, look)` returns TopBar, Hero, Level, Teaser, Play, Rank, Daily and Features.
  - `Card(width, height, insets, contentHeight)` and `Sheet(width, height, insets, contentHeight)`.
  - Rules 1–6 of the data model hold.
- [ ] T011 Create `client/Assets/Bloomlings/UI/Design/HomeLook.cs`, the data-model table of which Home elements show.
  - `HomeLook.From(unlocked ids, collection count, next milestone, daily available)`.
- [ ] T012 Create `client/Assets/Bloomlings/UI/Design/AssetSlots.cs`: the `AssetSlot` record, the `AssetCategory`,
  `SizeClass` and `Priority` enums, and the registry.
  - Seed it with every shape id of `ShapeLibrary`, the backdrop layers and the fonts.
  - Also seed every `SoundCue` (`audio.cue.*`) and the per-theme music.
  - US4 completes the rest.
- [ ] T013 [P] Add the design-kit tests in `client/Assets/Bloomlings/Tests/EditMode/`:
  - `DesignTokensTests.cs`:
    - names are unique;
    - `text.primary` on `surface.panel` is at least 4.5:1;
    - `text.on_color` on `button.primary`, `pill.level`, `badge.hard`, `badge.super_hard` and `badge.count` is at least
      3:1.
  - `ShapeLibraryTests.cs`:
    - every id rasterizes non-empty;
    - the 12 variant symbols are pairwise distinct (mask difference over 8%).
  - `ScreenLayoutTests.cs`: order, no overlap, inside the safe area and a board of at least 45% at 16:9, 18:9, 19.5:9,
    20:9 and 21:9.
  - `NumberTextTests.cs`.
  - `AssetSlotTests.cs`:
    - ids are unique;
    - prefixes match categories;
    - readability is set on symbol, pod, slot and tile slots;
    - every `ShapeLibrary` id is registered.
- [ ] T014 Rewrite `client/Assets/Bloomlings/UI/UiTheme.cs` over `DesignTokens` (Unity `Color` conversions). Keep the
  old member names used by the current screens as aliases until their stories restyle them.
- [ ] T015 Extend `client/Assets/Bloomlings/UI/UiFactory.cs` with the board components (FR-005, FR-007):
  - `PrimaryButton` (green, lighter top, darker lower edge, press motion);
  - `SecondaryButton` (cream);
  - `RoundIconButton` (white circle, glyph);
  - `DarkPill` (2×);
  - `LevelPill`;
  - `PetalsPill` (Petal symbol, grouped number, green "+");
  - `Badge` (HARD, SUPER HARD);
  - `CountBadge`;
  - `Card` (title, round close, scrim);
  - `BottomSheet` (grip, slides up);
  - `AutoFit` text (min size from the type token).

  Add the needed members to `client/DotnetCheck/UnityStubs.cs`.
- [ ] T016 [P] Create `client/Assets/Bloomlings/Gameplay/Themes/BackdropView.cs`, the procedural garden backdrop in
  Unity (research R8):
  - a sky gradient, hills, side bushes, blossoms and distant arches;
  - tinted by the level band's theme and cached as a texture per size.
- [ ] T017 Create `playtest/design/IPainter.cs`, as defined in `contracts/painter.md`, and `playtest/design/Kit.cs`,
  the same components as T015 drawn through `IPainter`: primary, secondary and round buttons, pills, badges, card,
  sheet, auto-fit text.
- [ ] T018 [P] Create `playtest/design/Backdrop.cs`, the garden backdrop drawn through `IPainter`. It uses the same
  layers and tokens as T016.
- [ ] T019 Create `playtest/android/Design/AndroidPainter.cs`, `IPainter` over `Android.Graphics.Canvas`.
  - Masks come from `ShapeRaster` as ALPHA_8 bitmaps, cached by (id, size), and are tinted with the paint color.
  - Text uses the bold system typeface, with outline strokes.
- [ ] T020 Create `playtest/android/Design/DesignView.cs`, the Android `View` that hosts `DesignApp`.
  - It handles insets, touch dispatch to hit targets, the frame loop while animating, and sound and haptics
    (`PlaytestSound`).
- [ ] T021 Create `playtest/preview/SkiaPainter.cs`, `IPainter` over SkiaSharp.
  - It records every shape id, text and hit target (contracts/painter.md, "Recording").
  - Create `playtest/preview/Program.cs`, which renders frames to `--out`, runs the checks and returns exit code 1 on a
    failure.
  - The checks:
    - every shape id is registered;
    - no hit targets overlap;
    - nothing leaves the safe area;
    - every target meets the touch minimum.
- [ ] T022 Create `playtest/design/DesignApp.cs`: the engine-free screen stack of the full playtest.
  - Screens: Splash, Home, Level and the overlay cards.
  - Navigation: Home ⇄ Level, overlays over Level or Home.
  - It calls `PlaytestMeta`.
  - It keeps the playtest's tester controls on Home as a small labelled dev row: −1, +1, Reset.

**Checkpoint**: The kit and its tests pass (`dotnet test client/DotnetCheck/...`). Both hosts draw an empty
backdrop. The preview tool runs.

---

## Phase 3: User Story 1 — Play a level in the board's visual style (Priority: P1) 🎯 MVP

**Goal**: The gameplay screen of frames 7–9, with the pod, slot and booster states of frames 12–14, in both clients.

**Independent Test**: Open a Normal, a Hard and a Super Hard level, then compare them with frames 7, 8 and 9. Put pods
and slots into every frame 12 and 13 state, and pass the L3, L4, L6 and L9 booster unlocks (frame 14). The preview
images 07–09 and 12–14 show the same.

### Unity client

- [ ] T023 [US1] Lay out `client/Assets/Bloomlings/UI/Screens/GameplayHud.cs` with `ScreenLayout.Gameplay`, in this
  order (FR-009): top bar, then the board, then slots, then tray, then the booster bar at the bottom.
  - The top bar has a round Pause, the `LevelPill` and the 2× `DarkPill`.
  - `BackdropView` sits behind it all.
  - The toast is kept.
- [ ] T024 [US1] Turn `client/Assets/Bloomlings/UI/Screens/DifficultyBanner.cs` into the badge under the level pill
  (FR-010):
  - "HARD" in `badge.hard`;
  - "SUPER HARD" in `badge.super_hard`, with the `pill.level_super_hard` pill;
  - nothing on Normal levels.

  Keep its short intro pulse.
- [ ] T025 [P] [US1] Restyle `client/Assets/Bloomlings/Gameplay/Board/TileView.cs` and `BoardView.cs` as raised
  rounded tiles.
  - The top is lighter and the lower edge darker.
  - The variant symbol is drawn in ink, with no faces (FR-011).
  - Keep open ground, layer peek and the mystery "?".
- [ ] T026 [P] [US1] Restyle stones, keys, locks and specials in `client/Assets/Bloomlings/Gameplay/Board/SpecialView.cs`
  and `KeyView.cs` as garden objects (FR-015). They must never cover the symbol or count.
- [ ] T027 [P] [US1] Restyle `client/Assets/Bloomlings/Gameplay/Tray/PodView.cs` and `TrayView.cs` to the frame 12
  states (FR-012):
  - the pod card with a variant tint;
  - the family body in the variant color, with a face and the big symbol in ink;
  - the count pill;
  - next in stack: grey and smaller;
  - pressed: sunk;
  - locked: a padlock over a grey card;
  - mystery: "?" with its count;
  - connected: a `state.link` bar.
- [ ] T028 [P] [US1] Restyle `client/Assets/Bloomlings/Gameplay/Slots/SlotRowView.cs` to the frame 13 states
  (FR-013):
  - empty: a `surface.sunk` tile;
  - working: bright;
  - stuck: greyed;
  - locked: a padlock;
  - danger: a `state.danger` dashed frame on the last free usable slot;
  - the extra slot marked with "+".
- [ ] T029 [P] [US1] Restyle `client/Assets/Bloomlings/UI/Gameplay/BoosterBar.cs` to frame 14 (FR-014).
  - The bar is hidden before the first unlock.
  - Round buttons in the booster colors, appearing at their unlock levels.
  - A `CountBadge` with the charges, or the Petal price when none is left.
  - Keep the disabled and targeting states.

### Full playtest

- [ ] T030 [P] [US1] Create `playtest/design/TilePainter.cs`: tiles, open ground, layers, mystery, stones, keys,
  locks, specials and the entry, as in T025–T026.
- [ ] T031 [P] [US1] Create `playtest/design/PodPainter.cs`: the frame 12 states, as in T027.
- [ ] T032 [P] [US1] Create `playtest/design/SlotPainter.cs`: the frame 13 states, as in T028, including the settling
  looks of `LevelAnimator.SlotLook`.
- [ ] T033 [P] [US1] Create `playtest/design/BoosterBarPainter.cs`: frame 14, as in T029.
- [ ] T034 [US1] Create `playtest/design/LevelScreen.cs`: the gameplay screen through `ScreenLayout.Gameplay`.
  - It covers the top bar, badge, board, walkers, fades, flights, slots, tray and boosters.
  - It keeps the playtest's taps, boosters, targeting, demos and toasts.
  - It is driven by `LevelAnimator`, as `GameView` is today.
- [ ] T035 [US1] Add frames 7, 8, 9, 12, 13 and 14 to `playtest/preview/Frames.cs`, taken from real levels:
  - a Normal early level;
  - a Hard level;
  - a Super Hard level with stones and specials;
  - pod and slot state sheets;
  - the booster bar at L1–2, L3, L4, L6 and L9+.

**Checkpoint**: US1 works on its own:
- the previews 07–09 and 12–14 match the board;
- `playtest/check` passes;
- the client tests pass.

---

## Phase 4: User Story 2 — Flow screens (Priority: P2)

**Goal**: The splash, Home (early and progressed), Pause, the jam sheet, the win card and the milestone card, in both
clients.

**Independent Test**:
1. Fresh launch: the splash, then Level 1.
2. After some wins: Home in the frame 2 look. Fast-forward past L10, L40 and L50: the frame 3 look.
3. Pause, jam and win, then reach a milestone level. Compare with frames 10, 11, 15 and 16.

### Unity client

- [ ] T036 [P] [US2] Create `client/Assets/Bloomlings/UI/Screens/SplashScreen.cs`: the wordmark over the backdrop,
  with no tap (FR-016). Show it from `client/Assets/Bloomlings/App/Boot.cs` while services and content load.
- [ ] T037 [US2] Restyle `client/Assets/Bloomlings/UI/Screens/HomeScreen.cs` with `ScreenLayout.Home` and `HomeLook`
  (FR-017).
  - Always shown:
    - the `PetalsPill` ("+" opens the Store once unlocked);
    - the round Settings button;
    - "LEVEL N";
    - the big PLAY.
  - Shown once unlocked:
    - the hero (a Bloomling figure);
    - Wardrobe and Collection round buttons;
    - "N levels to reward" with a gift;
    - "Rank #N >";
    - the Daily Challenge card with its reward.
  - Keep the free-booster offer and the demo targets. Update `client/Assets/Bloomlings/App/Home/HomeController.cs` for
    the new model.
- [ ] T038 [P] [US2] Restyle `client/Assets/Bloomlings/UI/Screens/PauseScreen.cs` as the frame 11 card (FR-018):
  "PAUSED", RESUME (primary), RESTART, SETTINGS and HOME (secondary), and a close button.
- [ ] T039 [P] [US2] Turn `client/Assets/Bloomlings/UI/Screens/JamScreen.cs` into the frame 10 bottom sheet (FR-019).
  - "NO MOVES LEFT", "Use a booster to continue".
  - One option tile per eligible recovery booster, with its icon, name and cost.
  - Free rescue (primary, with the ad icon) when offered.
  - Restart (secondary).
  - The board stays visible above the sheet.
- [ ] T040 [P] [US2] Restyle `client/Assets/Bloomlings/UI/Screens/WinScreen.cs` to frame 15 (FR-020).
  - The sequence is: the picture reveal, then "+N" with the Petal symbol rising, then NEXT (primary) and the optional
    "×2 reward" (secondary, ad icon).
- [ ] T041 [P] [US2] Create `client/Assets/Bloomlings/UI/Screens/MilestoneCard.cs`, the frame 16 card shown after a
  milestone win (FR-021).
  - It shows "LEVEL N", "Milestone reached!", one icon with an amount per reward (cosmetic, Petals, boosters), and
    CONTINUE.
  - Wire it in `client/Assets/Bloomlings/Gameplay/GameplayController.cs` in place of the win screen's milestone
    ribbon.

### Full playtest

- [ ] T042 [P] [US2] Create `playtest/design/SplashScreen.cs`. It shows for a short time at launch, then continues to
  Level 1 on the first launch and to Home later.
- [ ] T043 [US2] Create `playtest/design/HomeScreen.cs`: frames 2 and 3 through `ScreenLayout.Home` and `HomeLook`,
  plus the dev row of T022.
- [ ] T044 [P] [US2] Create `playtest/design/PauseCard.cs` (frame 11) and `playtest/design/SettingsCard.cs` (sound and
  haptics toggles, in the card style).
- [ ] T045 [P] [US2] Create `playtest/design/JamSheet.cs` (frame 10). It shows the eligible recovery boosters with
  their costs, the Free rescue (the playtest grants it without an ad, labelled), and Restart.
- [ ] T046 [P] [US2] Create `playtest/design/WinCard.cs` (frame 15), with the picture, "+N" and NEXT. "×2 reward" is
  shown disabled with "no ads in playtest".
- [ ] T047 [P] [US2] Create `playtest/design/MilestoneCard.cs` (frame 16), from `WinPayout.Milestone`.
- [ ] T048 [US2] Add frames 1, 2, 3, 10, 11, 15 and 16 to `playtest/preview/Frames.cs`. Home frame 3 uses a
  fast-forwarded profile (L88).
  - Add a flow check to `playtest/preview/Program.cs`. It drives `DesignApp` through fresh launch, then Level 1, then
    win, then Home, then pause, then jam, then milestone, with no exception and each expected screen reached.

**Checkpoint**: US1 and US2 work together; the previews of frames 1–3, 10, 11, 15 and 16 match the board.

---

## Phase 5: User Story 3 — Meta screens (Priority: P3)

**Goal**: Daily Reward, Leaderboard, Collection and Store in the board's card style, in both clients.

**Independent Test**: Unlock each feature (fast-forward), open it, and compare it with frames 4, 5, 6 and 17.

### Unity client

- [ ] T049 [P] [US3] Restyle `client/Assets/Bloomlings/Meta/DailyReward/DailyRewardPopup.cs` to frame 4 (FR-022):
  "Daily Rewards", "Day N", the reward basket (the Petal symbol cluster), "+N" with the Petal symbol, CLAIM, and
  "Get +N" with the ad icon.
- [ ] T050 [P] [US3] Restyle `client/Assets/Bloomlings/UI/Screens/LeaderboardScreen.cs` to frame 5 (FR-023).
  - Medals for ranks 1–3, avatar circles, names and scores.
  - A "…" gap row, the player's row highlighted as "You", and the offline notice.
- [ ] T051 [P] [US3] Restyle `client/Assets/Bloomlings/UI/Screens/CollectionScreen.cs` to frame 6 (FR-024):
  - a grid of framed pictures;
  - a detail view with the name and "Completed at Level N".

  It never selects a level.
- [ ] T052 [P] [US3] Restyle `client/Assets/Bloomlings/UI/Screens/StoreScreen.cs` to frame 17 (FR-025).
  - The `PetalsPill`, and rows of icon, name and price with the Petal symbol (or the real price, or "unavailable").
  - Shop and Cosmetics tabs in the same style.
- [ ] T053 [P] [US3] Restyle the screens that are not on the board to the same card style, for consistency (FR-005,
  FR-007): `client/Assets/Bloomlings/UI/Screens/SettingsScreen.cs`, `WardrobeScreen.cs` and
  `DailyChallengeScreen.cs`.

### Full playtest

- [ ] T054 [US3] Extend `playtest/android/PlaytestMeta.cs` with the linked engine-free services:
  - `DailyRewardService`;
  - `CollectionService` (add each won picture);
  - `WardrobeService`, for the hero's outfit and Petal purchases;
  - Petal purchases of boosters, through `EconomyService`.

  Extend `playtest/check/Program.cs` with checks for them.
- [ ] T055 [P] [US3] Create `playtest/design/DailyRewardCard.cs` (frame 4). CLAIM calls `DailyRewardService.Claim`.
  "Get +N" is disabled with "no ads in playtest".
- [ ] T056 [P] [US3] Create `playtest/design/LeaderboardCard.cs` (frame 5), in the offline form. It shows the
  player's own row and the notice, with no invented players (research R12).
- [ ] T057 [P] [US3] Create `playtest/design/CollectionCard.cs` (frame 6): a grid of framed pictures and a detail view.
- [ ] T058 [P] [US3] Create `playtest/design/StoreCard.cs` (frame 17).
  - Booster rows buy with Petals.
  - Real-money rows show "unavailable".
  - The Cosmetics tab appears after L40.
- [ ] T059 [US3] Add frames 4, 5, 6 and 17 to `playtest/preview/Frames.cs`.

**Checkpoint**: All 17 frames have a counterpart in both clients. `board-sheet.png` is ready for the SC-001 review.

---

## Phase 6: User Story 4 — Asset inventory (Priority: P2)

**Goal**: A complete, generated list of the art and audio assets that replace the placeholders (FR-029, FR-030,
SC-003).

**Independent Test**:
- Take any placeholder in a preview image or the game and find its entry.
- Take any entry and find where it is drawn (the preview's "used by" record).

- [ ] T060 [US4] Complete the registry in `client/Assets/Bloomlings/UI/Design/AssetSlots.cs` with every category of
  FR-029:
  - the brand (wordmark, app icon, splash art);
  - the backgrounds per theme and per screen;
  - the characters: four families, with idle, walk, work, finish, stuck, celebrate and hero poses (doc 12 §2), plus
    variant accents;
  - 12 symbols;
  - tiles and overlays;
  - specials;
  - pods and slots in every state;
  - boosters;
  - the UI kit;
  - currency and rewards;
  - Collection frames;
  - cosmetics, from `CosmeticCatalog`;
  - effects;
  - fonts;
  - audio.

  Each entry has its frames, uses, states, size class, readability duty and priority.
- [ ] T061 [US4] Add `--inventory` to `playtest/preview/Program.cs`.
  - It writes `specs/002-ux-design-board/asset-inventory.md`, grouped by category (the row format of
    `contracts/asset-slots.md`).
  - It adds a summary: the counts per category and per priority.
- [ ] T062 [US4] Add the coverage checks (SC-003):
  - in `playtest/preview/Program.cs`, every slot is used by a rendered frame or referenced by the Unity client;
  - in `client/Assets/Bloomlings/Tests/EditMode/AssetSlotTests.cs`, every `ProceduralSprites.Shape` id and backdrop
    layer is registered, and every `SoundCue` has an `audio.cue` slot.
- [ ] T063 [US4] Generate and commit `specs/002-ux-design-board/asset-inventory.md`. Check that it is in sync
  (`git diff --exit-code`).

**Checkpoint**: The inventory is complete both ways.

---

## Phase 7: Polish and cross-cutting concerns

- [ ] T064 [P] Add every new player-facing string to
  `client/Assets/Bloomlings/UI/Localization/Resources/Strings_en.csv`, for example:
  - "Milestone reached!", "NO MOVES LEFT", "Use a booster to continue", "Free rescue";
  - "N levels to reward", "Rank #N", "New today", "Completed at Level N", "Day N", "Get +N", "×2 reward";
  - "offline".

  Use them through `Loc.T` and `PlaytestText`, and keep `LocalizationTests` green.
- [ ] T065 [P] Update the docs:
  - `playtest/README.md`: the full playtest's designed screens, the tester unchanged, and the preview tool;
  - `client/README.md`: the design kit;
  - the `CLAUDE.md` build-command list: the preview tool command.
- [ ] T066 Run every suite:
  - `dotnet test core/Bloomlings.sln`;
  - `dotnet test client/DotnetCheck/...`;
  - `dotnet run --project playtest/check`;
  - `dotnet run --project playtest/preview`;
  - `node --test backend/tests/*.test.js`.

  Fix what fails. Test counts in core and backend stay the same (FR-004).
- [ ] T067 Type-check both playtest flavors against `Mono.Android.dll`, as done before for the APK projects, so the
  manual APK build is expected to pass.
- [ ] T068 Review `playtest/preview/out/board-sheet.png` side by side with `ux-design-board.webp` (SC-001).
  - Fix mismatches in layout, order and hierarchy.
  - Record any remaining deviation in `contracts/screen-map.md`.
- [ ] T069 Add `specs/002-ux-design-board/checklists/device.md` with the steps that need a phone or Unity:
  - SC-004's player test;
  - SC-005 fps on a low-end device;
  - SC-006 time-to-PLAY;
  - the three aspect ratios on real phones;
  - the Unity Editor run.
- [ ] T070 Mark the finished tasks in this file, commit and push to `claude/great-darwin-6qrpj8`.

---

## Dependencies and execution order

- **Setup (T001–T003)**: T003 depends on T020 existing for the `MainActivity` switch. Land T003's rename first, and the
  switch together with T020.
- **Foundational (T004–T022)**:
  - T004 comes before T005.
  - T006 comes before T007, which comes before T008.
  - T005 and T006 come before T012 and T013.
  - T005 comes before T014, which comes before T015 (Unity).
  - T017 comes before T018, T019, T021 and T022.
  - T019 comes before T020.
- **US1 (T023–T035)**: needs Phase 2. The Unity (T023–T029) and playtest (T030–T035) tracks are independent. T034
  needs T030–T033.
- **US2 (T036–T048)**: needs Phase 2. T037 needs T011, and T043 needs T011 and T022. It uses the US1 level screen for
  the jam, pause and win overlays.
- **US3 (T049–T059)**: needs Phase 2. T055–T058 need T054.
- **US4 (T060–T063)**: needs US1–US3, because it inventories their placeholders.
- **Polish (T064–T070)**: last. T064 can run alongside the stories as strings appear.

## Parallel opportunities

- **Phase 2**: T004, T009 and T016 in parallel. After T005 and T006: T013, T014, T017 and T018.
- **US1**: T025–T029 (Unity views) in parallel, and T030–T033 (playtest painters) in parallel.
- **US2**: T036 and T038–T041 in parallel; T044–T047 in parallel.
- **US3**: T049–T053 in parallel; T055–T058 in parallel.

## Implementation strategy

1. **MVP**: Phases 1–3 (US1). The gameplay screen in the board's style in both clients, checked by the previews and the
   existing suites.
2. **Then** US2 (the flow screens) and US3 (the meta screens), each checked by its preview frames.
3. **Last**, US4 turns the registered placeholders into the generated asset inventory. The owner receives it as the art
   production list.
