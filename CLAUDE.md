# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Bloomlings** is a light, minimal, strictly 2D mobile puzzle game modeled on the structure of
*Colony Flow!* (ABI Games). The player taps numbered Spirit Pods from a stacked Source Tray into
5 Waiting Slots. Bloomlings then automatically clear reachable board tiles of the pod's exact
target variant. The level is lost when the slots jam. There are 4 character families (Sprig,
Bloom, Drop, Twig) and 8 exact target variants at launch (Leaf/Moss, Flower/Violet Bud,
Water/Dew, Wood/Acorn). Progression is a linear sequence of 5000+ levels, with no map.

## Current stage: implementation of spec 001 (development gate open)

- **Gameplay reference: Colony Flow! (ABI Games).** Keep its core gameplay idea, its simplicity,
  how it paces new levels and mechanics, and how simple and convenient its screen and level
  layouts are.
- **Design docs: `product/` v0.5** (LOCKED on 2026-09-29, see `specs/001-core-game-mvp/gate.md`). `LOCKED_CONCEPT_v0.5.md` is the summary,
  and `01`–`15` are the detailed documents: rules, tray/buffer, tiles and variants, mechanics,
  level structure, generator/solver, difficulty, meta, boosters, economy, UX, art, unlock
  roadmap, MVP scope and technical architecture. `CONCEPT.md` is the original v0.1 vision. Treat
  the docs as the current design direction, not as immutable rules, and flag any conflicts to
  the user.
- **Consolidated requirements: `specs/001-core-game-mvp/spec.md`.** Do not implement gameplay
  that is not specified there.
