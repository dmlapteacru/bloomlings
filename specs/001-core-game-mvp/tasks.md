---

description: "Task list for the Bloomlings launch game (spec 001)"
---

# Tasks: Bloomlings Launch Game (Colony Flow–style buffer puzzle)

**Input**: Design documents from `specs/001-core-game-mvp/`: [plan.md](plan.md), [spec.md](spec.md),
[research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md).
The constitution is `.specify/memory/constitution.md` v1.0.0.

**Prerequisites**:

- plan.md, spec.md, research.md, data-model.md and contracts/ are all present.
- **Development gate**: T001 must be confirmed before any task after Phase 1 starts. Constitution → "Development
  Workflow and Quality Gates".

**Tests are REQUIRED for this feature.** The constitution requires every change to rules code to include rule tests,
golden replay corpus updates and passing determinism checks. Every content change must pass pipeline validation in
CI. Test tasks are therefore listed before the implementation they cover. Pure UI presentation gets no automated
tests; it is covered by the [quickstart.md](quickstart.md) scenarios.

**Organization**: Tasks are grouped by user story (US1–US7 from spec.md), so each story can be implemented and
validated as an increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task).
- **[Story]**: the user story the task belongs to (US1–US7). Setup, Foundational and Polish tasks carry no story
  label.
- Every task names its exact file path(s).

## Path Conventions (from plan.md → Project Structure)

| Path | Contents |
|---|---|
| `core/src/<Project>/` | Pure C# (netstandard2.1 / C# 9 for Core, Content and Solver; net10.0 for Generator and Pipeline) |
| `core/tests/<Project>.Tests/` | NUnit + FsCheck on net10.0 |
| `core/tests/golden/` | Golden replay corpus |
| `client/` | Unity 6.3 LTS project; runtime code in `client/Assets/Bloomlings/` |
| `content/` | Pictures, profiles, curated levels, catalog, roadmap tables |
| `backend/` | UGS Cloud Code scripts and Remote Config defaults |
| `.github/workflows/` | CI |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Gate check and repository scaffolding. The constitution allows this before the product docs are locked.

- [X] T001 Record the development-gate decision in `specs/001-core-game-mvp/gate.md`:
  - Ask the product owner whether `product/01`–`15` and `product/LOCKED_CONCEPT_v0.5.md` are locked. Locked means
    their status moved from "DRAFT FOR LOCK" to "LOCKED". The locks required are: the gameplay docs, the unlock
    roadmap, the generator/solver rules, the launch content strategy and the technical architecture.
  - Write the answer and the date.
  - If the docs are NOT locked, stop after Phase 1. Only scaffolding and explicitly labelled prototypes are allowed.
- [X] T002 Create the monorepo folders from plan.md:
  - `core/src/`, `core/tests/golden/`
  - `client/`
  - `content/pictures/src/`, `content/pictures/lib/`, `content/profiles/`, `content/curated/`, `content/catalog/`,
    `content/roadmap/`, `content/readability/`, `content/work/`
  - `backend/cloud-code/`, `backend/remote-config/`
  - `.github/workflows/`

  Then extend the root `.gitignore`:
  - `content/work/`, `build/`
  - `core/**/bin/`, `core/**/obj/`
  - `client/Library/`, `client/Temp/`, `client/Obj/`, `client/Build/`, `client/Builds/`, `client/Logs/`,
    `client/UserSettings/`
- [X] T003 [P] Create `core/Directory.Build.props` with `LangVersion=9.0`, `Nullable=enable`,
  `TreatWarningsAsErrors=true`, `Deterministic=true` and `InvariantGlobalization=true`. Create `core/global.json`
  pinning the .NET 10 SDK (`rollForward: latestFeature`).
- [X] T004 [P] Create the source project files:

  | Project file | Target | References / packages |
  |---|---|---|
  | `core/src/Bloomlings.Core/Bloomlings.Core.csproj` | netstandard2.1 | no package references |
  | `core/src/Bloomlings.Content/Bloomlings.Content.csproj` | netstandard2.1 | Core, `Newtonsoft.Json` 13.0.x |
  | `core/src/Bloomlings.Solver/Bloomlings.Solver.csproj` | netstandard2.1 | Core |
  | `core/src/Bloomlings.Generator/Bloomlings.Generator.csproj` | net10.0 | Core, Content, Solver |
  | `core/src/Bloomlings.Pipeline/Bloomlings.Pipeline.csproj` | net10.0 console, assembly name `bloomlings-pipeline` | all of the above, `System.CommandLine` 2.x, `JsonSchema.Net` |
- [X] T005 [P] Create the test projects on net10.0 with `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk` and
  `FsCheck.NUnit`, each referencing its source project:
  - `core/tests/Bloomlings.Core.Tests/Bloomlings.Core.Tests.csproj`
  - `core/tests/Bloomlings.Content.Tests/Bloomlings.Content.Tests.csproj`
  - `core/tests/Bloomlings.Solver.Tests/Bloomlings.Solver.Tests.csproj`
  - `core/tests/Bloomlings.Generator.Tests/Bloomlings.Generator.Tests.csproj`
- [X] T006 Create `core/Bloomlings.sln` containing all projects from T004–T005. Verify that `dotnet build
  core/Bloomlings.sln` and `dotnet test core/Bloomlings.sln` succeed on the empty projects.
- [X] T007 [P] Add Unity local-package manifests next to the shared sources. `.meta` files generated by Unity for
  these folders must be committed.
  - `core/src/Bloomlings.Core/package.json`: name `com.bloomlings.core`, unity `6000.3`.
  - `core/src/Bloomlings.Core/Bloomlings.Core.asmdef`: `noEngineReferences: true`, `autoReferenced: false`.
  - `core/src/Bloomlings.Content/package.json` and `Bloomlings.Content.asmdef`: depends on
    `com.unity.nuget.newtonsoft-json` and `com.bloomlings.core`.
  - `core/src/Bloomlings.Solver/package.json` and `Bloomlings.Solver.asmdef`: references Core.
- [ ] T008 Create the Unity project skeleton under `client/`:
  - `client/ProjectSettings/ProjectVersion.txt`: the latest `6000.3.x` LTS patch, recorded in `research.md` R1.
  - `client/Packages/manifest.json` with:
    - `com.unity.render-pipelines.universal`, `com.unity.ugui`, `com.unity.textmeshpro` (if not built in),
      `com.unity.localization`, `com.unity.addressables`, `com.unity.test-framework`,
      `com.unity.nuget.newtonsoft-json`;
    - `"com.bloomlings.core": "file:../../core/src/Bloomlings.Core"`, and the same for `.Content` and `.Solver`.

  Open the project once in Unity 6.3 and configure it:
  - portrait-only orientation;
  - IL2CPP with ARM64;
  - iOS minimum 15.0 and Android minimum API 26;
  - a URP 2D Renderer asset at `client/Assets/Settings/URP-2D.asset`;
  - company and product name "Bloomlings".
- [X] T009 [P] Create the client assembly definitions:
  - `client/Assets/Bloomlings/Bloomlings.Client.asmdef`, referencing `com.bloomlings.core`, `.content`, `.solver`,
    `Unity.TextMeshPro` and `Unity.Localization`;
  - `client/Assets/Bloomlings/Editor/Bloomlings.Client.Editor.asmdef`, editor only;
  - `client/Assets/Bloomlings/Tests/EditMode/Bloomlings.Client.Tests.EditMode.asmdef`;
  - `client/Assets/Bloomlings/Tests/PlayMode/Bloomlings.Client.Tests.PlayMode.asmdef`.
