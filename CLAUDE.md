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
  case and showcase solution and must end on the rules state, and its meta layer runs progression and economy.
- `dotnet run --project playtest/preview` renders the full playtest's designed screens (spec 002) for every design
  board frame (1–17, plus extras 18–31: themes, Settings, Collection picture, demo, boosters in use, the Bloomlings
  sheet, 25 the reference-look kit, 26 the Store cosmetics, 27 the Wardrobe, 28 Home's animated heroes in outfits, and
  the bottom menu's locked places: 29 the locked Store page, 30 the locked Wardrobe, 31 the locked Leaderboard page) to PNG in `playtest/preview/out/` (gitignored) and checks
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
  `generate` keeps them fixed (`--keep`).
- `tools/heroanim` (Node 22, not in the solution; `tools/heroanim/README.md`) pre-renders the owner's animated FBX
  heroes and prepares the layered Home: `cd tools/heroanim && npm ci`, then `node bake.mjs` (the four heroes, about 6
  minutes; `--only <family>`) and `node layers.mjs <folder>` (the owner's Home layers). `node tools/heroanim/check.mjs`
  (no npm packages) must pass before committing hero frames or Home layers.
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
  decoration). Buttons are a raised face on a cream plate (`Kit.GardenButton` / `UiKit.Garden`; main buttons sit in a
  light wood rim since spec 005), labels are sentence case in the bundled Nunito font
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
  key and size. `BoardLayout` places the grid and the stone border (no arch at the entries since 2026-10-03: the Bloomlings set off from the border beside the entry cell). The board is candy tiles in a
  stone border on a lawn. Pods are wooden frames wider than tall (the owner's icon in the middle, a small count in the corner) that stand one
  after another in a column per Source stack, never on each other: a gameplay rule (owner, 2026-10-03), whatever the
  look; 3 rows, 4 from a safe aspect of 1.95 (`ReferenceGameplayRegions.Pod`, `PodChip`). Waiting Slots are cream
  plates holding the tile with its count below; cards are parchment; Petals is a pink lotus. Components are `Kit.*`
  with same-named `UiKit*` twins. The owner's backgrounds keep at most 70% of the animated heroes' saturation (FR-031):
  run `node tools/heroanim/saturation.mjs` after adding one (idempotent; the Home layers through `layers.mjs`).
  Home's two promo scenes (FR-032, `HomePromo`, the owner's layers `Decor/promo-*.png`): No Ads at the left (from L1
  until Remove Ads is owned; a tap opens the Remove Ads card, FR-033) and Daily at the right (the Daily Reward), each
  idling and playing its attention sequence every 12 s, never together; hosts draw `HomePromo.Layers` per frame.
- The owner's animated heroes and layered Home (spec 005 FR-028, owner's delivery of 2026-10-02): `tools/heroanim`
  renders the owner's animated heroes offline into flat 24 fps frames (two Meshy FBX heroes with a 4 s idle loop and
  a 2 s reaction each, Twig's Blender `.glb` with its 3 s idle, 1.5 s reaction and the win's 3 s cheer, and Sprig's
  with its 4 s idle, 3 s wave and the win's 2 s celebrate and 3 s clap, in
  `Art/Heroes/Resources/HeroMotion/`), and the Home picture comes as layers (`home.jpg` and `home-*.png` in
  `Art/Backgrounds/Resources/Backgrounds/`). The kit's `HeroMotion`, `HeroMotionPlayer`, `HomeLayers` and `HomeMotion`
  place and time them for both builds. Home and the splash stand the four heroes on the painted fountain as in the
  reference (idling, taking turns to react, reacting to a tap); the win and the milestone show the level's celebrant,
  Twig and Sprig by turns (`CharacterArt.CelebrantOf`; Sprig alternates its celebrate and clap, `HeroMotion.WinClip`),
  celebrating (`HeroMotionPlayer.Celebrate`), then idling. The Wardrobe, profile and the group keep the still pictures. Constitution VII: no 3D model,
  scene or camera in the game, only these flat pictures on meta screens. Frames load when first drawn into a bounded
  cache, never all.
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
  run) are in `specs/001-core-game-mvp/checklists/`.

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
  same outcome, independent of animation, 2x speed or device.
- Every gameplay cell is unambiguous: fully one thing, never partially occupied.
- Difficulty comes from source ordering, dependencies, variants and mechanics, not from tile HP
  or repetitive tapping.
- Every shipped level is solver-validated, winnable without boosters, and has an exact
  per-variant work accounting.
- There are no gameplay power upgrades; progression rewards are cosmetic or convenience only.
- Flat 2D: what the player plays and navigates has no 3D scene, camera or perspective view. Pre-rendered 3D
  illustrations may appear as flat pictures on meta screens only (Home, win and milestone, Wardrobe, profile), never
  inside a level.