- **Implementation plan: `specs/001-core-game-mvp/plan.md`** (+ `research.md`, `data-model.md`,
  `contracts/`, `quickstart.md`): monorepo `core/` (pure C# rules, solver, generator, pipeline CLI),
  `client/` (Unity 6.3 LTS), `content/`, `backend/`.
- **Development gate** (from `LOCKED_CONCEPT_v0.5.md`): passed on 2026-09-29, all docs are locked. Changes to
  locked docs now go through the product owner.
- **Technical direction** (doc 15, locked): Unity + C#, a deterministic data-driven
  gameplay core, an offline generator and solver, versioned level definitions, and a lightweight
  backend.

## Build and test commands

Requires the .NET 10 SDK (pinned by `core/global.json`; outputs go to `core/artifacts/`).

- `dotnet build core/Bloomlings.sln` builds the shared libraries, tools and tests.
- `dotnet test core/Bloomlings.sln` runs the core, content, solver and generator tests.
- `dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj` compiles the Unity client scripts against
  API stubs and runs the engine-free EditMode tests under .NET (extend `client/DotnetCheck/UnityStubs.cs` when the
  client uses a new Unity API).
- `node --test backend/tests/*.test.js` runs the Cloud Code script tests (in-memory stand-ins for the UGS modules).
- `dotnet run --project playtest/check` checks the playtest client without Android: its animator replays every golden
  case and showcase solution, and a 22×28 big level (the generator tests' fixture), and must end on the rules state, and
  its meta layer runs progression and economy.
- `dotnet run --project playtest/preview` renders the full playtest's designed screens (spec 002) for every design
  board frame (1–17, plus extras 18–32: themes, Settings, Collection picture, 21 a booster's guided demo, boosters in
  use, the Bloomlings sheet, 25 the reference-look kit, 26 the Store cosmetics, 27 the Wardrobe, 28 Home's animated heroes
  in outfits, the bottom menu's locked places: 29 the locked Store page, 30 the locked Wardrobe, 31 the locked
  Leaderboard page, and 32 the Remove Ads card of Home's No Ads scene; and the guided spotlights 33–38: the entry, the
  first tap, the blocked entry, the kept charge, Return's slot, Bloom Burst's tiles; the profile 39–41: the page, the
  edit card's avatars, its name and frames; and the Store's Animations 42–43: the clearing styles' live previews, then
  their padlocks before L40; the lotus iris 44–47: the splash opening on Level 1, then the win's Next closing, closed
  on "Level 13" and opening) to PNG in `playtest/preview/out/` (gitignored) and checks
  slots, touch targets and the safe area; `-- --inventory` regenerates `specs/002-ux-design-board/asset-inventory.md`
  from the asset slot registry.
- `BLOOMLINGS_GOLDEN_REGEN=1 dotnet test core/Bloomlings.sln --filter GoldenReplayTests` regenerates golden replays
  after an intended, reviewed rules change (`core/tests/golden/README.md`).
- `dotnet run --project core/src/Bloomlings.Pipeline -- <command>` runs the content pipeline CLI
  (`contracts/pipeline-cli.md`). Generated batches go to `content/work/` (gitignored). The picture library in
  `content/pictures/lib` is mostly `draft` until a person approves it (FR-084): `generate --allow-draft` builds
  previews from drafts, `--history <batch>` chains preview batches, and `validate` still fails such levels on
  `picture-approved`. `content/readability/approved-pairs.json` is `provisional` until the readability sign-off.
  Mechanic showcase levels live in `content/showcase/` (generated with `generate --mechanics <m> --class normal`);
  `generate` keeps them fixed (`--keep`). `generate --segments N` fixes how the range is cut for `--jobs` threads, so
  the levels never depend on the machine's cores. The Daily Challenge pool is `content/daily/` (T185; FR-064 as
  amended on 2026-10-07: 365 entries, each on a picture of its own, 22×28, 4 Normal / 2 Hard / 1 Super Hard a week,
  `daily generate --seed 1 --count 365 --segments 7`; `publish --daily content/daily` packs it). Its pictures are the
  `daily` theme's (`sketch_pictures.py --daily`, 128 subjects the levels never draw, three each): only the daily profile
  takes them, and `validate` refuses one in a level.
- `tools/catalog/build-catalog.ps1` (Windows PowerShell 5.1 and PowerShell 7) and its twin `build-catalog.sh` build and
  validate `content/catalog/` band by band (`tools/catalog/README.md`): each band's profile, seed and fixed segments, the
  earlier bands as history, two more seeds for a level without an accepted candidate (else a recorded gap), every band
  validated in context before it is copied in, `content/catalog/build-manifest.jsonl` per band; resumable, and
  `-Check <band>` / `--check <band>` regenerates a band to compare it file by file. It never commits.
- Boards (spec 001 FR-008 and FR-036 as amended on 2026-10-07, the owner): the curated Levels 1–10 keep 11–12×12; from
  L11 every board size from 14×16 to 22×28 (`CellPos.MaxWidth`/`MaxHeight`) comes about as often as any other
  (`BandGuidelines.AnyBoard`; `PicturePicker.Pick` draws a size first, then a picture of it), so about half the levels are
  big. A board of 224–288 cells shows the layer peek; a big board of 289–616 cells is drawn in the icons look with the
  next layer hidden. The look is level data, never chosen per device (`boardLook`, `BoardLooks.For`; absent means
  peek): the generator writes it from the cell count, `validate` fails a mismatch, `LevelView` hides `CellInfo.Next` on
  an icons board and both builds skip the chip. An icons board's hidden layers pass the sampled fairness check of
  research R8b (`HiddenLayerFairness`, at most 72 layers, no mystery), and its buffer pressure is lower
  (`BigBoardPeakSlots`). A band row's pods, work and durations are a regular board's; on a big board they grow with its
  cells (`BandGuidelines.For(level, cells)`), and its class thresholds go from the band's own at 288 cells to the band's
  `big` ones at 616 (`BandGuidelines.ThresholdsFor`, `difficulty-thresholds.json`), so any class may fall on any board.
  Practice levels are never Super Hard (FR-059 as amended): with the roadmap, `DifficultySchedule` moves such a Super
  Hard to the next level that is not a showcase, a practice or a milestone level, and `validate` refuses a Super Hard
  practice level. A Hard or Super Hard level has at most 3 Source stacks (FR-011 as amended on 2026-10-08,
  `BandGuidelines.Stacks`, `HardMaxStacks`); a Normal level keeps its band's 2–6.
- `tools/heroanim` (Node 22, not in the solution; `tools/heroanim/README.md`) pre-renders the owner's animated FBX
  heroes and prepares the layered Home: `cd tools/heroanim && npm ci`, then `node bake.mjs` (the four heroes, about 6
  minutes; `--only <family>`) and `node layers.mjs <folder>` (the owner's Home layers). `node tools/heroanim/check.mjs`
  (no npm packages) must pass before committing hero frames or Home layers.
- `dotnet run --project tools/appicon` (not in the solution; `tools/appicon/README.md`) cuts the owner's app icon picture
  (`tools/appicon/source/app-icon.png`) into the Unity client's icon textures (`Art/Brand/AppIcon/`, set by
  `CiBuild.ApplyIcons`) and both playtest APKs' launcher mipmaps (`playtest/icon/`, adaptive, round and legacy).
- Open `client/` with Unity 6.3 LTS for the game client; see `client/README.md` for the first-open steps.
- CI: **every workflow is manual only for now** (Actions → Run workflow), so pushes and pull requests spend no
  Actions minutes (the owner's budget rule). Run the tests above locally before every push instead.
  `core-tests.yml` builds and tests `core/` and the client check; `content-validate.yml` validates content and diffs
  it against `main`; `catalog-nightly.yml` certifies the whole catalog. `android-apk.yml` builds the temporary
  playtest APKs (.NET for Android on the shared core, no secrets): the full playtest (`playtest/android`) and the level
  tester (`playtest/tester`), both or one of them; `unity-apk.yml` builds the Unity
  client's APK and needs the `UNITY_EMAIL`, `UNITY_PASSWORD` and `UNITY_LICENSE` secrets. Both APK workflows keep only
  the newest APK artifact (older ones are deleted, and each expires after 7 days). Do not add push, pull_request or
  schedule triggers until the owner allows it, and give every uploaded artifact a short `retention-days`.
- `playtest/` is a temporary playtest client, not the product client (see `playtest/README.md`), kept so an APK builds
  without Unity secrets. It builds two APKs from the same sources: the full playtest and the level tester
  (`PLAYTEST_TESTER`: ◀ ▶ between levels, free boosters, instant results). Never let gameplay or meta rules live there: it draws `LevelView`, plays the core's events
  (`LevelAnimator`) and calls the Unity client's engine-free services, linked from `client/` (save, progression,
  economy, milestones, sound). Keep those client files engine-free so the link keeps compiling.
- The look comes from the design board (`specs/002-ux-design-board/`): its tokens, shapes, garden backdrop, layouts and
  asset slot registry are one engine-free kit in `client/Assets/Bloomlings/UI/Design/`, linked into the playtest. Use
  token names, never literal colors or sizes; every placeholder shape or procedural visual is a registered asset slot
  (`AssetSlots`), which the asset inventory is generated from.
- The cartoon "Garden" look (`specs/003-cartoon-ui-style/`) lives in the same kit: `DesignTokens.Garden` and the
  `garden.*` colors, `GardenLook` (color sets, label looks, press/breath/count-up/glow curves, booster tile states,
  decoration). Buttons are a raised face on a plate (`Kit.GardenButton` / `UiKit.Garden`; since spec 005 FR-045 every
  button's face is raised on the wooden plate, `Kit.RaisedButton` / `UiKit.RaisedButton`, below), labels are sentence case in the bundled Nunito font
  (`client/Assets/Bloomlings/UI/Fonts/Resources/`, SIL OFL; only `type.badge` stays uppercase), and the board, pods and
  slots are volumetric 2D, never 3D. The level tester keeps its minimal look.
- The characters are generated art (`specs/004-character-art/`, replacing spec 003's kawaii figures): each variant is a
  2D character whose whole shape is its symbol, with a face (happy, asleep in the stack, worried when stuck, blank under
  a worn expression), as the walking Bloomlings and on the Bloomlings sheet (since spec 005, pods, slots and board
  tiles show candy tiles instead); the four families are 3D heroes on the meta screens only (splash, Home, win and
  milestone cards, Wardrobe, profile). `tools/artgen` (SkiaSharp
  vector drawing and a CPU raymarcher, not in the solution) writes them as PNG files with a `manifest.json` into
  `client/Assets/Bloomlings/Art/Characters/Resources/Characters/`: `dotnet run --project tools/artgen -- build`
  (about 10 minutes for the 3D set; `--only 2d` takes a second), `-- check` (must pass before committing art changes),
  `-- sheet` (review sheet in `tools/artgen/out/`), `-- adopt 3d/<file>.png` (records an owner picture as
  `"source": "owner"` with its source record: `build` keeps it, `check` verifies its hash, size and margin instead of
  re-rendering it). Names and placements come from the kit's `CharacterArt`; Unity loads them with `CharacterSprites`,
  the playtest and preview embed them (`IPainter.Sprite`). A missing picture falls back
  to the spec 002 family silhouette. The art is the project's own work (`tools/artgen/OWNERSHIP.md`). Over the owner's
  layered Home picture, Home and the splash show the owner's animated heroes instead (below; `HomeStage.ShowsHeroes`);
  the drawn stand-in keeps the still ones.
- The reference look (`specs/005-reference-look/`, after the owner's `reference.jpg`; recipes in `contracts/look.md`)
  restyles every element of the Unity client and the full playtest, presentation only: layouts, order, rules and tap
  outcomes stay (FR-002). It adds the saturated variant palette (`VariantCatalog`, readability-checked) and the
  material tokens (`wood.*`, `stone.*`, `parchment.*`, `cream.*`, `ink.*`, `lotus.*`, `lawn.*`, `ivy.*`). `UiRaster`
  (kit) renders engine-free material pictures (planks, pod frames, stones, pedestal, candy tiles; deterministic,
  straight alpha), drawn through `IPainter.Picture` (playtest) and `ProceduralSprites.Picture` (Unity), each cached by
  key and size. `BoardLayout` places the grid and the stone border; each Garden Entry is a small stone arch set in the border beside its entry cell, turned to its side (`BoardLayout.Arch`, `UiRaster.EntryArch`; the owner's "B" of 2026-10-05, FR-034; the big arch under the board stays retired since 2026-10-03), and the Bloomlings set off from there. The board is candy tiles (soft cubes since FR-044, below) in a
  stone border on a lawn (a big board, over 288 cells, in the icons look without the layer chip). Pods are wooden frames wider than tall (the owner's icon in the middle, a small count in the corner) that stand one
  after another in a column per Source stack, never on each other: a gameplay rule (owner, 2026-10-03), whatever the
  look; 3 rows, 4 from a safe aspect of 1.95 (`ReferenceGameplayRegions.Pod`, `PodChip`). A connected pod carries a chain badge
  of its group's color wherever it stands, a column whose "+N" hides one shows a small one, and a tap that waits for a
  partner says "Its linked pod isn't on top yet" and pulses it (FR-043, `PodLinks`, `UiRaster.LinkBadge`). Waiting Slots are cream
  plates holding the tile with its count below; cards are parchment; Petals is a pink lotus. Components are `Kit.*`
  with same-named `UiKit*` twins. The owner's backgrounds keep at most 70% of the animated heroes' saturation (FR-031):
  run `node tools/heroanim/saturation.mjs` after adding one (idempotent; the Home layers through `layers.mjs`).
  Home's two promo scenes (FR-032, `HomePromo`, the owner's layers `Decor/promo-*.png`): No Ads at the left (from L1
  until Remove Ads is owned; a tap opens the Remove Ads card, FR-033) and Daily at the right (the Daily Reward), each
  idling and playing its attention sequence every 12 s, never together; hosts draw `HomePromo.Layers` per frame.
- The owner's Home tuning (spec 005 FR-036, research D29, 2026-10-05), chosen on a constructor page of the game's own
  layers: Home has no logo (the splash keeps it); the garden is blurred in `home.jpg` (`layers.mjs` `gardenBlur`); the
  fountain and heroes stand in `HomeLayers.Stage` (0.9 of the cover box) at `HeroScale` 1.05; the heroes' Home frames
  are sharpened and at 110% contrast and saturation (`heroes.json` `home`, `tools/heroanim/post.mjs`, applied by the
  bake); Play, the plaque, the promo scenes and the sun follow `ReferenceHomeRegions`' shares; each promo scene stands on
  the round buttons' cream cushion with soft shadows made from its pictures' alpha (`HomePromo.PlateBox`, `ShadowOf`,
  `UiRaster.SilhouetteShadow`, `RoundShadow`; `IPainter.SpriteAlpha`, `OwnerArt.DecorAlpha`). Home has no falling
  petals and Settings no switch for them (the owner removed both on 2026-10-06; older saves' `homePetalsOn` is ignored).
- The guided spotlights (spec 005 FR-035, the owner, 2026-10-05): `GuideTour` (kit) decides the onboarding's steps
  (Level 1's arch and forced first tap; the first level that starts with a pod's tiles out of reach, Level 2; each
  booster's forced demo at its unlock, Return's once a pod waits) and `Spotlight` lays them out (a scrim with soft holes,
  a ring, a parchment bubble, a hand); the playtest draws them with `GuidePainter`, Unity with `GuideOverlay` (a raycast
  filter lets taps through only inside the holes). A booster's guided use is free (no charge, clean-clear kept, analytics
  `source` `demo`; spec 001 FR-042 as amended). Mechanic and variant demos keep their cards.
- The clearing styles (spec 005 FR-038, the owner, 2026-10-06; recipes in `contracts/look.md` §6.12): the board clears in
  one of seven styles, presentation only. The kit's `ClearStyles` (the styles, the free pair by level, one trip time
  `TripSeconds` split into each style's legs (Colony Flow's ants' walk, 0.11 s a cell, a pod's walkers 0.30 s apart, since the owner's video of 2026-10-08), the walkers' line from each arch) and `ClearLook` (each frame's walkers,
  restores and flights as an `FxList` of items in cell units) are drawn by the playtest's `ClearPainter` and Unity's
  `ClearFxView`, so both builds and the Store's previews (`ClearPreview`; `Kit.ClearingPreview`, `ClearPreviewView`)
  show the same thing. Blossom and Munchers are free and alternate by level (`ClearStyles.ForLevel`); Fireflies,
  Bubbles, Pushers, Fireworks and Confetti Parade are board cosmetics bought once for Petals from L40 on the Store's
  Animations tab (`ClearingService`; Remote Config `economy.price.clearing`, the save's `clear.<name>` and
  `cosmetics.equipped.board.clearing`), and a chosen one plays on every level. The rules' clear stays at each trip's
  end; `LevelAnimator` and `TimelinePlayer` keep the same schedule.
- The clearing sounds and haptics (spec 005 FR-042, 2026-10-07; recipe in `contracts/look.md` §6.16): each style plays
  its act's soft textures at the moments its look draws them and its collect with each tile's clear, a pod's collects
  climbing a ladder of ten C-major-pentatonic notes (`ClearSounds`, `ClearLadder`; all synthesized by `ToneSynth.Clear`,
  no audio assets), and one micro haptic a tile (`HapticPattern`: transients where the phone renders them, a short pulse
  with amplitude control, nothing on a phone that can only buzz). `FeedbackPolicy` (engine-free, shared by both builds)
  spaces the sounds, keeps at most five starting within 0.2 s and a tile's tick 90 ms clear of the last pattern, in real
  time. Unity plays them through `GameFeedback` / `Haptics` (iOS: `Assets/Plugins/iOS/BloomlingsHaptics.mm`), the
  playtest through `PlaytestSound` (a SoundPool); the Store's previews stay silent. `dotnet run --project
  playtest/preview -- --sounds` writes every clip as WAV with a listening schedule to `playtest/preview/out/sounds/`.
- The lotus loader and the lotus iris (spec 005 FR-039, the owner's choice of 2026-10-06; recipe in
  `contracts/look.md` §6.13): the splash is the logo and the lotus on the parchment with a ring of petals filling as the
  game loads, then a round iris opens from the lotus on the first screen; the win's Next closes the iris, shows the lotus
  with "Level N", starts the next level under the cover (an interstitial that is due shows there) and opens on it. The
  kit's `LotusIris` holds the layout and the timing; the playtest's `LotusPainter` and Unity's `LotusIrisView` draw its
  `LotusPose` (the cover is `UiRaster.IrisHole` scaled to the hole plus plain boxes round it, never rendered per
  frame); Unity's `SplashScreen` takes its progress from `Boot`, and `LevelTransition` sits on the Boot object.
  `tools/loading-gifs` (Python) renders the review GIFs of the concepts from the game's pictures.
- Purchases and touches (spec 005 FR-040, FR-041, the owner, 2026-10-06; `contracts/look.md` §6.14, §6.15): every
  purchase, for Petals or real money, asks the purchase confirmation first (the kit's `PurchaseConfirmation` and
  `ScreenLayout.PurchaseConfirm`; the playtest's `DesignApp.ConfirmPurchase` / `Kit.PurchaseCard`, Unity's
  `UiKit.PurchaseCard`); never spend Petals or start a store purchase without it. Pages that scroll register their lists
  (the playtest's `IPainter.Scroll`, Unity's `UiKit.Scrolls`): there a drag past `touch.slop` (10 dp, `TouchGesture`)
  never taps and a swipe turns the page. Preview frame 49 shows the confirmation over the Store's Animations tab.
- The owner's animated heroes and layered Home (spec 005 FR-028, owner's delivery of 2026-10-02): `tools/heroanim`
  renders the owner's animated heroes offline into flat 24 fps frames, all four from the owner's `Heroes.glb`
  (2026-10-04; `heroes.json` picks each hero's mesh in it): Sprig with its 4 s idle, 3 s wave and the win's 2 s celebrate
  and 3 s clap, Bloom and Drop with a 4 s idle and a 1.5 s jump (Drop's breath added by the bake), and Twig, delivered
  without clips, with the bake's stand-ins (a 3 s idle, a 1.5 s hop, the win's 3 s jumps) until its own come, in
  `Art/Heroes/Resources/HeroMotion/`), and the Home picture comes as layers (`home.jpg` and `home-*.png` in
  `Art/Backgrounds/Resources/Backgrounds/`). The kit's `HeroMotion`, `HeroMotionPlayer`, `HomeLayers` and `HomeMotion`
  place and time them for both builds. Home and the splash stand the four heroes on the painted fountain as in the
  reference (idling, taking turns to react, reacting to a tap); the win and the milestone show the level's celebrant,
  Twig and Sprig by turns (`CharacterArt.CelebrantOf`; Sprig alternates its celebrate and clap, `HeroMotion.WinClip`),
  celebrating (`HeroMotionPlayer.Celebrate`), then idling, facing the player (their win set is baked a second time front
  on, `winYaw` and the `winidle` clip; Home's heroes stay turned toward the fountain's middle by their `yaw`). The Wardrobe, profile and the group keep the still pictures. Constitution VII: no 3D model,
  scene or camera in the game, only these flat pictures on meta screens. Frames load when first drawn into a bounded
  cache, never all.
- The profile page (spec 005 FR-037, the owner, 2026-10-05; after the reference game's): Home's avatar opens it (no
  bottom menu), and its "Edit profile" card picks the avatar, frame, badge and name. The owner's 14 avatar pictures are
  opaque 384 px JPEG in `client/Assets/Bloomlings/Art/Avatars/Resources/Avatars/` (`AvatarCatalog`: four free, ten for
  Petals at 300 / 600 / 1200, Remote Config `economy.price.avatar*`), shown in a rounded-square clip
  (`IPainter.PushClipRound` with a radius, Unity a `Mask` on a `UiKit.RoundRect`) that fills the avatar's whole disc
  inside a thin ring; the avatar has one border, the icon buttons' wooden plate or the shown frame in its place, never
  both (the kit's `AvatarLook`; the owner, 2026-10-06). Every icon button
  (Settings, Pause, close, back, the pencil, ‹ ›, the Daily Challenge) and the speed pill is a rounded square, a cream
  face raised on that wooden plate (`Kit.RimmedIconFace`, `Kit.RaisedPlate` / `UiKit.IconFace(..., rim: true)`,
  `UiKit.RaisedPlate`; `GardenLook.IconRadiusShare`, `IconRimShare`; FR-044, below); they were circles. Five drawn rounded-square frames (`ProfileFrames`,
  `UiRaster.ProfileFrame`: Wooden Frame, Leaf Frame, Flower Wreath, Stone Frame, Golden Ribbon; their catalog ids keep
  the `_ring` names) are the cosmetic catalog's `free` items: everyone's from Level 1 with no save entry
  (`WardrobeService.Owns`), listed first in the Frame tab before the Wardrobe opens too, never a default frame. The
  data is engine-free in `client/Assets/Bloomlings/Meta/Profile/` (`ProfileService`,
  `ProfileEditor`, linked into the playtest); the save keeps bought avatars in `cosmetics.owned`, the shown one in
  `cosmetics.equipped.profile.avatar`, the name and joining day in the optional `profile` section. The name stays on
  the device (the playtest asks with the system's text dialog through `ITextPrompt`, Unity uses a `TMP_InputField`);
  the three achievements (`Achievements`: Green Thumb, Picture Keeper, Daily Gardener, bronze, silver and gold from the save's counters) are shown only. Layouts: `ScreenLayout.ReferenceProfile`, `ProfileEdit`.
- The volume look (spec 005 FR-044, the owner, 2026-10-08: "cells like cubes, but not cubes", buttons "as if laid on a
  volumetric plane"; recipe in `contracts/look.md` §6.18): the kit's `VolumeRaster.cs` (part of `UiRaster`, engine-free
  pictures lit from the upper left) draws every board tile as a soft cube (`UiRaster.Cube` through `Tile`'s board style:
  a rounded top over a front face, `CubeSide`; the icon on the top's middle, `TileLipShare`), the icon buttons', speed
  pill's and avatar's wooden plate (`ButtonPlate`), the cream face raised on it (`ButtonFace` in `RaisedFaceBox`, sunk by
  a press, `RaisedFaceSink`) and every brown glyph on a cream face raised (`RaisedGlyph`, `RaisedChevron`; its picture
  `GlyphPictureScale` larger than the glyph box, which Unity's `UiKit.RaisedGlyph` sets as the image's scale). The
  colors are the owner's pick (`PlateVivid`, `PlateDepth`, `FaceVivid`, `FaceWarmth`, half way between the review's 5a and
  6a). The playtest draws them with `Kit.RimmedIconFace`, `Kit.RaisedPlate`, `Kit.RaisedGlyph`; Unity with
  `UiKit.IconFace(..., rim: true)` (a picture face, `GardenButton.BuildPictureFace`), `UiKit.RaisedPlate`,
  `UiKit.RaisedGlyph` and `ProceduralSprites.ButtonPlate`, `ButtonFace`, `RaisedGlyph`, `RaisedChevron`. Flat 2D
  pictures only (constitution VII). Since FR-045 (below) every button stands on that plate, Play too.
- The popups after the owner's mockup (spec 005 FR-045, 2026-10-08: "rectangular with rounded corners", rows "like the
  buttons but without the rim", the sign "in the color we made", "every button the new ones, even Play"; recipe in
  `contracts/look.md` §6.19): a popup card (`Kit.Card` / `UiKit.Card`, the jam card, Unity's modal) is the wooden frame of
  the plate's laminate round a cream panel (`UiRaster.CardFrame`, `Kit.CardFrame` / `UiKit.CardFrame`), the main buttons'
  sprig over its top-left and bottom-right corners, its title on a wooden sign over the frame's top edge with the owner's
  lotus rising behind it and the close over the top-right corner (the kit's `CardLook`); every wooden sign is the
  laminate plank (`UiRaster.LaminateSign`, `Kit.WoodSign` / `UiKit.WoodSign`); every list row (Settings, Store,
  Leaderboard, the player's own row kept green) and the dev row's pills are a raised cream slab (`Kit.RaisedRow` /
  `UiKit.RaisedRow`), the Settings rows with their icons (`CardLook.SettingsIconOf`); every primary and secondary button,
  the jam's choices and the clearing cards' buttons are raised on the plate (`Kit.RaisedButton` / `UiKit.RaisedButton`,
  `UiRaster.ButtonPlate(w, h, share)`, `ButtonFace(w, h, set, share, gloss)`: 0.5 for the pills, 0.22 for the choices,
  the colored faces glossy). The booster tiles, tabs and cost pills keep their own look; the pages' panels stay parchment.
- The owner's pictures (3D heroes and poses, backgrounds, logo) are listed with sizes and slots in
  `specs/005-reference-look/pictures.md` (names in `OwnerPictures` and `CharacterArt`): `Art/Backgrounds/Resources/`,
  `Art/Brand/Resources/` (Unity `OwnerArt`, the playtest embeds them) and the artgen folder's `3d/` (record them with
  `artgen -- adopt`). The drawn stand-in shows while a file is missing; every picture needs a source record for the
  originality test.
- Player-facing text lives in `client/Assets/Bloomlings/UI/Localization/Resources/Strings_en.csv` and is read with
  `Loc.T("key")` (the playtest: `PlaytestText.T`); `LocalizationTests` fails on UI literals and on unknown keys in
  either build.
- Analytics go through `GameAnalytics` (events of `contracts/analytics-events.md`, held until consent); a test keeps
  the event catalog equal to the contract.
- Checklists for the human, Editor and device steps (accessibility, performance, originality, playtests, quickstart
  run, and the release steps of `release.md`, such as turning the Daily Challenge on) are in
  `specs/001-core-game-mvp/checklists/`.

## Spec-Driven Development (GitHub Spec Kit)

The repo is initialized with [Spec Kit](https://github.com/github/spec-kit) for the Claude
integration (`.specify/` + `.claude/skills/speckit-*`). Feature work goes through these skills,
in order:

1. `/speckit-constitution` — project principles → `.specify/memory/constitution.md`
   (ratified 2026-09-29, current **v1.0.2**; amend only via PR with a version bump — see its Governance).
2. `/speckit-specify <description>` — feature spec → `specs/NNN-<name>/spec.md`
3. `/speckit-clarify` (optional) — resolve ambiguities before planning
4. `/speckit-plan` — implementation plan, research, data model, contracts
5. `/speckit-checklist` (optional) — requirement quality checklists
6. `/speckit-tasks` — `tasks.md` broken down by user story
7. `/speckit-analyze` (optional) — cross-artifact consistency check
8. `/speckit-implement` — execute tasks
9. `/speckit-converge` — compare code against spec and append remaining tasks

Layout:

- `.specify/templates/` — spec/plan/tasks/checklist/constitution templates
- `.specify/scripts/bash/` — helper scripts the skills call (feature numbering, paths, prerequisites)
- `.specify/memory/constitution.md` — project constitution
- `specs/NNN-<short-name>/` — per-feature artifacts (created by `/speckit-specify`)

Features are numbered sequentially (`001-`, `002-`, …). `.specify/feature.json` points the
skills at the current feature. It is machine-local state and is gitignored, so a fresh clone
(every cloud session) does not have it, and `/speckit-plan`, `/speckit-tasks` and the other skills
then fail with "Feature directory not found". Recreate the file before running them:
`echo '{"feature_directory":"specs/001-core-game-mvp"}' > .specify/feature.json`.
Alternatively, set `SPECIFY_FEATURE_DIRECTORY`.

To upgrade Spec Kit templates/skills: install the CLI with
`uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`, then run
`specify init --here --force --non-interactive --integration claude --script sh`.

## Design invariants to respect in any spec or code

Summary of the constitution (`.specify/memory/constitution.md` v1.0.2, principles I–VII). The
constitution is authoritative; it adds the Colony Flow structure, fair monetization (no lives,
no pay-to-win), simplicity/offline-first and the workflow gates.

- Exact matching: a pod clears only its exact target variant. A family (Sprig, Bloom, Drop,
  Twig) is never a wildcard.
- Deterministic rules: the same level definition plus the same tap sequence always gives the
  same outcome, independent of animation, fast forward or device.
- Every gameplay cell is unambiguous: fully one thing, never partially occupied.
- Difficulty comes from source ordering, dependencies, variants and mechanics, not from tile HP
  or repetitive tapping.
- Every shipped level is solver-validated, winnable without boosters, and has an exact
  per-variant work accounting.
- There are no gameplay power upgrades; progression rewards are cosmetic or convenience only.
- Flat 2D: what the player plays and navigates has no 3D scene, camera or perspective view. Pre-rendered 3D
  illustrations may appear as flat pictures on meta screens only (Home, win and milestone, Wardrobe, profile), never
  inside a level.