- [X] T010 [P] Add a root `.editorconfig` with C# conventions: 4-space indent, file-scoped namespaces off
  (C# 9), `var` when the type is apparent, private fields `_camelCase`.
- [X] T011 [P] Create the CI workflow `.github/workflows/core-tests.yml`. It runs on pull_request and on push to any
  branch, sets up .NET 10, then runs `dotnet build core/Bloomlings.sln -c Release` and
  `dotnet test core/Bloomlings.sln -c Release --logger trx`, and uploads the test results.
- [X] T012 [P] Update the "Current stage" section of `CLAUDE.md` with the real commands:
  - `dotnet build core/Bloomlings.sln`
  - `dotnet test core/Bloomlings.sln`
  - `dotnet run --project core/src/Bloomlings.Pipeline -- <command>`
  - opening `client/` with Unity 6.3 LTS

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared data model, board expansion, reachability, determinism primitives and content loading. Every
story depends on these.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete, and only after T001 confirms the gate.

- [X] T013 [P] Implement the target variants in `core/src/Bloomlings.Core/Variants/VariantId.cs`,
  `Family.cs`, `ColorGroup.cs` and `VariantCatalog.cs`, following data-model §1.1:
  - `VariantId` has launch ids `leaf, moss, flower, violet_bud, water, dew, wood, acorn` and expansion ids
    `vine, berry, mist, bark`.
  - `Family` is `sprig|bloom|drop|twig`; the family is "Character and animation family only; never used for matching".
  - `ColorGroup` is `green|pink_purple|blue_cyan|brown_orange|lime|red|indigo|gold`.
  - `VariantCatalog` stores family, colorGroup, `introducedAtLevel` and status (`launch|expansion`). Follow the rule "A
    variant catalog of at least 12 entries must be supported without rule changes".
- [X] T014 [P] Implement `core/src/Bloomlings.Core/Board/CellPos.cs`:
  - an `x` and `y` value type with row 0 at the bottom;
  - bounds `x ≤ 13`, `y ≤ 15`;
  - a row-major index helper;
  - the 4-neighbourhood iterator (no diagonals, FR-010);
  - a comparer implementing the fixed tie-break of FR-021: route distance ascending, then row ascending (bottom row
    first), then column ascending.
- [X] T015 [P] Implement immutable definition records in `core/src/Bloomlings.Core/Definitions/LevelDefinition.cs`,
  `PictureRef.cs`, `EntryDef.cs`, `CellOverlay.cs`, `SpecialDef.cs`, `LockDef.cs`, `SlotsDef.cs`, `TrayDef.cs`,
  `PodDef.cs` and `DifficultyDef.cs`. They mirror `contracts/level-definition.schema.json` and data-model §1.3.
  Document these constraints verbatim in XML docs:
  - entries: "At least 1 entry; default is bottom-center (FR-009)";
  - `layersBelow`: "depth ≤ 2 before L125 and ≤ 3 after (FR-036)";
  - slots: "At most 1 locked slot, and only from L80 (FR-039)";
  - `tray.stacks`: "2–6 stacks (tuned per band)";
  - pods: "`count ≥ 1`. Connected groups have 2 members (3 only if that mechanic is unlocked). Members of a connected
    group sit at the **same depth** in different stacks".
- [X] T016 [P] Implement the `BasePicture` record in `core/src/Bloomlings.Core/Definitions/BasePicture.cs` and
  `PictureRole.cs`, following data-model §1.2 and `contracts/base-picture.schema.json`:
  - width and height: "7 ≤ width ≤ 14, 8 ≤ height ≤ 16";
  - roles: "At least 2 roles; each role has exactly one color group";
  - grid rows run bottom-first, and each value is a role index, `-1` = EMPTY or `-2` = STONE;
  - `review.status`: "Only `approved` pictures can be used".
- [X] T017 [P] Implement `SplitMix64` and `Xoshiro256StarStar` in `core/src/Bloomlings.Core/Random/SplitMix64.cs` and
  `Xoshiro256StarStar.cs` (R3). Use only integer arithmetic and no `System.Random`. Add known-answer tests from the
  reference implementations in `core/tests/Bloomlings.Core.Tests/Random/PrngVectorTests.cs`.
- [X] T018 [P] Implement the Zobrist keys and incremental hasher in `core/src/Bloomlings.Core/Hashing/ZobristKeys.cs`
  and `StateHasher.cs`:
  - keys are generated with SplitMix64 from the fixed seed `0xB100B100B100B100`;
  - they cover the cell layer stack and open state, pod remaining and location, tray exposure, slots, collected keys
    and special states;
  - XOR updates are applied incrementally.
- [X] T019 Implement `core/src/Bloomlings.Core/Board/Board.cs` and `BoardBuilder.cs` (depends on T013–T016). The
  builder expands a `LevelDefinition` plus `BasePicture` exactly as data-model §1.3 *Derived board* describes:
  1. `cell(x,y)` starts as `grid[y][x']`, where `x' = mirror ? width-1-x : x`;
  2. the top-layer variant is `mapping[role]`;
  3. the overlays `layersBelow`, `stone`, `hole`, `mystery` and `keyId` are applied.

  The builder throws `InvalidLevelException` in these cases:
  - a mapping variant's colorGroup differs from the role's colorGroup;
  - a picture's `review.status` is not approved;
  - the board exceeds 14×16.
- [X] T020 Implement `core/src/Bloomlings.Core/Board/Reachability.cs` (depends on T014, T019):
  - a BFS from all Garden Entries over open cells, 4-neighbourhood only;
  - the route distance of a target is the number of BFS steps to the adjacent open cell, plus 1;
  - it returns the reachable top layers sorted by the T014 comparer, plus each target's route (the list of cells) for
    `TileCleared.routeFromEntry`;
  - stones are never walkable (FR-010, FR-032).
- [X] T021 [P] Write board tests in `core/tests/Bloomlings.Core.Tests/Board/BoardBuilderTests.cs` and
  `ReachabilityTests.cs`. They cover:
  - mirroring, and mapping onto the top layer;
  - overlays;
  - a colorGroup mismatch is rejected;
  - diagonal contact is not reachable;
  - stones block routes;
  - two entries;
  - tie-break ordering.
- [X] T022 [P] Implement JSON serialization with Newtonsoft in `core/src/Bloomlings.Content/Json/DefinitionJson.cs` and
  `BasePictureJson.cs`:
  - camelCase property names exactly as in the schemas;
  - the uint64 `seed` is a decimal string;
  - a round-trip must be byte-stable, with sorted keys and no trailing whitespace.

  Embed the four schemas from `specs/001-core-game-mvp/contracts/*.schema.json` as resources under
  `core/src/Bloomlings.Content/Schemas/` unchanged (the contract already requires 2–6 stacks).
- [X] T023 [P] Implement pack reading in `core/src/Bloomlings.Content/Packs/LevelPackReader.cs`,
  `PicturePackReader.cs` and `ContentManifest.cs`:
  - read gzip JSON-Lines packs;
  - parse `content-manifest.v1`, including `shuffleNodeBudget`;
  - verify each pack's SHA-256 and byte length (R5, R6);
  - support a loose-folder "dev" mode with one JSON file per level and picture, used until `publish` exists (US3).
- [X] T024 [P] Write content tests in `core/tests/Bloomlings.Content.Tests/DefinitionJsonTests.cs` and
  `PackReaderTests.cs`:
  - a schema-valid sample round-trips unchanged;
  - an invalid document is rejected;
  - a pack with a wrong hash is rejected.
- [ ] T025 Create the client composition root:
  - `client/Assets/Bloomlings/App/Boot.cs` and `AppServices.cs`: a simple service registry, with no third-party DI;
  - scene `client/Assets/Bloomlings/Scenes/Boot.unity`, which loads services and then routes through `GameFlow`
    (stubbed until US2).

  Status: `Boot.cs`, `AppServices.cs` and the `GameFlow` stub are written. The scene is generated by the editor menu
  "Tools/Bloomlings/Create Boot Scene" (`client/Assets/Bloomlings/Editor/SceneSetupMenu.cs`). Run it once in the
  Editor after T008, then check this task.
- [X] T026 [P] Implement `client/Assets/Bloomlings/Services/Content/BundledContentLoader.cs`:
  - reads `Application.streamingAssetsPath/content/manifest.json` and the packs through `Bloomlings.Content` (T023);
  - falls back to `StreamingAssets/content/dev/` loose files in development builds;
  - on Android, reads through `UnityWebRequest`.
- [X] T027 [P] Implement `client/Assets/Bloomlings/Services/Clock/IClock.cs` and `SystemClock.cs`: current UTC time and
  UTC date, injectable for tests. Daily features and ad caps use it. Also define
  `client/Assets/Bloomlings/Services/Config/IRemoteConfigService.cs` and `BundledRemoteConfigService.cs`: typed getters
  for the keys in `contracts/backend-services.md`, bundled defaults and range clamping, with no network access. US3
  (T092) uses it; T126 adds the UGS implementation.
  Status: `UgsRemoteConfigService` reads through `BundledRemoteConfigService`, so values fall back and clamp. The UGS
  fetch lives in the `Bloomlings.Integrations.Ugs` assembly, compiled only with the package installed. A client test
  keeps `backend/remote-config/defaults.json` in step with `RemoteConfigKeys`. The UGS code is not yet compiled against
  the SDK.

**Checkpoint**: The foundation is ready. A board can be built from a definition and a picture, reachability is
computed deterministically, and content loads in Unity.

---

## Phase 3: User Story 1 - Play a level: commit pods, restore the picture, avoid the jam (Priority: P1) 🎯 MVP

**Goal**: One level is fully playable in Unity:

- the player taps exposed pods into 5 slots;
- Bloomlings clear reachable exact-variant tiles, and the finished picture shows through;
- partial pods wait;
- win, jam and stuck are detected;
- Restart rebuilds the level identically.

**Independent Test**: Play a curated level with 4 variants, including two from the same family (for example Leaf and
Moss), from start to win. Then replay it in a deliberately bad order until it jams. Quickstart §1 and §5 steps 1–6
must pass.

### Tests for User Story 1 ⚠️ (write first; they must fail before implementation)

- [X] T028 [P] [US1] Write rule tests in `core/tests/Bloomlings.Core.Tests/Rules/CoreLoopTests.cs`, covering the
  doc 01 §20 pre-lock tests that do not need later mechanics:
  - two variants of the same family are active simultaneously;
  - a Leaf pod does not clear a reachable Moss tile;
  - exact per-variant accounting holds;
  - partial completion: `Moss ×20` with 12 reachable tiles becomes ×8, waits, then resumes automatically;
  - same-variant priority goes to the oldest slot, and the younger pod gets only the leftovers;
  - a full buffer where a pod is still progressing is not a jam;
  - a true jam;
  - a win takes precedence over a simultaneous jam;
  - `TapPod` is rejected with `NoFreeSlot` or `NotExposed`, with no state change;
  - the slot frees immediately at count 0.
- [X] T029 [P] [US1] Write FsCheck properties in `core/tests/Bloomlings.Core.Tests/Properties/InvariantProperties.cs`.
  The generator creates random small pictures (7×8), random mappings and exact-accounting pods, then random legal
  command sequences. The properties are:
  - pod counts are never negative;
  - after every settle, the remaining pod count per variant equals the remaining layers per variant;
  - applying the same commands to two fresh sessions gives equal `StateHash`;
  - `Won` and `Jammed` are never both true.
- [X] T030 [P] [US1] Create the golden replay harness in `core/tests/Bloomlings.Core.Tests/Golden/GoldenReplayTests.cs`
  and describe the format in `core/tests/golden/README.md`. Each case is a `*.golden.json` file holding:
  - `definition`;
  - `picture`;
  - `commands[]`;
  - `shuffleNodeBudget`;
  - `expectedEventsDigest`;
  - `expectedStateHash`;
  - `expectedStatus`.

  The harness fails when any value differs. It also offers a `--regenerate` switch through an environment variable
  `BLOOMLINGS_GOLDEN_REGEN=1`, which is used only when a rules change is intended and reviewed.

### Implementation for User Story 1

- [X] T031 [P] [US1] Implement commands, events and results in
  `core/src/Bloomlings.Core/Simulation/Commands.cs`, `Events.cs`, `CommandResult.cs`, `RejectReason.cs` and
  `LevelStatus.cs`. They follow the tables in `contracts/simulation-api.md`, and every event carries `Round`.
- [X] T032 [P] [US1] Implement `core/src/Bloomlings.Core/Tray/SourceTray.cs` and `PodRuntime.cs`:
  - stacks hold pods top-first, and only the top pod is exposed (FR-011);
  - pod locations are `tray|slot|done|removed`;
  - the tray validates "2–6 stacks";
  - it exposes `Exposed()`, `Take(podId)` and `PushTop(stack, pod)`. `PushTop` is used by Return in US5.
- [X] T033 [P] [US1] Implement `core/src/Bloomlings.Core/Slots/WaitingSlots.cs`:
  - 5 slots by default (FR-015);
  - slot states `free|occupied|locked|absent`;
  - a newly committed pod takes the leftmost free usable slot (FR-014);
  - each commit stamps a monotonic `ageOrder` counter;
  - pods in slots cannot be reordered manually.
- [X] T034 [US1] Implement `core/src/Bloomlings.Core/Simulation/LevelState.cs` and `LevelSession.cs` with
  `Load(definition, picture, SessionOptions)`, `View`, `Status`, `StateHash`, `CommandLog` and `Check`. `Load` builds
  the board with `BoardBuilder` and verifies exact accounting. It throws `InvalidLevelException` when "for every
  variant `v`, the sum of `count` over pods of `v` equals the number of top layers of `v` plus the number of hidden
  layers of `v`" does not hold (FR-023). Depends on T019, T031–T033.
- [X] T035 [US1] Implement the settle loop in `core/src/Bloomlings.Core/Simulation/Settler.cs`, following research R3:
  1. compute reachability (T020);
  2. build each active pod's candidates of its exact variant;
  3. allocate in slot-age order, where each pod claims `min(remaining, unclaimed)`;
  4. apply all claims at once, emitting `TileCleared` and `CellOpened`;
  5. remove pods whose count is 0, emitting `PodCompleted` and `SlotFreed`.

  Repeat until a round claims nothing. Add extension points `IRoundHook` for layers, keys, specials and mystery
  (implemented in US4).
- [X] T036 [US1] Implement `core/src/Bloomlings.Core/Simulation/StatusEvaluator.cs`. After each fixpoint it checks, in
  this order:
  1. Won: all required layers are cleared and the mandatory specials are resolved (FR-025).
  2. Jammed: every usable slot is occupied, no active pod has a candidate, and nothing is pending (FR-026).
  3. Stuck: no pod can progress, no tray pod is committable, and work remains.

  Emit `LevelWon`, `LevelJammed` or `LevelStuck`.
- [X] T037 [US1] Implement `Apply` for `TapPod` and `Restart` in `core/src/Bloomlings.Core/Simulation/LevelSession.cs`:
  - validate first with `NotExposed`, `NoFreeSlot` and `LevelNotPlaying`;
  - on commit, emit `PodCommitted` and then settle;
  - update `StateHash` incrementally (T018);
  - append the command to `CommandLog`;
  - `Restart` rebuilds from the same definition (FR-028).
- [X] T038 [US1] Author development content:
  - three base pictures, `content/pictures/lib/dev_tulip_pot.json`, `dev_mushroom.json` and `dev_watering_can.json`,
    each 8×9 with 3–4 roles and `review.status=approved` for dev use only;
  - three levels, `content/curated/dev/level-dev-001.json` to `level-dev-003.json`, with one using Leaf and Moss
    together.

  Add them as golden cases `core/tests/golden/dev-001-win.golden.json`, `dev-001-jam.golden.json` and
  `dev-002-siblings.golden.json`, and fill in the expected hashes after reviewing the event logs.
- [ ] T039 [P] [US1] Create the variant visuals:
  - `client/Assets/Bloomlings/Art/Variants/VariantVisualCatalog.cs`, a ScriptableObject holding per `VariantId`: color,
    icon sprite, tile sprite, pod skin and family;
  - `client/Assets/Bloomlings/Art/Variants/VariantVisuals.asset`, filled with placeholder flat art for the 8 launch
    variants. The icons are leaf, moss tuft, flower, bud, drop/wave, dew droplet, stump/log and acorn;
  - hue alone must never carry meaning (FR-005, FR-072).

  Status: `VariantVisualCatalog.cs` is done and falls back to procedural placeholder icons (a distinct shape per
  variant). The asset is generated by "Tools/Bloomlings/Create Variant Visuals Asset"; check this task after running
  it in the Editor.
- [ ] T040 [P] [US1] Create placeholder family worker prefabs:
  - `client/Assets/Bloomlings/Art/Families/Sprig.prefab`, `Bloom.prefab`, `Drop.prefab` and `Twig.prefab`;
  - an Animator with the states idle, emerge, move, restore and despawn;
  - variant tint and accent applied at runtime (doc 12 §2).

  Status: worker visuals are procedural for now. `BloomlingWorker` drives the idle, emerge, move, restore and
  despawn states in code with the family silhouette and variant tint. The prefabs with an Animator come with the
  final family art.
- [X] T041 [US1] Implement `client/Assets/Bloomlings/Gameplay/Board/BoardView.cs` and `TileView.cs`:
  - fit the board of up to 14×16 to the board area without scrolling or zooming (FR-008);
  - an active tile is a framed tile with the variant color and icon;
  - an open cell hides the tile and shows the finished picture (T042);
  - render the Garden Entry markers on the board edge;
  - character faces never appear on tiles.
- [X] T042 [US1] Implement `client/Assets/Bloomlings/Gameplay/Board/FinishedPictureRenderer.cs`:
  - automatic finished look (R7, FR-007): flat cells with rounded merged regions per role, a soft light version of
    the mapped variant color, and no symbols;
  - on `CellOpened`, reveal that cell;
  - on `LevelWon`, reveal the full picture;
  - restored cells must stay clearly distinct from active tiles (SC-003).
- [X] T043 [P] [US1] Implement `client/Assets/Bloomlings/Gameplay/Tray/TrayView.cs` and `PodView.cs`:
  - stacks with the exposed pod highlighted;
  - each pod shows the variant icon, the variant color, the count and the family silhouette, in that order of
    prominence (FR-012);
  - taps are forwarded to `GameplayController`.
- [X] T044 [P] [US1] Implement `client/Assets/Bloomlings/Gameplay/Slots/SlotRowView.cs`:
  - 5 slots, plus an optional sixth added later by US5;
  - each shows the pod variant, remaining count and waiting/active/stuck state;
  - a jam-risk highlight appears when only one usable slot is free (FR-070).
- [X] T045 [US1] Implement `client/Assets/Bloomlings/Gameplay/Timeline/EventTimeline.cs` (R4):
  - consume the event logs as waves, one per round;
  - scale playback by 2× speed;
  - compress a backlog above `fx.backlogThresholdMs` (default 1500) by speeding up to 4× and merging walkers;
  - never block input.
- [X] T046 [US1] Implement `client/Assets/Bloomlings/Gameplay/Workers/WorkerPool.cs` and `BloomlingWorker.cs`:
  - a pooled set of family prefabs, capped at 60 active on low-end devices;
  - workers walk `routeFromEntry` from the Garden Entry, play restore, then despawn;
  - batches are aggregated when the pool is saturated;
  - characters never hide tile state (doc 12 §8).
- [X] T047 [US1] Implement `client/Assets/Bloomlings/Gameplay/GameplayController.cs`:
  - owns the `LevelSession`;
  - on tap, calls `Check` then `Apply`;
  - shows tap feedback within 0.1 s, even while the timeline is behind (SC-008);
  - feeds events to `EventTimeline`;
  - routes Won, Jammed and Stuck to their screens;
  - pauses on `OnApplicationPause(true)` and resumes exactly;
  - keeps no mid-level save, so a killed app restarts the level.
- [X] T048 [P] [US1] Implement `client/Assets/Bloomlings/UI/Screens/GameplayHud.cs` (FR-068):
  - top bar: Pause, Level N and a 2× speed toggle;
  - the board in the center;
  - the Garden Entry and slots below the board;
  - the tray at the bottom;
  - no goals panel.
- [X] T049 [P] [US1] Implement `client/Assets/Bloomlings/UI/Screens/PauseScreen.cs` with Resume, Restart and Leave,
  all free of penalty (FR-030, FR-040).
- [X] T050 [P] [US1] Implement `client/Assets/Bloomlings/UI/Screens/WinScreen.cs`: full reveal of the finished picture,
  then a reward panel (placeholder until US5), then Next.
- [X] T051 [P] [US1] Implement `client/Assets/Bloomlings/UI/Screens/JamScreen.cs`. The board stays visible behind it
  (FR-027). It offers a recovery-options list, empty until US5 and US6, and Restart. It never opens the Store.
- [ ] T052 [US1] Create the gameplay scene `client/Assets/Bloomlings/Scenes/Gameplay.unity`, wiring T041–T051. Add
  the editor-only menu `client/Assets/Bloomlings/Editor/DevLevelMenu.cs` ("Tools/Bloomlings/Play Dev Level") to play
  the T038 levels.

  Status: `DevLevelMenu.cs` is done. The scene is generated by "Tools/Bloomlings/Create Gameplay Scene" (also run
  automatically on first use of Play Dev Level). Check this task after running it in the Editor.
- [X] T053 [US1] Write `client/Assets/Bloomlings/Tests/EditMode/GoldenReplayEditModeTests.cs`. It runs every
  `core/tests/golden/*.golden.json` through the Unity-compiled `com.bloomlings.core` and asserts the same hashes
  (SC-005).

**Checkpoint**: US1 is playable and deterministic. This is the MVP.

---

## Phase 4: User Story 2 - Endless linear progression: Home → Play → Level N → Next (Priority: P1)

**Goal**:

- the first launch goes straight to Level 1 with a guided tap;
- later launches open Home with Level N and Play;
- a win leads to Next and Level N+1;
- difficulty labels and roadmap unlocks fire;
- progress persists;
- Level N is identical for everyone.

**Independent Test**: A new player completes Levels 1–10 without help and meets every roadmap unlock. They restart
the app and continue from Level 11. A second device shows the same Level 11 board (SC-011).

### Tests for User Story 2 ⚠️

- [X] T054 [P] [US2] Write `client/Assets/Bloomlings/Tests/EditMode/SaveServiceTests.cs`:
  - the atomic write survives a simulated crash between the temp write and the rename;
  - a corrupt main file falls back to the backup;
  - the `schemaVersion` migration hook runs;
  - "Petals and charges are never negative";
  - "`claimed` and `ledger.transactionId` are unique".
- [X] T055 [P] [US2] Write `client/Assets/Bloomlings/Tests/EditMode/ProgressionServiceTests.cs`:
  - a win at N sets `highestCompletedLevel=N` and the current level to N+1;
  - unlock events fire exactly at the roadmap levels;
  - a demo is shown within 0–2 levels of its unlock (FR-031).
- [X] T056 [P] [US2] Add golden cases for the curated Levels 1–10 in `core/tests/golden/curated-l0001.golden.json` to
  `curated-l0010.golden.json`, each with a winning command log. They prove that the same definition gives the same
  result everywhere (SC-011).

### Implementation for User Story 2

- [X] T057 [P] [US2] Create the unlock roadmap table:
  - `content/roadmap/unlock-roadmap.json` mirrors the spec's Unlock Roadmap. Each entry is `{level, unlockId, kind:
    system|booster|mechanic|variant|profile, demoWithin: 0–2}`.
  - The pure loader `core/src/Bloomlings.Core/Progression/UnlockRoadmap.cs` is shared by the client and by pipeline
    validation (FR-031).
  - Defaults: the Key unlocks at L8 (`mechanic.key`), Stones at L11 (`mechanic.stone`). If the Mystery Pod takes L8,
    `mechanic.key` moves to L14.
- [X] T058 [P] [US2] Implement `client/Assets/Bloomlings/Services/Save/PlayerSave.cs` and `SaveSerializer.cs`,
  mirroring `contracts/player-save.schema.json` (data-model §3.1) and using Newtonsoft.
- [X] T059 [US2] Implement `client/Assets/Bloomlings/Services/Save/SaveService.cs` (R15):
  - the save lives at `Application.persistentDataPath/save/player_save_v1.json`;
  - writes are atomic (temp file plus rename) and keep a `.bak`, with a checksum field;
  - it saves after a win, a purchase, a claim, a settings change or a booster use, and never mid-level.
- [X] T060 [US2] Implement `client/Assets/Bloomlings/App/Progression/ProgressionService.cs`:
  - tracks `highestCompletedLevel` and the current level (= highest + 1);
  - on a win, advances and saves;
  - raises `UnlockReached(unlockId)` from T057;
  - tracks `demosSeen`.

  Also add the editor-only menu `client/Assets/Bloomlings/Editor/FastProgressMenu.cs` ("Tools/Bloomlings/Fast
  Progress"). It sets the highest completed level to a chosen N and fires every unlock passed on the way. Quickstart
  §5 step 7 uses it.
- [X] T061 [US2] Implement `client/Assets/Bloomlings/Services/Content/CatalogService.cs`. It maps a level number to
  its `LevelDefinition` and `BasePicture` through `BundledContentLoader`. The content version used for an attempt is
  pinned until that attempt ends (R6).
- [X] T062 [US2] Implement `client/Assets/Bloomlings/App/GameFlow.cs`:
  - on the first launch (no save), start Level 1 directly with no menus or sign-in (FR-045);
  - on later launches, open Home (FR-046);
  - on a win, go to WinScreen, then Next, then Level N+1;
  - there is no level map or chooser (FR-057).
- [X] T063 [P] [US2] Implement `client/Assets/Bloomlings/UI/Screens/HomeScreen.cs` (FR-058):
  - logo, Level N, a single Play/Continue button and Petals;
  - Settings;
  - the Store button, hidden until L12;
  - the milestone teaser, e.g. "Level 100 reward in 12";
  - a leaderboard rank slot, hidden until L10.
- [X] T064 [P] [US2] Implement `client/Assets/Bloomlings/UI/Tutorial/DemoOverlay.cs` and `DemoScript.cs`:
  - a pointer hand, at most one short message, skippable;
  - the Level 1 guided first tap (FR-045);
  - the same-family sibling demo "Match the exact symbol", which shows the Leaf pod ignoring a Moss tile, shown once
    (FR-071).
- [X] T065 [P] [US2] Implement `client/Assets/Bloomlings/UI/Screens/DifficultyBanner.cs`. Before a level starts it
  shows Hard (from L5) or Super Hard (from L10) with a distinct color and icon treatment (FR-059).
- [X] T066 [P] [US2] Implement `client/Assets/Bloomlings/UI/Screens/SettingsScreen.cs` with toggles for music, sound
  effects, haptics and the 2× default, persisted in the save. The Restore Purchases button is wired in US6.
- [ ] T067 [US2] Author the curated tutorial Levels 1–10 as `content/curated/level-0001.json` to `level-0010.json`,
  with approved base pictures `content/pictures/lib/*.json`, following the roadmap:
  - L1 uses 2 variants and L2 adds a third;
  - L3, L4, L6 and L9 are easy levels suited to the booster demos;
  - L5 is the first Hard level;
  - L10 is the first Super Hard level.

  Run `validate` on them, have a person playtest each level (FR-084), then copy the accepted definitions and
  validation records into `content/catalog/` (constitution IV: hand-curated levels pass the same validation).

  Each level loads through `LevelSession.Load` (exact accounting) and uses a different base picture (FR-083).


  Status: Levels 1–10 and their 10 pictures are authored. Every tap order was searched exhaustively: all levels are
  winnable without boosters; L5 (Hard) jams in 17% of orders and L10 (Super Hard) in 27%. Their winning logs are the
  T056 golden cases, and `ContentFolderTests` checks the band rules. `validate --defs content/curated` passes (0
  errors; the warnings are the provisional readability pairs). Still open: the person playtest (FR-084) and the copy
  with validation records into `content/catalog/`. The L8 Key showcase follows with US4 (T111).

**Checkpoint**: US1 and US2 give a playable vertical slice from first launch to Level 10.

---

## Phase 5: User Story 3 - 5000+ deterministic levels built from a validated catalog (Priority: P2)

**Goal**: The picture library, solver, generator, validation, scoring, review, publishing and content updates. These
produce and certify a versioned catalog, and CI gates it.

**Independent Test**:

1. Generate a band.
2. Validate it; invalid candidates are rejected with reasons.
3. Publish it.
4. Check that published levels build identically on two devices.
5. Change the content version; shipped levels do not change silently.

Quickstart §2–§4 must pass.

### Tests for User Story 3 ⚠️

- [X] T068 [P] [US3] Write `core/tests/Bloomlings.Solver.Tests/SolverTests.cs`:
  - hand-built fixtures that are solvable and unsolvable;
  - a jam witness exists for a non-tutorial fixture;
  - an exhausted node budget returns `unknown`;
  - the result is identical across runs.
- [X] T069 [P] [US3] Write `core/tests/Bloomlings.Generator.Tests/GeneratorTests.cs`:
  - the same profile and seed give byte-identical definitions;
  - every accepted level passes `CatalogValidator`;
  - every rejected candidate records a reason;
  - the visible top layer always follows the picture mapping.
- [X] T070 [P] [US3] Write `core/tests/Bloomlings.Content.Tests/PackWriterTests.cs`. Write and then read back 600
  definitions: they fill 3 packs of 250, 250 and 100. The gzip output is byte-stable across runs (mtime 0), and the
  manifest hashes match.

### Implementation for User Story 3

- [X] T071 [P] [US3] Implement `core/src/Bloomlings.Content/Packs/LevelPackWriter.cs` and `ManifestWriter.cs`:
  - write packs of 250 levels in gzip JSON-Lines with a deterministic order;
  - also write the picture pack and the daily pack;
  - write `content-manifest.v1` with `sha256`, `bytes`, `contentVersion`, `minAppVersion`, `pictureLibraryVersion`
    and `shuffleNodeBudget` (R5, R10).
- [X] T072 [P] [US3] Implement `core/src/Bloomlings.Pipeline/Pictures/IndexedPngReader.cs`. It reads PNGs with color
  type 3, bit depth 1/2/4/8 and no interlacing, using `System.IO.Compression.ZLibStream`. A palette index `i` below the
  role count maps to role `i`; `roles.length` maps to EMPTY and `roles.length+1` maps to STONE.
  Also implement `TextGridReader.cs` for `.grid.txt` files: one character per cell with the top row first, mapped
  through a `legend` in the sidecar where `.` means EMPTY and `#` means STONE.
- [X] T073 [US3] Implement `core/src/Bloomlings.Pipeline/Pictures/PictureImporter.cs` and `StructureMetrics.cs`:
  - read `content/pictures/src/<id>.png|.grid.txt` plus the sidecar `<id>.meta.json`, which is base-picture.v1
    without `grid` and `structure`, plus an optional `legend`;
  - write `content/pictures/lib/<id>.json`;
  - compute `regionCount`, `nestingDepth` (relative to a bottom-center entry) and `backgroundShare` (R7).
- [X] T074 [US3] Implement `core/src/Bloomlings.Pipeline/Pictures/PictureValidator.cs`:
  - validate against the embedded `base-picture.schema.json` with JsonSchema.Net;
  - check "7 ≤ width ≤ 14, 8 ≤ height ≤ 16";
  - check "At least 2 roles; each role has exactly one color group";
  - a licence must be present;
  - flag pictures whose review is not `approved` as unusable (FR-084, FR-091).
- [X] T075 [US3] Implement a readability tool in `core/src/Bloomlings.Pipeline/Readability/ReadabilityCommand.cs`:
  - compute pairwise CIEDE2000 color distance for each variant pair, under normal vision and simulated
    protanopia/deuteranopia/tritanopia;
  - add a grayscale-contrast check;
  - write candidates to `content/readability/pairs-report.json`.

  A human then records approved pairs in `content/readability/approved-pairs.json`, which FR-005 validation consumes.

  Status: the tool is done and the palette was tuned with it (launch variants: minimum CIEDE2000 20 across normal
  vision and the three simulations; all 12 variants: 10.7). The grayscale check is advisory because 8 hues cannot all
  differ in lightness. `approved-pairs.json` holds the candidates with status `provisional`, so the pipeline works and
  `validate` warns on every level until a person runs the in-game readability tests and sets it to `approved`.
- [X] T076 [US3] Implement the search engine in `core/src/Bloomlings.Core/Search/StateSearch.cs` and
  `TranspositionTable.cs`. It lives in Core so that Shuffle can use it (R10). It provides:
  - DFS over settled states from `LevelSession` clones;
  - a transposition table keyed by `StateHash`;
  - move ordering: progressable pods first;
  - symmetry pruning of identical exposed pods (same variant, count and modifiers, and the same pods below);
  - a node-count budget that is never time-based.
- [X] T077 [US3] Implement `core/src/Bloomlings.Solver/Solver.cs` as `ISolver.Solve` and `FindJam`, following
  `contracts/simulation-api.md` (R8). It returns `solvable|unsolvable|unknown`, a winning trace and a jam witness.
- [X] T078 [US3] Implement `core/src/Bloomlings.Solver/Metrics.cs` and `DifficultyScorer.cs`:
  - metrics: dependency depth, branching, unsafe-choice density, dead-end depth, peak and mean buffer, connected
    commitments, variant load (count, siblings, similarity, cross-variant layers), special load, total work and
    estimated duration;
  - score and class thresholds come from `content/profiles/difficulty-thresholds.json` (FR-082);
  - the score is an integer fixed-point value (× 1000) with integer thresholds, so generation is reproducible across
    machines;
  - a manual override flag is supported.
  Status: the metrics live in `LevelMetrics.cs`. The metrics walk is capped at `Solver.MetricsNodeCap` nodes, so a
  level's metrics and score are the same under any solve budget.
- [X] T079 [P] [US3] Implement `core/src/Bloomlings.Content/Validation/ValidationRecord.cs`, following data-model §1.4:
  `result`, `solutionTrace`, `jamWitness`, `playerInfoFair`, `metrics`, `checks[]`, `solverVersion`, `nodeBudget` and
  `nodesUsed`.
- [X] T080 [US3] Implement `core/src/Bloomlings.Pipeline/Validation/CatalogValidator.cs`. It checks every FR-080
  invariant:
  - solvable without boosters, with a stored trace;
  - accounting;
  - reachability of every mandatory layer and key;
  - no mechanic or variant before its unlock (T057 table);
  - no hidden-information fail (from US4 once mystery exists);
  - readable pairs (T075).

  It also checks:
  - FR-081: losable, with a jam witness, unless the level is a tutorial;
  - FR-004: variant count within the band range;
  - FR-008: board limits;
  - FR-083 similarity: Levels 1–100 use distinct pictures, the same base picture never appears within 50
    consecutive levels, and a reuse differs in mapping or mirroring **and** in Source design;
  - FR-083 sequences: no 3 consecutive levels share the same active variant set or the same set of mechanics, and
    no Source layout signature (stack count plus ordered pod counts per stack) repeats within 50 levels;
  - the data-model rules: keys and locks pair 1:1, at most 1 locked slot and only from L80, layer depth ≤ 2 before
    L125 and ≤ 3 after, 2–6 stacks, and connected members at the same depth.
- [X] T081 [US3] Implement the CLI in `core/src/Bloomlings.Pipeline/Program.cs` and `Commands/*.cs`, following
  `contracts/pipeline-cli.md`:
  - commands: `pictures import`, `pictures validate`, `readability`, `solve`, `validate` (`--changed-only` uses
    `git diff --name-only origin/main`), `score`, `replay`, `diff`, `publish`;
  - exit codes 0/1/2 and `--json` output;
  - `score` reports the per-100-level class counts from L11 (Hard 15–25, Super Hard 6–10) and checks that the level
    after a Super Hard is Normal (FR-059).
- [X] T082 [US3] Implement `core/src/Bloomlings.Pipeline/Review/ReviewRenderer.cs` and `PngWriter.cs`. For each level
  it renders a board PNG (variant colors plus icon letters) and a finished-picture PNG. It builds `index.html` with
  metrics and flags for manual QA tiers (FR-084), for the `review` command.
- [X] T083 [P] [US3] Implement `core/src/Bloomlings.Generator/Profiles/GenerationProfile.cs` and
  `ProfileLoader.cs`, with the fields from data-model §1.5.
- [X] T084 [US3] Implement `core/src/Bloomlings.Generator/PicturePicker.cs`. It picks from approved pictures by tags,
  size range and structure targets, and skips pictures blocked by the FR-083 windows (R9 step 1).
- [X] T085 [US3] Implement `core/src/Bloomlings.Generator/RoleMapper.cs`, which enumerates role→variant mappings
  (R9 step 2):
  - the mapped variant must be in the same colorGroup as the role;
  - it must be allowed in the band;
  - similar roles may be merged when the band allows fewer variants;
  - only approved readability pairs are used.
- [X] T086 [US3] Implement `core/src/Bloomlings.Generator/EntryPlanner.cs` and `SolutionPlanner.cs`. The first chooses
  the entry placement and mirroring toward the structure target. The second simulates a planned good-play policy on
  `LevelSession` and records the order in which each variant's work becomes reachable (R9 steps 3 and 5).
- [X] T087 [US3] Implement `core/src/Bloomlings.Generator/PodPartitioner.cs`. It splits each variant's demand along
  the planned waves into pods within the size classes: small 5–15, medium 16–40, large 41–100, exceptional 100+
  (R9 step 6, doc 05 §12).
- [X] T088 [US3] Implement `core/src/Bloomlings.Generator/TrayBuilder.cs`:
  - place the planned pods round-robin across the stacks;
  - inject difficulty: tempting premature pods and buried needs;
  - re-solve with `Solver` after each injection until `DifficultyScorer` reaches the target class (R9 step 7).
- [X] T089 [US3] Implement the orchestrator `core/src/Bloomlings.Generator/Generator.cs`, which runs R9 steps 1–9 with
  a seeded PRNG and records a rejection reason for each discarded candidate. Add the `generate` and `daily generate`
  CLI commands in `core/src/Bloomlings.Pipeline/Commands/GenerateCommand.cs` and `DailyGenerateCommand.cs`.
  Status: the orchestrator is `core/src/Bloomlings.Generator/LevelGenerator.cs`. Tray tuning searches with a
  10k-node budget (a trace found within it is the one the full budget finds). `generate --history <batch>` chains
  unpublished preview batches for the FR-083 windows, and `--allow-draft` generates previews from draft pictures.
- [X] T090 [US3] Create the generation profiles `content/profiles/band-0011-0025.json`, `band-0026-0050.json`,
  `band-0051-0100.json`, `band-0101-0250.json`, `band-0251-0500.json`, `band-0501-1000.json`,
  `band-1001-2000.json`, `band-2001-5000.json` and `daily.json`, plus `content/profiles/difficulty-thresholds.json`.
  Take the values from the spec's Level Band Guidelines:
  - board size, active variants, Source Pods, work and duration;
  - buffer-pressure targets;
  - `allowedMechanics` from the unlock roadmap.
  Status: bands 11–25, 26–50 and 51–100 were calibrated by trial generation on the draft library (Normal levels
  score about 950–1600, tuned levels reach about 3000; pod counts sit in the upper half of the spec ranges so levels
  can jam). Later bands are extrapolated. Recalibrate the weights and thresholds after playtests.
- [X] T091 [US3] Implement the editor menu `client/Assets/Bloomlings/Editor/ImportContentMenu.cs` ("Tools/Bloomlings/
  Import Published Content"). It copies `build/content/` from `publish` into `client/Assets/StreamingAssets/content/`
  and removes the dev folder from release builds (FR-078).
- [X] T092 [US3] Implement `client/Assets/Bloomlings/Services/Content/IContentUpdateService.cs` and
  `ContentUpdateService.cs` (R6):
  - read the remote manifest URL from `IRemoteConfigService` (`content.manifestUrl`, default empty, which skips the
    update);
  - download packs over HTTPS and verify their SHA-256;
  - activate atomically;
  - never change the definition of an attempt in progress;
  - raise analytics hooks `content_update` and `content_error`.
  Status: `ContentCache` (atomic install, verified loads) and `ContentUpdater` (decisions) are engine-free and tested
  under .NET (`ContentUpdateTests`). Boot starts from a newer valid cached version, and the update check runs after
  the game is playable. The analytics hooks are an event that the T147 analytics service subscribes to.
- [X] T093 [P] [US3] Create the CI workflows:
  - `.github/workflows/content-validate.yml` runs on pull requests: `pictures validate`, `validate --changed-only` and
    `diff --from main`.
  - `.github/workflows/catalog-nightly.yml` runs on a nightly cron: full `validate`, `score` and the similarity
    statistics, and uploads a JSON report artifact (SC-004, SC-012).
  Status: `score` also reports the SC-012 similarity statistics (distinct pictures up to L100, the smallest reuse
  gap, repeats within 50 levels).
- [X] T094 [US3] Seed the picture library for Levels 11–100 with at least 90 base pictures under
  `content/pictures/src/`, as `.grid.txt` or indexed PNG plus `.meta.json`. They show garden-world subjects:
  flowers, fruit, insects, small animals, garden tools and cozy objects. Import them with `pictures import`. Status
  stays `draft` until a person approves each picture for recognizability (FR-084, SC-015).
  Status: 94 draft pictures (27 subjects in up to four variations: 16 for 9×10–10×10, 26 for 10×10–12×12, 52 for
  10×12–14×14) are sketched procedurally by `content/pictures/tools/sketch_pictures.py` and imported (`pictures
  validate`: 0 errors). They are placeholders for an artist to refine. A person still has to approve each picture
  for recognizability (FR-084, SC-015); until then `validate` fails every generated level on `picture-approved`.
- [ ] T095 [US3] Produce the curated Levels 11–100:
  - run `generate` with the band-0011-0025, band-0026-0050 and band-0051-0100 profiles, using only the mechanics
    already implemented;
  - have a person playtest every level (the FR-084 tier for 1–100);
  - commit accepted definitions and validation records to `content/catalog/`.

  Levels using US4 mechanics are regenerated after US4.

  Status: open (human steps). A preview of Levels 11–100 was generated from the draft pictures with
  `generate --allow-draft` into `content/work/` (gitignored). It passes every `validate` check except
  `picture-approved`. Still needed: picture approval (T094), the readability sign-off (T075), a person playtest of
  every level (FR-084), then regeneration with approved pictures and the commit into `content/catalog/`. For
  playtest builds, `publish --allow-draft` packs draft pictures as marked previews.

**Checkpoint**: The pipeline produces and certifies catalogs, the client loads published packs, and CI gates content.

---

## Phase 6: User Story 4 - Mechanics expand on a planned roadmap (Priority: P3)

**Goal**: The mechanics appear in the rules core, the solver, the generator and the client, following the roadmap:

- stones and multiple entries;
- keys and locked pods;
- connected pods;
- layered tiles;
- the Garden Gate / heavy blocker;
- the Fountain;
- the locked waiting slot;
- mystery pods and tiles, which need the fairness solver.

**Independent Test**: For each mechanic, play its showcase level and one combination level, and check the US4
acceptance scenarios.

### Tests for User Story 4 ⚠️

- [X] T096 [P] [US4] Write `core/tests/Bloomlings.Core.Tests/Rules/MechanicsTests.cs`, covering the remaining doc 01
  §20 tests and the US4 scenarios:
  - a layered cross-family reveal (`Leaf → Violet Bud`) wakes an active Violet pod;
  - a key is collected when its supporting layer clears, with no extra work, and opens exactly one lock (pod, slot or
    special);
  - a locked pod is refused with `Locked` and buries the pods below it;
  - connected pods with mixed variants commit together, are refused with `NotEnoughSlotsForGroup` when slots are
    short, and act independently afterwards;
  - a stone is never walkable;
  - a gate and a Fountain trigger on their visible conditions;
  - a locked slot leaves 4 usable slots until its key is collected;
  - a mystery pod reveals its fixed variant on commit;
  - a mystery tile reveals when it becomes reachable.
  Status: `MechanicsTests` (17 cases) plus property tests whose random levels now carry mystery tiles, keys, a
  locked slot, a connected pair and a gate; the incremental hash matches the full recompute throughout.
- [X] T097 [P] [US4] Write `core/tests/Bloomlings.Solver.Tests/FairnessTests.cs`. One fixture needs a blind guess and
  must fail; another is deducible from the visible per-variant accounting and must pass. A mystery load above the cap
  is rejected.

### Implementation for User Story 4

- [X] T098 [US4] Implement layered tiles in `core/src/Bloomlings.Core/Mechanics/LayeredTiles.cs`, as an `IRoundHook`
  of T035:
  - clearing a top layer reveals the next one (`LayerRevealed`);
  - clearing the last layer opens the cell (`CellOpened`);
  - `LevelView` exposes the next layer's variant for the peek indicator (FR-036);
  - every layer counts in demand.
  Status: layers were already part of the board and the settle loop (`Board.ClearTopLayer`, `LayerRevealed`,
  `CellOpened`, `LevelView` peek), so no separate hook is needed; `MechanicsTests` covers the cross-family wake-up.
- [X] T099 [US4] Implement keys and locks in `core/src/Bloomlings.Core/Mechanics/KeysAndLocks.cs`:
  - a key is collected when its supporting top layer clears (`KeyCollected`);
  - the paired lock opens (`LockOpened`);
  - the lock target is a pod, a slot or a special;
  - "Exactly one lock per key and one key per lock" (FR-033, FR-034).
- [X] T100 [US4] Implement connected pods in `core/src/Bloomlings.Core/Tray/SourceTray.cs` and
  `Simulation/LevelSession.cs`:
  - tapping any member commits all members when they are all exposed at the same depth and there are enough free
    usable slots;
  - otherwise the tap is refused with `NotEnoughSlotsForGroup`;
  - the link ends after placement (FR-035).
- [X] T101 [US4] Implement specials in `core/src/Bloomlings.Core/Mechanics/Specials.cs`, following the schema's
  special kinds:
  - gate conditions `key`, `clear_count_adjacent` and `clear_region`;
  - the Fountain condition `clear_count_adjacent` on an exact variant;
  - the effects `open_cells`, `reveal_layers` and `remove_stones`;
  - the events `SpecialProgressed` and `SpecialTriggered` (FR-037, FR-038).
  Status: a Gate opens its own cells when it triggers, and a Fountain stays as a landmark. `open_cells` opens listed
  non-target cells, `remove_stones` opens listed stones, and `reveal_layers` reveals listed mystery tiles. An
  untriggered special blocks the win. A single-cell gate with a key condition counts as the Key mechanic
  (`LevelMechanics`): L14 is the "first simple Key tutorial" before keys unlock Source content at L16, while the Garden
  Gate with a counter is the L35 mechanic.
- [X] T102 [US4] Implement the locked waiting slot in `core/src/Bloomlings.Core/Slots/WaitingSlots.cs`: the slot is
  `locked` until its key is collected, then it becomes `free` (FR-039).
- [X] T103 [US4] Implement the mystery mechanics in `core/src/Bloomlings.Core/Mechanics/Mystery.cs`:
  - a mystery pod reveals its variant on commit (`MysteryPodRevealed`);
  - a mystery tile reveals its variant when it becomes reachable (`MysteryTileRevealed`);
  - hidden variants are fixed in the data and excluded from `LevelView` until revealed.
- [X] T104 [US4] Implement `core/src/Bloomlings.Solver/FairnessChecker.cs` (R8):
  - an AND-OR search over mystery reveals;
  - variant assignments must be consistent with the visible per-variant accounting;
  - the cap is at most 2 mystery pods plus 3 mystery tiles.
  Status: worlds are all assignments of the level's active variants to mystery pods and tiles that keep exact accounting
  (`LevelSession.LoadHypothesis`) and match the start the player sees. Observation classes are split by the event
  text. `CatalogValidator` writes `playerInfoFair` and fails unfair, over-cap or budget-exhausted levels.

  Wire it into `CatalogValidator` for `playerInfoFair`.
- [X] T105 [US4] Implement `core/src/Bloomlings.Generator/Overlays/OverlayPlanner.cs` (R9 step 4), which adds:
  - layers, within the band's depth limit;
  - keys and locks;
  - connected groups at the same depth;
  - gates, Fountains, locked slots and mystery elements.
  Status: `OverlayPlanner` chooses 0–2 unlocked mechanics per level (stones and holes on background cells, layers,
  mystery tiles, keys on early-wave tiles locking later-wave pods, a locked slot, key doors, gates and Fountains). A
  connected pair is added after tray tuning and kept only if the level stays winnable, losable and in class. The
  `generate --mechanics … --class normal` showcase mode forces a mechanic set, and `--keep content/showcase` keeps
  showcase levels fixed.

  It adds only mechanics allowed by the band and roadmap, and keeps the visible top layer following the picture.
- [X] T106 [P] [US4] Add the layer peek indicator and stone tile to `client/Assets/Bloomlings/Gameplay/Board/TileView.cs`.
  The peek is a small corner marker with the next variant's icon and color.
  Status (T106–T110): written and compiled against the Unity stubs, not yet run in the Editor. The peek badge shows
  the next variant's color and icon. `SpecialView` shows the condition (key, or the exact variant's icon) and the
  counter, and animates the trigger. `KeyView` flies the key to its pod, slot or special. The tray draws link lines;
  a mystery pod's slot card flips when revealed, and a locked slot pops its lock when its key lands.
- [X] T107 [P] [US4] Implement `client/Assets/Bloomlings/Gameplay/Board/KeyView.cs`. The key overlay must not hide the
  tile's icon or color. On collection it flies to its lock.
- [X] T108 [P] [US4] Implement `client/Assets/Bloomlings/Gameplay/Board/SpecialView.cs`. It shows the gate or hedge
  seal with its visible condition and counter, and the Fountain with its exact-variant condition. It plays the
  trigger animation.
- [X] T109 [P] [US4] Add pod states to `client/Assets/Bloomlings/Gameplay/Tray/PodView.cs` and `TrayView.cs`:
  - a lock badge, and a key highlight when a locked pod is tapped;
  - the connected link lines;
  - the mystery `? + count` with a flip reveal.
- [X] T110 [P] [US4] Add the locked-slot visual and its unlock animation to
  `client/Assets/Bloomlings/Gameplay/Slots/SlotRowView.cs`.
- [ ] T111 [US4] Author the showcase and practice levels for each mechanic at its roadmap level in `content/curated/`:
  - L8: Key preview (the Key unlocks here by default); a mystery pod takes L8 only once T104 passes, and the Key then
    unlocks at L14;
  - L11 Stones, L14 Key practice, L16 Locked Pod, L18 Connected Pair, L28 Layered;
  - L35 Gate, L60 Fountain, L80 Locked Slot, L90 Mystery Tile.

  Add a `DemoScript` entry per unlock in `client/Assets/Bloomlings/UI/Tutorial/Demos/`. Regenerate and re-curate the
  affected Levels 11–100 from T095. Validate every showcase level with `validate`, playtest it, and copy the accepted
  levels into `content/catalog/`.

  Status: open (human steps). L8 now carries the Key preview (a key door in the sky; its golden case was
  regenerated). The other showcases are generated in showcase mode into `content/showcase/` (see its README) and pass
  every check except `picture-approved`. `MechanicDemos` holds one demo per unlock, shown once when a level first
  uses an unlocked mechanic. Still needed: picture approval, playtests, then the copy into `content/catalog/`.
- [X] T112 [US4] Add a golden case per mechanic in `core/tests/golden/mech-*.golden.json`: layered, key, locked pod,
  connected, gate, Fountain, locked slot, mystery pod and mystery tile.
  Status: 10 cases (`mech-layered`, `mech-key`, `mech-locked-pod`, `mech-connected`, `mech-connected-refused`,
  `mech-gate`, `mech-fountain`, `mech-locked-slot`, `mech-mystery-pod`, `mech-mystery-tile`) run under .NET and in
  the client check.

**Checkpoint**: Every launch mechanic works end to end and is validated by the pipeline.

---

## Phase 7: User Story 5 - Boosters and Petals help recover from mistakes (Priority: P3)

**Goal**:

- four deterministic boosters: Extra Slot, Shuffle with its winnability guarantee, Return and Bloom Burst;
- the Petals economy;
- booster unlocks with free charges at L3, L4, L6 and L9;
- jam recovery.

**Independent Test**: Reach L9 and use each free charge. Earn Petals and buy a booster. Jam a level and recover it
with Extra Slot.

### Tests for User Story 5 ⚠️

- [X] T113 [P] [US5] Write `core/tests/Bloomlings.Core.Tests/Rules/BoosterTests.cs`:
  - Extra Slot works at most once per level;
  - Return puts the pod on top of its original stack with its remaining count, and cleared tiles stay cleared;
  - Return on a pod committed as part of a connected group moves only that pod; the other members stay in their slots;
  - Bloom Burst removes every layer of the variant (visible and hidden) and every pod of the variant, and accounting
    still reconciles;
  - Shuffle leaves waiting pods untouched, keeps locks attached and connected pods together, and yields a winnable
    arrangement whenever the relaxed problem is solvable;
  - Shuffle is deterministic for the same state and use index;
  - `BoosterNotApplicable` is returned in each disabled case (FR-043 to FR-050).
  Status: `BoosterTests` (11 cases), plus property tests whose random command sequences now mix in all four
  boosters (hash, accounting, determinism) and a property that Shuffle never turns a winnable tray into a losing one.
  They found and fixed a real bug: board clones shared their layer stacks, which Bloom Burst rewrites (now
  copy-on-write).
- [X] T114 [P] [US5] Write `client/Assets/Bloomlings/Tests/EditMode/EconomyServiceTests.cs`:
  - a win pays base, plus the clean-clear bonus, plus the Hard/Super Hard bonus;
  - buying a booster with Petals works;
  - "Petals and charges are never negative";
  - an unlock grants exactly one charge;
  - remote values are clamped to their ranges.

### Implementation for User Story 5

- [X] T115 [US5] Implement `core/src/Bloomlings.Core/Boosters/ExtraSlot.cs` and `Return.cs`, emitting
  `ExtraSlotAdded` and `PodReturned` (FR-043, FR-045).
  Status: boosters run on a Jammed or Stuck board too (they are the recoveries), count in `BoostersUsed`, and settle
  afterwards. A returned member of a connected group commits alone: only members still in the tray form the group.
- [X] T116 [US5] Implement `core/src/Bloomlings.Core/Boosters/BloomBurst.cs`, emitting `VariantBurst` (FR-050 default semantics; see research.md *Deferred decisions*).
  Status: only a removed visible top layer counts as a clear (its key is collected and special counters advance);
  removed hidden layers do not, as the player never saw them.
- [X] T117 [US5] Implement `core/src/Bloomlings.Core/Boosters/ShufflePlanner.cs` (R10):
  1. search the relaxed problem with `Search/StateSearch.cs`, where any eligible pod may be committed at any time;
  2. lay the found order out round-robin (pod `i` goes to stack `i mod k` at depth `⌊i/k⌋`, connected members at the
     same depth);
  3. verify with the normal search;
  4. fall back to the next PRNG candidate, seeded with `hash(seed, contentVersion, shuffleUses, StateHash)`;
  5. as a last resort, use the arrangement that maximizes immediately progressable exposed pods.
  Status: `ShufflePlanner` runs the relaxed search (any tray unit, locks and groups respected), a round-robin layout
  that puts group members at equal heights, verification, 8 seeded candidates, then a progress-first layout, all
  within `ShuffleNodeBudget`.

  The budget is `SessionOptions.ShuffleNodeBudget`, loaded from the content manifest and never from Remote Config.

  Use `SessionOptions.ShuffleNodeBudget` as the budget and emit `TrayShuffled`.
- [X] T118 [P] [US5] Implement `client/Assets/Bloomlings/Services/Economy/EconomyConfig.cs`. It holds the bundled
  defaults and clamping ranges for the `economy.*` keys in `contracts/backend-services.md`, for example
  `economy.petals.base` 12 (5–50), `economy.price.bloomBurst` 60 (10–500) and `economy.drop.everyLevels` 5 (2–20).
- [X] T119 [US5] Implement `client/Assets/Bloomlings/Services/Economy/EconomyService.cs`:
  - the Petals wallet;
  - level rewards (FR-041);
  - booster purchases with Petals;
  - unlock grants at L3, L4, L6 and L9 with one charge each (FR-042);
  - level-completion drops: every `economy.drop.everyLevels`-th completed level grants 1 charge, rotating through the
    unlocked boosters, with no randomness (FR-047);
  - a milestone grant hook (FR-047);
  - each booster use consumes a charge (FR-048).
- [X] T120 [US5] Implement `client/Assets/Bloomlings/UI/Gameplay/BoosterBar.cs`:
  - four buttons showing the owned count or the price;
  - disabled when `LevelSession.Check` rejects (FR-051);
  - Return picks its target slot, and Bloom Burst picks a visible variant;
  - Shuffle plays the tray animation.
  Status (T118–T122): written and compiled against the Unity stubs, not yet run in the Editor. Before a booster, the
  timeline plays out what is pending, so slots and tray are rebuilt from the settled state. Return targets a slot
  and Bloom Burst a tile. Without Petals or a charge the player gets a message; the Store is never forced.
- [X] T121 [US5] Wire recovery into `client/Assets/Bloomlings/UI/Screens/JamScreen.cs`. List the eligible boosters,
  owned or affordable with Petals, plus Restart. Never force the Store (FR-027).
- [X] T122 [US5] Add booster demos in `client/Assets/Bloomlings/UI/Tutorial/Demos/` for L3, L4, L6 and L9, and replace
  the WinScreen reward placeholder with real Petals in `client/Assets/Bloomlings/UI/Screens/WinScreen.cs`.
- [X] T123 [US5] Add booster golden cases `core/tests/golden/boost-extra-slot.golden.json`,
  `boost-return.golden.json`, `boost-bloom-burst.golden.json` and `boost-shuffle.golden.json`. The shuffle case must
  prove the same result on every run.

**Checkpoint**: Boosters, the economy and recovery work, and determinism holds with boosters (SC-005).

---

## Phase 8: User Story 6 - Store, ads and daily rewards (Priority: P4)

**Goal**:

- the Store from L12 with IAP and Remove Ads;
- restore purchases;
- server-validated purchases;
- consent;
- rewarded placements, including the jam rescue;
- interstitials under strict placement rules;
- the Daily Reward from L7;
- Remote Config tuning.

**Independent Test**: Quickstart §7 rows "Offline play", "Interstitial rules" and "Remove Ads + restore" pass.

### Tests for User Story 6 ⚠️

- [X] T124 [P] [US6] Write `client/Assets/Bloomlings/Tests/EditMode/AdPolicyTests.cs` (SC-013):
  - no interstitial before `ads.interstitial.firstLevel` (11), during a level, or right after a fail;
  - `minSeconds` and `minLevels` caps apply;
  - none when Remove Ads is owned;
  - a rescue happens at most `ads.rescue.perAttempt` (1) times per attempt.
- [X] T125 [P] [US6] Write `client/Assets/Bloomlings/Tests/EditMode/PurchaseLedgerTests.cs` and
  `DailyRewardTests.cs`:
  - grants are idempotent by `transactionId`;
  - Remove Ads is restored;
  - one daily claim is allowed per UTC calendar day, using a fake `IClock`.

### Implementation for User Story 6

- [X] T126 [P] [US6] Implement the UGS-backed `IRemoteConfigService` (interface from T027) as
  `client/Assets/Bloomlings/Services/Backend/UgsRemoteConfigService.cs` (package `com.unity.remote-config`). It
  falls back to the `BundledRemoteConfigService` defaults and keeps the clamping. Mirror all keys from `contracts/backend-services.md` in
  `backend/remote-config/defaults.json` (FR-085).
- [X] T127 [P] [US6] Implement `client/Assets/Bloomlings/Services/Consent/IConsentService.cs` and `ConsentService.cs`,
  using Google UMP and
  Apple ATT through `com.google.ads.mobile` (OpenUPM scoped registry in `client/Packages/manifest.json`). It must run
  before ads or analytics initialize, and it defaults to the most restrictive choice (FR-090).
  Status: `ConsentService` stays `Unknown` (no ads, no analytics) until a provider answers. The UMP provider (with ATT
  under `BLOOMLINGS_ATT`) lives in the ads integration assembly. The OpenUPM registry is in `manifest.json`; the
  package is added through Package Manager (client README). Not yet compiled against the SDK.
- [X] T128 [US6] Implement `client/Assets/Bloomlings/Services/Ads/IAdsService.cs` and `GoogleMobileAdsService.cs`,
  covering rewarded and interstitial load and show with mediation-ready ad unit config per environment.
  Status: `IAdsService` and the offline `UnavailableAdsService` are in the client. `GoogleMobileAdsService` (Google
  test units in development, release ids to fill) lives in `Integrations/GoogleMobileAds/` rather than
  `Services/Ads/`, so the game assembly never references an SDK. Not yet compiled against the SDK.
- [X] T129 [US6] Implement `client/Assets/Bloomlings/Services/Ads/AdPolicy.cs`, enforcing FR-052 and FR-053 with the
  T124 rules and `IClock`, and hook it at the post-win transition in `client/Assets/Bloomlings/App/GameFlow.cs`.
  Status: `GameFlow.PostWinTransition`, set by Boot, asks `AdPolicy` before Next loads the following level.
- [X] T130 [US6] Add the rewarded placements:
  - jam rescue in `JamScreen.cs`: grants a free use of an eligible jam-resolving booster, once per attempt;
  - free booster offer on `HomeScreen.cs`;
  - doubled win reward on `WinScreen.cs`;
  - daily bonus in `client/Assets/Bloomlings/Meta/DailyReward/DailyRewardPopup.cs`.
  Status: the jam rescue grants a free Extra Slot (else Shuffle) use; the free booster on Home goes to the unlocked
  booster with the fewest charges, once per session; the doubled win reward and the daily bonus follow the same
  pattern. Every offer shows only when an ad is ready.

  Every placement is started by the player.
- [X] T131 [US6] Implement `client/Assets/Bloomlings/Services/Purchases/IPurchaseService.cs` and
  `UnityIapPurchaseService.cs` (`com.unity.purchasing` v5), with the product catalog in
  `client/Assets/Bloomlings/Services/Purchases/ProductCatalog.json`:
  - `petals_s|m|l`: consumable;
  - `boosters_bundle_*`: consumable;
  - `starter_pack`: consumable, offered once;
  - `remove_ads`: non-consumable.
  Status: `IPurchaseService`, `PurchaseLedger` (idempotent by transaction id, restores Remove Ads) and the Settings
  Restore wiring are in the client. The catalog moved to `Services/Purchases/Resources/ProductCatalog.json` so it can
  load at runtime. `UnityIapPurchaseService` (IAP 5; validate through Cloud Code, then grant, then confirm) lives in
  `Integrations/UnityIap/`. Not yet compiled against the SDK.

  Grants go through the purchase ledger, idempotent by `transactionId`. Restore Purchases is wired to
  `SettingsScreen.cs` (FR-073).
- [X] T132 [P] [US6] Write the UGS Cloud Code scripts `backend/cloud-code/ValidatePurchase.js` and
  `backend/cloud-code/GetStarterPackOffer.js`, following the Cloud Code functions table in
  `contracts/backend-services.md`. `ValidatePurchase` is idempotent by `transactionId` (FR-089).
  Status: syntax-checked with Node, not yet deployed. Google receipts are checked by signature with the Play license
  key, Apple transactions through the App Store Server API; secrets come from the UGS Secret Manager (see
  `backend/README.md`).
- [X] T133 [US6] Implement `client/Assets/Bloomlings/UI/Screens/StoreScreen.cs`:
  - unlocks at L12 (FR-051);
  - sells Petal packs, boosters for Petals, Remove Ads and the starter pack;
  - shows an "unavailable" state when offline (FR-074).
- [X] T134 [US6] Implement `client/Assets/Bloomlings/Meta/DailyReward/DailyRewardService.cs` and
  `DailyRewardPopup.cs`. It unlocks at L7 and allows one claim per UTC calendar day through `IClock`, persisted in the
  save's `daily` section (FR-055). The claim pays `daily.reward.petals` plus `daily.reward.streakBonusPetals` per
  consecutive day, capped at `daily.reward.streakMaxDays`.

**Checkpoint**: Monetization works and never blocks or breaks the puzzle (FR-056).

---

## Phase 9: User Story 7 - Long-run motivation: leaderboard, milestones, cosmetics, collection (Priority: P5)

**Goal**:

- the Leaderboard from L10;
- milestones;
- the Wardrobe from L40;
- the Daily Challenge from L50, behind a flag;
- the Collection;
- theme rotation;
- identity and cloud save.

**Independent Test**: Reach L50 and check each unlock, the leaderboard rank, an equipped skin, the daily challenge and
the Collection. Quickstart §7 rows "Cloud merge", "Leaderboard" and "Daily Challenge" pass.

### Tests for User Story 7 ⚠️

- [X] T135 [P] [US7] Write `client/Assets/Bloomlings/Tests/EditMode/SaveMergeTests.cs`, following R15:
  - the base is the save with the higher `highestCompletedLevel`, then the later `updatedAt`;
  - entitlements, cosmetics and milestones are unioned;
  - missing ledger entries are re-applied idempotently.
  Status: 4 merge tests plus 3 sync tests in `CloudSaveAndLeaderboardTests.cs` (offline queue, merge on reconnect,
  empty cloud, failed upload, unreadable cloud document never overwritten), all passing under `client/DotnetCheck`.
- [X] T136 [P] [US7] Write `client/Assets/Bloomlings/Tests/EditMode/LeaderboardScoreTests.cs` for the score
  `level × 10_000_000 + (9_999_999 − minutesSince(2026-01-01T00:00Z))`: for the same level, the earlier completion
  ranks higher, and the value is exact in a double.
  Status: passing under `client/DotnetCheck` (formula, tie-break, exactness up to level 900 million, UTC handling).
- [X] T137 [P] [US7] Write `client/Assets/Bloomlings/Tests/EditMode/MilestoneAndDailyChallengeTests.cs`:
  - each milestone is granted exactly once;
  - a level that matches several cadences (L100) grants only the largest cadence's reward;
  - the UTC date maps to the same daily-pool index on every device.
  Status: passing under `client/DotnetCheck`; also checks that `MilestoneTable.Default` mirrors
  `content/roadmap/milestones.json` and that the Daily Challenge pays once per day without touching Level N.

### Implementation for User Story 7

- [X] T138 [US7] Implement `client/Assets/Bloomlings/Services/Backend/IAuthService.cs` and `UgsAuthService.cs`
  (`com.unity.services.authentication`). It signs in anonymously on the first launch without blocking play, and
  offers optional Sign in with Apple and Google Play Games linking from Settings (FR-087).
  Status: `IAuthService` and the offline `LocalOnlyAuthService` are in `Services/Backend/IAuthService.cs`. The UGS
  implementation lives in `Integrations/Ugs/UgsAuthService.cs` (as with T128, so the game assembly never references an
  SDK). Boot signs in after the game is playable. Settings shows the link buttons only when a platform token source is
  registered (`ServiceProviders.AppleIdToken` / `GooglePlayGamesAuthCode`). The Sign in with Apple and Google Play Games
  plugins that provide those tokens are not in the repository yet. Not yet compiled against the SDKs.
- [X] T139 [US7] Implement `client/Assets/Bloomlings/Services/Save/ICloudSaveService.cs`, `CloudSaveSync.cs` and
  `SaveMerge.cs`
  (`com.unity.services.cloudsave`, key `player_save_v1`). While offline it queues changes; on reconnect it merges
  following R15.
  Status: `SaveMerge` merges out of place and `PlayerSave.Assign` applies the result in place, because the services
  hold the save instance. `CloudSaveSync` runs at sign-in, when Home opens and after a win, never mid-level. A cloud
  document this app cannot read is left untouched. `UgsCloudSaveService` is in `Integrations/Ugs/`. Not yet compiled
  against the SDK.
- [X] T140 [US7] Implement `client/Assets/Bloomlings/Services/Backend/ILeaderboardService.cs` and
  `UgsLeaderboardService.cs`
  (`com.unity.services.leaderboards`, id `global_highest_level`) with the T136 score encoding. Write the Cloud Code
  script `backend/cloud-code/SubmitProgress.js`, whose sanity checks reject:
  - a non-monotonic level;
  - an implausible jump for the elapsed time;
  - an unsupported content version.

  Rejections are logged, not banned.

  Status: `ILeaderboardService` and the offline fallback are done. `LeaderboardClient` keeps the queue in the stats
  counter `leaderboard.submittedLevel`, so no schema change was needed. `UgsLeaderboardService` is in
  `Integrations/Ugs/`. `backend/cloud-code/SubmitProgress.js` computes the score with server time, which is harder to
  tamper with than a client timestamp. Neither the client code nor the script has been run against a live UGS project.
- [X] T141 [P] [US7] Implement `client/Assets/Bloomlings/UI/Screens/LeaderboardScreen.cs`. It unlocks at L10 and
  shows the player's rank and neighbours, with a stale label when offline. Also fill the Home rank slot (FR-058,
  FR-062).
  Status: code done; the layout has not been checked in the Unity Editor yet.
- [X] T142 [US7] Implement `client/Assets/Bloomlings/App/Progression/MilestoneService.cs`, with its data in
  `content/roadmap/milestones.json`:
  - every 25 levels a bundle;
  - every 50 a cosmetic or profile reward;
  - every 100 a major milestone;
  - at 250, 500, 1000 and later cadences, a prestige reward;
  - when a level matches several cadences, only the largest cadence's reward is granted (FR-061).

  Each milestone is granted once, with a short celebration. The service also feeds the Home teaser (FR-061).
  Default contents of `milestones.json`, tuned later:

  | Cadence | Default bundle |
  |---|---|
  | 25 | 50 Petals + 1 booster charge |
  | 50 | 100 Petals + 1 cosmetic or profile item |
  | 100 | 300 Petals + 1 charge of each booster + 1 cosmetic |
  | 250 / 500 / 1000 and later | 500 Petals + prestige frame, skin, badge or leaderboard marker |

  Status: `MilestoneTable.Default` mirrors the JSON, and a test keeps the two equal. The single booster charge of the
  25-level bundle goes to the unlocked booster with the fewest charges. Items come from the cadence's list: the first
  one not yet owned. The Win screen shows a short milestone line.
- [X] T143 [US7] Implement the Wardrobe in `client/Assets/Bloomlings/Meta/Wardrobe/WardrobeService.cs`,
  `CosmeticCatalog.asset` and `client/Assets/Bloomlings/UI/Screens/WardrobeScreen.cs`:
  - unlocks at L40;
  - one equipped skin per family, affecting presentation only;
  - a check that cosmetics do not reduce readability (FR-063).
  Status: the catalog is `Meta/Wardrobe/Resources/CosmeticCatalog.json` rather than `CosmeticCatalog.asset`. The
  repository does not track `.meta` files, so a hand-written asset could not reference its script. Readability check
  (tested): worn items are small accessories in neutral tints (HSV saturation ≤ 0.3) and never change the variant tint
  or icon. Frames, badges and markers decorate the profile only. The accessory art is procedural placeholder art, not
  yet checked in the Editor.
- [X] T144 [US7] Implement the Daily Challenge in `client/Assets/Bloomlings/Meta/DailyChallenge/DailyChallengeService.cs`
  and `client/Assets/Bloomlings/UI/Screens/DailyChallengeScreen.cs`:
  - the daily pack from `publish` provides the puzzles;
  - the UTC date maps to a pool index;
  - flag `feature.dailyChallenge`, unlock L50;
  - it has a separate reward and never changes Level N (FR-064, R19).
  Status: `ContentSet.DailyPool` now reads the daily pack in `ContentLoader`, and the bundled loader no longer skips
  it. The pool index is whole UTC days since 2026-01-01 modulo the pool size. The reward is a constant 30 Petals
  (`DailyChallengeService.RewardPetals`); it is not a Remote Config key because the contract lists none. Dev content
  without a daily pack hides the button.
- [X] T145 [US7] Implement the Collection in `client/Assets/Bloomlings/Meta/Collection/CollectionService.cs` and
  `client/Assets/Bloomlings/UI/Screens/CollectionScreen.cs`. Each win adds `{pictureId, pictureVersion, mappingHash,
  levelNumber}`, and the screen shows the finished pictures. It is never a level selector (FR-065).
  Status: entries are added when a level is won, and the mapping hash is the first 16 hex digits of the SHA-256 of the
  sorted `role=variant` pairs. The screen redraws a picture only when the current content still has the same picture
  and colors for that level; otherwise it shows the level number.
- [X] T146 [US7] Implement `client/Assets/Bloomlings/Gameplay/Themes/ThemeRotation.cs` with its data in
  `content/roadmap/themes.json`. It picks the background theme by level band (daylight garden, pond, orchard, moonlit
  garden). This is visual only (FR-066).
  Status: the rotation starts at L100 (`system.theme_rotation`) with the pond theme and changes every 50 levels. All
  themes keep light backgrounds (tested). Applied to Home and to the gameplay background.

**Checkpoint**: All user stories are complete.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Analytics, performance, accessibility, localization, device determinism, launch content production and
final validation.

- [X] T147 [P] Implement `client/Assets/Bloomlings/Services/Analytics/IAnalyticsService.cs`,
  `FirebaseAnalyticsService.cs`, `client/Assets/Bloomlings/Services/Analytics/ICrashReporter.cs` and
  `CrashlyticsCrashReporter.cs` (Firebase Crashlytics):
  - emit every event in `contracts/analytics-events.md` with its common parameters;
  - add the crash custom keys `app_version`, `content_version`, `level_number`, `definition_version` and `picture_id`
    (FR-086, R14);
  - initialize only after consent (T127).
  Status: `GameAnalytics` is the engine-free facade. It adds the common and level parameters, keeps events on the
  device until consent allows analytics, and drops them if consent is refused. `AnalyticsContractTests` parses the
  contract and checks every event and parameter. All 22 events are wired: gameplay, meta, monetization and content.
  `purchase` reports `price_micros` 0 and an empty `currency` because the purchase service exposes only a formatted price
  string. The Firebase code lives in `Integrations/Firebase/` (`FirebaseServices.cs`, with the analytics service and
  the Crashlytics reporter) rather than `Services/Analytics/`, so the game assembly never references an SDK. It has not
  been compiled against the Firebase SDK yet.
- [ ] T148 [P] Move all player-facing strings into Unity Localization tables under
  `client/Assets/Bloomlings/UI/Localization/`, starting with the English table (R18).
- [ ] T149 [P] Run the accessibility pass and write the results to
  `specs/001-core-game-mvp/checklists/accessibility.md`:
  - run the readability tool (T075) on every variant pair that can share a level;
  - take colorblind-simulation screenshots of 6-variant boards;
  - check pod counts at the smallest supported screen (FR-072, SC-003);
  - run the manual FR-005 checks for every approved pair: tell the two variants apart on a pod in the tray, in a
    Waiting Slot, and on moving Bloomling characters, at 1× and 2× speed.
- [ ] T150 Profile and optimize on the reference low-end devices (R16) and record the results in
  `specs/001-core-game-mvp/checklists/performance.md`:
  - sprite atlases and pooled workers (T046);
  - backlog compression tuning (T045);
  - targets: a 30 fps floor, no hitches over 100 ms, level load ≤ 1 s and cold start ≤ 5 s.
- [ ] T151 Create the IL2CPP device determinism check: scene `client/Assets/Bloomlings/Tests/Device/RunGoldenReplays.unity`
  and `RunGoldenReplays.cs`, which run the golden corpus on Android and iOS builds and report hashes (SC-005,
  SC-011).
- [ ] T152 [P] Create `.github/workflows/unity-build.yml`, which runs GameCI `unity-builder` for Android and iOS
  (Unity licence secrets) plus the Unity EditMode tests (`unity-test-runner`).
- [ ] T153 Produce the launch catalog in `content/catalog/`, publishing to `build/content/` and then
  `client/Assets/StreamingAssets/content/`:
  - run `generate` for Levels 101–5000+ with every band profile;
  - run `validate` and `score` (cadence: from L11, 15–25 Hard and 6–10 Super Hard per 100 levels);
  - human sampling per the FR-084 tiers, with stronger review for milestone, Hard and Super Hard levels;
  - run `daily generate`;
  - run `publish --content-version 1`, then import into StreamingAssets (T091).

  SC-004 and SC-012 must pass.
- [ ] T154 [P] Run an originality review and record it in `specs/001-core-game-mvp/checklists/originality.md`. Confirm
  that no Colony Flow name, characters, art, audio, level pictures or UI graphics appear in `client/Assets/` or
  `content/pictures/`, and that every picture's `source.licence` is owned or licensed (FR-091).
- [ ] T155 Run the playtests from quickstart §8 with at least 30 new players and at least 15 for the glance and
  recognition tests. Record SC-001, SC-002, SC-003, SC-006, SC-007, SC-010, SC-014 and SC-015 in
  `specs/001-core-game-mvp/checklists/playtest-results.md`.
- [ ] T156 Run the full `specs/001-core-game-mvp/quickstart.md` validation, sections 1–8. Update `CLAUDE.md` and
  `README.md` with the final build, test and pipeline commands.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies. T001 decides whether work may continue past Phase 1 (constitution development
  gate).
- **Foundational (Phase 2)**: depends on Setup and on T001 confirming the lock. It blocks every user story.
- **User stories (Phases 3–9)**: all depend on Foundational.
- **Polish (Phase 10)**: depends on the stories being shipped. T153 needs US3 and US4.

### User Story Dependencies

| Story | Priority | Depends on | Notes |
|---|---|---|---|
| US1 Play a level | P1 | Foundational | The MVP. It has no dependency on any other story |
| US2 Linear progression | P1 | US1 (plays levels through `GameplayController`) | Can start its save and roadmap work (T054, T057, T058) in parallel with US1 |
| US3 Validated catalog | P2 | US1 core (T031–T037): the solver and generator drive `LevelSession` | The client part (T091, T092) needs US2's `CatalogService` |
| US4 Mechanics | P3 | US1 core; extends the US3 solver, validator and generator (T104, T105, T111) | Rules and client work can start right after US1 |
| US5 Boosters & Petals | P3 | US1 core (Shuffle also needs `Search/StateSearch.cs`, T076); US2 save | T076 can be pulled forward if US5 starts before US3 |
| US6 Store, ads, daily | P4 | US2 (save, progression, flow); US5 (economy, boosters for the rescue) | |
| US7 Long-run motivation | P5 | US2 (save, progression) | The Daily Challenge pack needs US3 `publish` |

### Within Each User Story

- Tests are written first and must fail before implementation. This is required by the constitution for rules
  code.
- Rules core before client presentation. Models before services before UI.
- Every rules change updates the golden corpus (T030 harness) and keeps `dotnet test` green.
- Every content change passes `validate` before commit.

### Parallel Opportunities

- **Setup**: T003, T004, T005, T007, T009, T010, T011 and T012 are [P].
- **Foundational**: T013–T018 are all [P]. T021–T024 are [P] after their subjects exist. T026 and T027 are [P].
- **US1**: the tests T028–T030 are [P]. T031–T033 are [P]. On the client, T039, T040, T043, T044 and T048–T051 are
  [P] once T047's interfaces are agreed.
- **US3**: T068–T070, T071, T072, T079 and T083 are [P].
- **US4**: T096 and T097, and the client visuals T106–T110, are [P].
- **Across stories**: once US1's core (T031–T037) is done, US3 (pipeline), US4 (rules) and US5 (boosters) can proceed
  in parallel with separate owners.

---

## Parallel Example: User Story 1

```bash
# Tests first (all [P]):
Task: "T028 Write rule tests in core/tests/Bloomlings.Core.Tests/Rules/CoreLoopTests.cs"
Task: "T029 Write FsCheck properties in core/tests/Bloomlings.Core.Tests/Properties/InvariantProperties.cs"
Task: "T030 Create golden replay harness in core/tests/Bloomlings.Core.Tests/Golden/GoldenReplayTests.cs"

# Core building blocks (all [P]):
Task: "T031 Implement commands/events/results in core/src/Bloomlings.Core/Simulation/"
Task: "T032 Implement core/src/Bloomlings.Core/Tray/SourceTray.cs and PodRuntime.cs"
Task: "T033 Implement core/src/Bloomlings.Core/Slots/WaitingSlots.cs"

# Client views (all [P], after T047 interfaces):
Task: "T043 TrayView/PodView"  Task: "T044 SlotRowView"  Task: "T048 GameplayHud"
Task: "T049 PauseScreen"       Task: "T050 WinScreen"    Task: "T051 JamScreen"
```

## Parallel Example: User Story 3

```bash
Task: "T068 SolverTests"   Task: "T069 GeneratorTests"   Task: "T070 PackWriterTests"
Task: "T071 LevelPackWriter/ManifestWriter"   Task: "T072 IndexedPngReader/TextGridReader"
Task: "T079 ValidationRecord"   Task: "T083 GenerationProfile/ProfileLoader"
```

## Parallel Example: User Story 4

```bash
Task: "T096 MechanicsTests"   Task: "T097 FairnessTests"
Task: "T106 TileView peek/stone"   Task: "T107 KeyView"   Task: "T108 SpecialView"
Task: "T109 PodView/TrayView states"   Task: "T110 SlotRowView locked slot"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup, including the T001 gate decision.
2. Phase 2 Foundational.
3. Phase 3 US1.
4. **Stop and validate**: run quickstart §1 and §5 steps 1–6. A hand-made level plays to a win and to a jam, and the
   golden replays match in `dotnet test` and in Unity EditMode.

### Incremental Delivery

| Step | Adds | Result |
|---|---|---|
| 1 | US1 | Playable, deterministic level (MVP) |
| 2 | US2 | Vertical slice: first launch → Level 10, Home, save |
| 3 | US3 | Validated content pipeline and curated 1–100 |
| 4 | US4 + US5 | Full mechanic set and boosters; regenerate the affected levels |
| 5 | US6 | Store, ads and daily reward (soft-launch economy) |
| 6 | US7 | Long-run meta |
| 7 | Polish | T153 launch catalog 1–5000+, performance, device determinism, playtests |

### Parallel Team Strategy

After Foundational and US1 core:

- **Rules/tools developer**: US3 pipeline → US4 rules → US5 boosters.
- **Client developer**: US2 → US4/US5 visuals → US6 → US7.
- **Content designer and artist**: T094 picture library → T067, T095 and T111 curation → T153 catalog.

---

## Notes

- [P] tasks touch different files and have no dependency on incomplete tasks.
- The story labels trace back to the spec's user stories. Setup, Foundational and Polish tasks carry no label.
- Determinism is non-negotiable (constitution III). Never introduce `float`, `System.Random`, wall-clock time or
  hash-order iteration into `core/src/Bloomlings.Core`.
- Commit after each task or logical group. Stop at any checkpoint to validate the story on its own.
- Deferred decisions (research.md): the ads mediation vendor, the final Bloom Burst semantics, the economy numbers,
  the Level 8 unlock and the L40 cosmetic. Tasks use the documented defaults and must not block on them.
