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

**Status after the audit of 2026-09-29**: 129 tasks are done and 27 open. A task is checked only when its deliverable
has actually run, not just been written:

- Unchecked by the audit: T007 (no `.meta` files), T053 (never run in Unity), the SDK integrations T126, T127, T128,
  T131, T138, T139, T140 and T147 (never compiled: their packages are not in `manifest.json`), T148 (no Unity
  Localization tables), T151 (device scene never built) and T152 (the Unity workflow has never run).
- Fixed by the audit, with tests: a Bloom Burst soft-lock next to specials (T116), recoveries offered on a jam that could
  not change it and a Jam screen that did not come back (T113–T121, T130), consent mapping (T127), IAP orders confirmed
  without a grant and receipt replay across accounts (T131, T132), a player-writable leaderboard state (T140), the
  daily reward clock exploit and repeatable ad bonuses (T130, T134), and Level N+1 replaying Level 1 in release builds.
- Completed by the audit: the generator and validator follow the Level Band Guidelines (T080, T087, T089; the 9
  showcases were regenerated), and `publish` validates the whole catalog before it writes anything (T081).
- Not in the plan, added by the audit: sound, music and haptics (the Settings toggles had nothing to control). The
  cues and a music loop are synthesized in code (`Services/Feedback/ToneSynth.cs`, no audio assets yet), Android
  vibrates in short pulses and iOS only for strong events, and each follows its toggle at once (see T066).
- The 13 pictures marked `approved` were approved by the implementing agent, not by a person (see T067).
- Known deviations from plan.md and research.md: uGUI instead of the URP 2D renderer (R1), no Addressables (R6), no
  Unity Test Framework, Input System or Localization package in `manifest.json` (R17, R18), no iOS build path, and
  generator profiles without `podSizes`, `difficultyTarget` and `milestoneConstraints` (data-model §1.5).

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
- [ ] T007 [P] Add Unity local-package manifests next to the shared sources. `.meta` files generated by Unity for
  these folders must be committed.
  Open (Audit 2026-09-29): the manifests and asmdefs are in place, but no `.meta` files are committed anywhere in the repository.
  Unity creates them on first open; commit them then.
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
  - compress a backlog above `fx.backlogThresholdMs` (default 12000 since 2026-10-05, 6000 from 2026-10-03, 1500 before) by speeding up to 4× and merging walkers;
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
- [ ] T053 [US1] Write `client/Assets/Bloomlings/Tests/EditMode/GoldenReplayEditModeTests.cs`. It runs every
  `core/tests/golden/*.golden.json` through the Unity-compiled `com.bloomlings.core` and asserts the same hashes
  (SC-005).
  Open (Audit 2026-09-29): the test is written and passes under .NET through `client/DotnetCheck`, which is not the Unity-compiled
  package. The Unity Test Framework is not in `manifest.json` yet, so it has never run in Unity.

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
  Fixed (Audit 2026-09-29): the music, sound and haptics toggles were saved but controlled nothing, because the client
  had no audio or haptics at all. `GameFeedback` now plays synthesized cues (tap, refused tap, tile clear, pod done,
  key, special, booster, jam, win, button click) and a pentatonic music loop, and pulses haptics; `FeedbackPolicy`
  applies the toggles (tested in `FeedbackTests`). Placeholder sounds until real audio exists; never heard on a device
  yet. Settings also gained Privacy options and Restore feedback (T127, T131). Amended by spec 005 FR-042 (the owner,
  2026-10-07): each clearing style has its own synthesized sounds and a micro haptic a tile, and a pod done its own
  haptic; tile clears vibrate now, as gentle spaced ticks (spec 005 T172–T178).
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
  Correction (Audit 2026-09-29): the 10 pictures of L1–10 and 3 more (13 in all) were set to `approved` by the implementing agent
  (reviewer `content`), not by a person as FR-084 requires. `validate` passes on L1–10 only because of that. They are
  provisional until the product owner approves or rejects them.

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
  Fixed (Audit 2026-09-29): the Level Band Guidelines were only partly checked (variant counts, and not as the roadmap moves
  them). Now `BandGuidelines` drives the variant count (L26–31 4, L51–100 5 with 6 in Hard from L70, …), and the
  validator checks board size, Source Pod count, work by class and pods of 5+ tiles by band, and all four families in
  any 5 levels from L20. The typical duration is not checked: the solver's estimate is uncalibrated until playtests.
- [X] T081 [US3] Implement the CLI in `core/src/Bloomlings.Pipeline/Program.cs` and `Commands/*.cs`, following
  `contracts/pipeline-cli.md`:
  - commands: `pictures import`, `pictures validate`, `readability`, `solve`, `validate` (`--changed-only` uses
    `git diff --name-only origin/main`), `score`, `replay`, `diff`, `publish`;
  - exit codes 0/1/2 and `--json` output;
  - `score` reports the per-100-level class counts from L11 (Hard 15–25, Super Hard 6–10) and checks that the level
    after a Super Hard is Normal (FR-059).
  Fixed (Audit 2026-09-29): `publish` wrote packs without validating, although plan.md says it refuses failing
  catalogs. It now runs the validator over the whole catalog (the daily pool level by level) and exits 1 on any
  error; `--allow-draft` tolerates only unapproved pictures.
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
  Fixed (Audit 2026-09-29): pods of 1–4 tiles were produced (66 of 841 in the previews). A small wave now joins the nearest pod of
  its variant, and splits never go under 5.
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
  Fixed (Audit 2026-09-29): the profiles' buffer-pressure and Hard pressure targets were loaded but never used, work ranges
  ignored the class, and the roadmap's L70 (6-variant Hard), L125 (depth 3), L175 (advanced combinations), L225
  (advanced Hard) and L20 (all four families) rows had no effect. The generator (gen-1.1.0) now applies the profile
  and the guidelines together per level and class, checks the winning line's peak slot use against the pressure
  target, allows 3 mechanics from L175, and presses Hard levels critically from L225. The 9 showcase levels were
  regenerated with it. The playtest levels in `playtest/content/levels` predate it.
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
  gap, repeats within 50 levels). Changed 2026-09-29/30: every workflow runs by hand only (the owner's Actions budget
  rule): `catalog-nightly.yml` lost its cron, `core-tests.yml` its push and pull_request triggers, and
  `content-validate.yml` its pull_request trigger (a `base` input replaces the pull request's base branch). The tests
  run locally before each push; the triggers come back when the budget allows.
- [X] T094 [US3] Seed the picture library for Levels 11–100 with at least 90 base pictures under
  `content/pictures/src/`, as `.grid.txt` or indexed PNG plus `.meta.json`. They show garden-world subjects:
  flowers, fruit, insects, small animals, garden tools and cozy objects. Import them with `pictures import`. Status
  stays `draft` until a person approves each picture for recognizability (FR-084, SC-015).
  Status: 94 draft pictures (27 subjects in up to four variations: 16 for 12×12–12×13, 26 for 12×13–13×14, 52 for
  13×14–14×16 since the bigger boards of 2026-10-05, version 2; they were 9×10–10×10, 10×10–12×12 and 10×12–14×14)
  are sketched procedurally by `content/pictures/tools/sketch_pictures.py` and imported (`pictures validate`: 0
  errors). Since 2026-10-05 every subject has at least five color roles (a sun, a plate, grass, a reed and so on were
  added to the four-role ones), so the five-variant levels from L51 can use any core picture, and every role keeps at
  least 5 cells (the smallest pod, `MIN_ROLE_CELLS`): a role under that, such as a ladybug's dots or a daisy's
  centers, grows into its nearest neighbours, because a variant with fewer tiles cannot fill a pod and the generator
  rejected such candidates at its partition step. They are placeholders for an artist to refine. A person still has to approve each picture
  for recognizability (FR-084, SC-015); until then `validate` fails every generated level on `picture-approved`.
- [ ] T095 [US3] Produce the curated Levels 11–100:
  - run `generate` with the band-0011-0025, band-0026-0050 and band-0051-0100 profiles, using only the mechanics
    already implemented;
  - have a person playtest every level (the FR-084 tier for 1–100);
  - commit accepted definitions and validation records to `content/catalog/`.

  Levels using US4 mechanics are regenerated after US4.

  Status: open (human steps). A preview of Levels 11–100 was generated from the draft pictures with
  `generate --allow-draft`, showcases included, into `content/work/` (gitignored). Last run on 2026-09-29: 84 of 90
  levels generated, all passing every `validate` check except `picture-approved`. L95–L100 failed with 360
  `picture:none-available` rejections: the 107-picture library runs out of pictures that fit the band tags and the
  no-repeat-within-50 rule, so it needs more pictures (T094). Rerun on 2026-09-29 with `gen-1.2.0` (band rules and
  practice levels): 34 of the 67 non-showcase levels in 11–94 were generated; L51+ mostly fail with
  `mapping:none-for-<picture>`, because Levels 1–100 use distinct pictures (FR-083) and from L51 a level needs 5
  variants, which only the 44 pictures with 5+ color roles can carry. The library needs about 50 more pictures with 5+
  color roles before Levels 51–100 can be generated. The playtest APK filled the gaps with older previews. Rerun on
  2026-10-05 at the bigger band sizes (T162; `content/work/pt5.sh`, then the levels it could not fill one by one with
  other seeds): all 90 levels of 11–100 generated, showcases included, every one passing every `validate` check except
  `picture-approved`; the library grew to 110 sketches (16 more five-variant core pictures), every role at least 5
  cells. The playtest APK now holds only generated levels. Still needed: picture approval (T094), the readability sign-off (T075), a person playtest of
  every level (FR-084), then regeneration with approved pictures and the commit into `content/catalog/`. For
  playtest builds, `publish --allow-draft` packs draft pictures as marked previews.
  Update 2026-10-06: Levels 1–50 are in `content/catalog/` (the curated L1–10, the showcases, the rest generated on
  approved regular pictures by `tools/catalog/build-catalog.sh`; `validate` 0 errors, `content/catalog/README.md`).
  L51–100 follow with the catalog build (T153). Still needed: the readability sign-off and a person playtest of every
  level (FR-084).

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
  regenerated). The other showcases, and since 2026-09-29 their practice levels (L12, L14, L17, L19, L29, L36, L61,
  L81, L91) and the late showcases and practices (L150–151 Chest, L250–251 Statue/Bridge, L400–401 Connected
  Triple, both Hard), are generated with `gen-1.2.0` into `content/showcase/` (see its README) and pass every check except
  `picture-approved`. `MechanicDemos` holds one demo per unlock, shown once when a level first uses an unlocked
  mechanic. Still needed: picture approval, playtests, then the copy into `content/catalog/`.
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
  `economy.petals.base` 12 (5–50), `economy.price.bloomBurst` 1800 (10–10000; 60 and 10–1000 before 2026-10-05) and
  `economy.drop.everyLevels` 5 (2–20).
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

- [ ] T126 [P] [US6] Implement the UGS-backed `IRemoteConfigService` (interface from T027) as
  `client/Assets/Bloomlings/Services/Backend/UgsRemoteConfigService.cs` (package `com.unity.remote-config`). It
  falls back to the `BundledRemoteConfigService` defaults and keeps the clamping. Mirror all keys from `contracts/backend-services.md` in
  `backend/remote-config/defaults.json` (FR-085).
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.
- [ ] T127 [P] [US6] Implement `client/Assets/Bloomlings/Services/Consent/IConsentService.cs` and `ConsentService.cs`,
  using Google UMP and
  Apple ATT through `com.google.ads.mobile` (OpenUPM scoped registry in `client/Packages/manifest.json`). It must run
  before ads or analytics initialize, and it defaults to the most restrictive choice (FR-090).
  Status: `ConsentService` stays `Unknown` (no ads, no analytics) until a provider answers. The UMP provider (with ATT
  under `BLOOMLINGS_ATT`) lives in the ads integration assembly. The OpenUPM registry is in `manifest.json`; the
  package is added through Package Manager (client README). Not yet compiled against the SDK.
  Fixed (Audit 2026-09-29): the UMP provider read "ads may be requested" as consent, so an EEA refusal became Personalized on
  Android. The state now comes from the TCF values (`TcfConsent`, tested); ATT is requested on iOS; Settings has
  Privacy options, and a change there applies at once.
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.
- [ ] T128 [US6] Implement `client/Assets/Bloomlings/Services/Ads/IAdsService.cs` and `GoogleMobileAdsService.cs`,
  covering rewarded and interstitial load and show with mediation-ready ad unit config per environment.
  Status: `IAdsService` and the offline `UnavailableAdsService` are in the client. `GoogleMobileAdsService` (Google
  test units in development, release ids to fill) lives in `Integrations/GoogleMobileAds/` rather than
  `Services/Ads/`, so the game assembly never references an SDK. Not yet compiled against the SDK.
  Fixed (Audit 2026-09-29): a release build without production ad unit ids used Google's test ads; it now shows no ads.
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.
- [X] T129 [US6] Implement `client/Assets/Bloomlings/Services/Ads/AdPolicy.cs`, enforcing FR-052 and FR-053 with the
  T124 rules and `IClock`, and hook it at the post-win transition in `client/Assets/Bloomlings/App/GameFlow.cs`.
  Status: `GameFlow.PostWinTransition`, set by Boot, asks `AdPolicy` before Next loads the following level.
- [X] T130 [US6] Add the rewarded placements:
  - jam rescue in `JamScreen.cs`: grants a free use of an eligible jam-resolving booster, once per attempt;
  - free booster offer on `HomeScreen.cs`;
  - doubled win reward on `WinScreen.cs`;
  - daily bonus in `client/Assets/Bloomlings/Meta/DailyReward/DailyRewardPopup.cs`.
  Status: the jam rescue grants a free use of Extra Slot, else Shuffle (a Stuck board only), else Return, and only of a
  booster that can change the jam (fixed in the Audit 2026-09-29: it could offer a Shuffle that changed nothing). The free booster
  on Home goes to the unlocked booster with the fewest charges, once per UTC day, saved (it was once per session, reset
  by a relaunch). The daily bonus claims the day's reward with its extra Petals, so it is once a day (it could be
  watched again on every Home visit). The doubled win reward follows the same pattern. Every offer shows only when an
  ad is ready.

  Every placement is started by the player.
- [ ] T131 [US6] Implement `client/Assets/Bloomlings/Services/Purchases/IPurchaseService.cs` and
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
  Fixed (Audit 2026-09-29): an order delivered at startup (a purchase interrupted earlier) was confirmed without being granted;
  the ledger is now known from `Initialize`. Owned purchases are fetched at start and on Restore (Remove Ads comes back
  on a new device), Restore shows its result, the backend's grants win over the bundled catalog, and the starter pack
  is checked with `GetStarterPackOffer`.
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.

  Grants go through the purchase ledger, idempotent by `transactionId`. Restore Purchases is wired to
  `SettingsScreen.cs` (FR-073).
- [X] T132 [P] [US6] Write the UGS Cloud Code scripts `backend/cloud-code/ValidatePurchase.js` and
  `backend/cloud-code/GetStarterPackOffer.js`, following the Cloud Code functions table in
  `contracts/backend-services.md`. `ValidatePurchase` is idempotent by `transactionId` (FR-089).
  Status: syntax-checked with Node, not yet deployed. Google receipts are checked by signature with the Play license
  key, Apple transactions through the App Store Server API; secrets come from the UGS Secret Manager (see
  `backend/README.md`).
  Fixed (Audit 2026-09-29): idempotency was per player, so one receipt could be granted again on another account. Transactions are
  now claimed once game-wide in custom data; a second starter pack is refused; server state lives in protected player
  data. `node --test backend/tests/*.test.js` covers this with in-memory UGS stand-ins (CI runs it).
- [X] T133 [US6] Implement `client/Assets/Bloomlings/UI/Screens/StoreScreen.cs`:
  - unlocks at L12 (FR-051);
  - sells Petal packs, boosters for Petals, Remove Ads and the starter pack;
  - shows an "unavailable" state when offline (FR-074).
  Added 2026-09-29: a Cosmetics tab after the Wardrobe unlock (T143), and pages of 7 rows.
- [X] T134 [US6] Implement `client/Assets/Bloomlings/Meta/DailyReward/DailyRewardService.cs` and
  `DailyRewardPopup.cs`. It unlocks at L7 and allows one claim per UTC calendar day through `IClock`, persisted in the
  save's `daily` section (FR-055). The claim pays `daily.reward.petals` plus `daily.reward.streakBonusPetals` per
  consecutive day, capped at `daily.reward.streakMaxDays`.
  Fixed (Audit 2026-09-29): the claim compared dates with "not equal", so setting the device clock back and forth
  gave a claim every time. A claim now needs a later day than the last one. The day stays the UTC day, so cloud saves
  from different time zones merge by date.

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

- [ ] T138 [US7] Implement `client/Assets/Bloomlings/Services/Backend/IAuthService.cs` and `UgsAuthService.cs`
  (`com.unity.services.authentication`). It signs in anonymously on the first launch without blocking play, and
  offers optional Sign in with Apple and Google Play Games linking from Settings (FR-087).
  Status: `IAuthService` and the offline `LocalOnlyAuthService` are in `Services/Backend/IAuthService.cs`. The UGS
  implementation lives in `Integrations/Ugs/UgsAuthService.cs` (as with T128, so the game assembly never references an
  SDK). Boot signs in after the game is playable. Settings shows the link buttons only when a platform token source is
  registered (`ServiceProviders.AppleIdToken` / `GooglePlayGamesAuthCode`). The Sign in with Apple and Google Play Games
  plugins that provide those tokens are not in the repository yet. Not yet compiled against the SDKs.
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.
- [ ] T139 [US7] Implement `client/Assets/Bloomlings/Services/Save/ICloudSaveService.cs`, `CloudSaveSync.cs` and
  `SaveMerge.cs`
  (`com.unity.services.cloudsave`, key `player_save_v1`). While offline it queues changes; on reconnect it merges
  following R15.
  Status: `SaveMerge` merges out of place and `PlayerSave.Assign` applies the result in place, because the services
  hold the save instance. `CloudSaveSync` runs at sign-in, when Home opens and after a win, never mid-level. A cloud
  document this app cannot read is left untouched. `UgsCloudSaveService` is in `Integrations/Ugs/`. Not yet compiled
  against the SDK.
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.
- [ ] T140 [US7] Implement `client/Assets/Bloomlings/Services/Backend/ILeaderboardService.cs` and
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
  Fixed (Audit 2026-09-29): the jump allowance lived in player-writable data, so deleting it reset the check; it is now protected
  player data, and the score is written with the service token (README: deny player leaderboard writes).
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.
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
  Added 2026-09-29: rewards never run out. Once a cadence's list is all owned, it grants a generated level badge
  (`badge.level_N`, a profile reward) or, for prestige cadences, a leaderboard marker (`marker.level_N`); the catalog
  resolves those ids without listing them. The prestige lists now hold skins too (`skin.moon_frost` at 250,
  `skin.golden_petals` at 500).
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
  Added 2026-09-29:
  - skins (FR-063, US7 "cosmetic skins"): a neutral pattern (spots, stripes, petals, speckles) cut to the family's
    silhouette and laid over the variant-colored body at 45% opacity, so the variant color and icon still read;
  - each family wears one skin, hat, trail and expression at once, and the profile shows one frame, badge and marker
    (the newest owned by default). The save's `cosmetics.equipped` holds `{family → {kind → id}, profile → {kind →
    id}}`; the early one-item-per-family form is still read (contract and data-model updated);
  - Store cosmetics for Petals (FR-051 "cosmetics join after the Wardrobe unlock", doc 10 §2): items with a `price`
    in the catalog, on their own Store tab once the Wardrobe is open. Milestone and prestige items are never sold
    (tested). Prices are fixed, never tied to the level (doc 10 §11);
  - the profile avatar on Home (frame and badge; it opens the Wardrobe's Profile tab), the marker on the Home rank
    button, and the frame, badge and marker on the player's own leaderboard row. Other players' decorations need the
    server (deferred);
  - workers draw the outfit through `BloomlingFigure`, hop along their route, face the way they walk, and carry their
    variant icon in a contrasting ink (doc 12 §6 moving-character test).
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

- [ ] T147 [P] Implement `client/Assets/Bloomlings/Services/Analytics/IAnalyticsService.cs`,
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
  Open (Audit 2026-09-29): the integration has never been compiled or run. Its package is not in `client/Packages/manifest.json`, so every build uses the offline fallback. It closes after the first Unity build with the package.
- [ ] T148 [P] Move all player-facing strings into Unity Localization tables under
  `client/Assets/Bloomlings/UI/Localization/`, starting with the English table (R18).
  Status: every player-facing string is looked up by key through `Loc`:
  - the English source is `UI/Localization/Resources/Strings_en.csv` (145 keys, including cosmetic names);
  - at runtime the selected locale's Unity Localization table `UI` is checked first, then English;
  - **Tools/Bloomlings/Localization/Import English Strings** builds the Unity string table collection from the CSV;
  - `LocalizationTests` fails on any literal left in the UI and on any key missing from the table.

  Open: the Unity table assets are created by that menu, which has not been run yet (Editor step). The Unity
  Localization code compiles only with the package (`BLOOMLINGS_LOCALIZATION`) and has not been compiled against it yet.
  (Audit 2026-09-29: unchecked. Today the strings come only from the CSV through `Resources`; the Localization package is not in
  `manifest.json`, and no Unity table exists.)
- [ ] T149 [P] Run the accessibility pass and write the results to
  `specs/001-core-game-mvp/checklists/accessibility.md`:
  - run the readability tool (T075) on every variant pair that can share a level;
  - take colorblind-simulation screenshots of 6-variant boards;
  - check pod counts at the smallest supported screen (FR-072, SC-003);
  - run the manual FR-005 checks for every approved pair: tell the two variants apart on a pod in the tray, in a
    Waiting Slot, and on moving Bloomling characters, at 1× and 2× speed.
  Status: open (Editor, device and person steps). The automated part is recorded in `checklists/accessibility.md`:
  - the readability tool, re-run, reproduces `pairs-report.json`;
  - all 66 variant pairs have distinct icons and ΔE2000 ≥ 10 under every simulated vision;
  - 7 grayscale-flagged pairs are listed for the icon test;
  - cosmetic and theme contrast are tested.

  Still needed: the colorblind-simulation screenshots, the smallest-screen check, and the per-pair manual FR-005 table.
- [ ] T150 Profile and optimize on the reference low-end devices (R16) and record the results in
  `specs/001-core-game-mvp/checklists/performance.md`:
  - sprite atlases and pooled workers (T046);
  - backlog compression tuning (T045);
  - targets: a 30 fps floor, no hitches over 100 ms, level load ≤ 1 s and cold start ≤ 5 s.
  Status: open (devices). `checklists/performance.md` lists the targets, what is already in place (worker pool,
  backlog compression, input never waiting, off-thread content parsing) and the measurement table for the reference
  devices.
- [ ] T151 Create the IL2CPP device determinism check: scene `client/Assets/Bloomlings/Tests/Device/RunGoldenReplays.unity`
  and `RunGoldenReplays.cs`, which run the golden corpus on Android and iOS builds and report hashes (SC-005,
  SC-011).
  Status: `RunGoldenReplays.cs` (own assembly `Bloomlings.Client.DeviceTests`) and `GoldenReport.cs` are done;
  `GoldenReportTests` cover the corpus digest and the result line. The scene is generated by **Tools/Bloomlings/Device
  Tests/Prepare Golden Replays** (the repository does not track `.meta` files, so a hand-written `.unity` could not
  reference the script). **Build Golden Replays (Android/iOS)** builds it with IL2CPP. Release builds leave the corpus
  out. Not yet run on devices.
  (Audit 2026-09-29: unchecked. The scene has never been generated, built or run.)
- [ ] T152 [P] Create `.github/workflows/unity-build.yml`, which runs GameCI `unity-builder` for Android and iOS
  (Unity licence secrets) plus the Unity EditMode tests (`unity-test-runner`).
  Status: replaced by `.github/workflows/unity-apk.yml` at the product owner's request (2026-09-29). It builds
  the Android APK (`game` or `golden-replays`) with GameCI and Unity 6000.3.25f1. It is manual only
  (`workflow_dispatch`), so no push, pull request or schedule spends Actions minutes. It keeps only the newest APK
  artifact (older ones are deleted after each successful upload, 7-day expiry). `CiBuild` performs the first-open
  steps (TMP essentials, Android player settings, scenes, content import). iOS builds and the Unity EditMode test job
  were dropped to save minutes; the engine-free EditMode tests still run in `core-tests.yml` through
  `client/DotnetCheck`. Not yet run: it needs the Unity licence secrets.
  (Audit 2026-09-29: unchecked until the first successful run. No CI job compiles the client in Unity today.)

  Also at the product owner's request, `.github/workflows/android-apk.yml` builds a temporary playtest APK without
  Unity or secrets. The app is `playtest/android`, .NET for Android on the shared core, with Levels 1–94. It is also
  manual only and keeps only the newest artifact. It is not the product client (see `playtest/README.md`).
- [ ] T153 Produce the launch catalog in `content/catalog/`, publishing to `build/content/` and then
  `client/Assets/StreamingAssets/content/`:
  - run `generate` for Levels 101–5000+ with every band profile;
  - run `validate` and `score` (cadence: from L11, 15–25 Hard and 6–10 Super Hard per 100 levels);
  - human sampling per the FR-084 tiers, with stronger review for milestone, Hard and Super Hard levels;
  - run `daily generate`;
  - run `publish --content-version 1`, then import into StreamingAssets (T091).

  SC-004 and SC-012 must pass.

  Status: open. It needs:
  - approved pictures (T094);
  - a larger picture library: the draft library already runs out at L95 under the no-repeat-within-50 rule (T095);
  - the human sampling tiers.

  The pipeline side is ready. `daily generate` and `publish` with a daily pool were exercised on 2026-09-29 (3 daily
  entries, 3 packs). Generation takes several hours of solver time per 100 levels in this environment, so producing
  5000+ levels needs a dedicated, parallelized run once the pictures exist.
  Update 2026-10-06: the pictures exist (T176), and `tools/catalog/build-catalog.ps1` / `.sh` (README there) build the
  catalog band by band with fixed segments (`generate --segments`, T179), up to two more seeds per level without an
  accepted candidate, each band validated in context, a manifest line per band, resumable, and a `-Check` that
  regenerates a band to compare it file by file. Measured here: about 100 s per level on average (a Super Hard level
  10–20 min), about 145 core-hours for L11–5000 before the engine speed-up. Levels 1–50 are built and validated here
  (`content/catalog/README.md`); the owner builds L51–5000 on his PC once the speed-up is merged, then `score`, `daily
  generate` and `publish` follow.
- [ ] T154 [P] Run an originality review and record it in `specs/001-core-game-mvp/checklists/originality.md`. Confirm
  that no Colony Flow name, characters, art, audio, level pictures or UI graphics appear in `client/Assets/` or
  `content/pictures/`, and that every picture's `source.licence` is owned or licensed (FR-091).
  Status: open (human review). Automated checks pass and run in CI (`OriginalityTests`):
  - no reference-game name in the client or the pictures;
  - no imported art, audio or fonts in `client/Assets/`;
  - every picture is `owned`.

  Recorded in `checklists/originality.md`. Still needed: a person compares the pictures with the reference game's
  levels, and legal signs off on the name and store listing.
- [ ] T155 Run the playtests from quickstart §8 with at least 30 new players and at least 15 for the glance and
  recognition tests. Record SC-001, SC-002, SC-003, SC-006, SC-007, SC-010, SC-014 and SC-015 in
  `specs/001-core-game-mvp/checklists/playtest-results.md`.
  Status: open (players). The results template is `checklists/playtest-results.md`.
- [ ] T156 Run the full `specs/001-core-game-mvp/quickstart.md` validation, sections 1–8. Update `CLAUDE.md` and
  `README.md` with the final build, test and pipeline commands.
  Status: open. Sections 1–4 were run on 2026-09-29 and pass on the committed content; the details are in
  `checklists/quickstart-validation.md`. Sections 5–8 need Unity, devices, live services and players. `CLAUDE.md`,
  the root `README.md` and `client/README.md` list the final build, test, pipeline, localization, device-test and CI
  commands.
- [X] T157 Placeholder visuals and animations pass (docs 11 and 12, FR-015, FR-025, FR-031, FR-036–FR-039, FR-070,
  FR-071), added 2026-09-29 after the visual audit:
  - slots: a committed pod flies from the tray and pops in; a mystery pod shows "?" and flips over (the commit used to
    pass the revealed variant, so the flip never showed); counts bump as work lands; a finished pod puffs away before
    a queued one moves in; a waiting pod shows an hourglass; the last free slot shows a "!" with its outline (never
    color alone); on a jam the occupied slots shake; the sixth slot carries a plus and pops in; an unlocked slot
    returns to the empty color;
  - keys: a pod or slot keeps its lock, and a key door stays shut, until the flying key lands (the rules open them
    at once, so the lock used to vanish first);
  - tray: a "+N" under deep stacks; link bars over the gap between cards, one color per connected group; Shuffle
    spins the cards; Return flies the pod back;
  - board: a restored tile shrinks away with a small sparkle; a revealed layer or mystery tile flips in; Bloom Burst
    bursts each tile of the variant and its targets breathe while it waits for a tap; the cells that count toward a
    special are outlined in its color; ClearRegion shows a dashed-square icon; Chest, Statue and Bridge have their own
    placeholder sprites and triggers (chest pops open, statue glows away, bridge is repaired and stays, the Fountain
    sprays droplets);
  - win: the finished picture shines, confetti falls, and one Bloomling per variant hops below the board; a milestone
    adds a ribbon and more confetti;
  - workers: hop along the route, face the way they walk, carry their variant icon, wear the outfit (T143);
  - icons and counts on tiles, pods and slots use a contrasting ink (dark on Leaf, Flower, Dew), and the bespoke
    `Tile` and `PodSkin` art of the visual catalog is used when set;
  - demos: Chest, Statue/Bridge and Connected Triple; a new variant from a pool expansion is shown beside its family
    once; the sibling demo now animates the pod turning back at the cross; Home demonstrates the Leaderboard, Store,
    first milestone, Wardrobe, Daily Challenge and theme rotation on their buttons once (a test keeps every roadmap
    mechanic, booster and Home system covered);
  - the theme accent colors the play-area band; the Collection keeps each picture's proportions; the 2× toggle in the
    HUD becomes the saved default.
  Status: code done and compiled against the Unity API stubs; none of it has been seen in the Unity Editor yet.
- [X] T158 Late roadmap mechanics and progression rules (spec roadmap, FR-031, FR-035, FR-039, FR-060), added
  2026-09-29:
  - Chest (L150, optional): a sealed special that restores all adjacent layers and then removes the stones near it;
    Statue or Bridge (L250 `environment_2`, optional): a Statue opens when a region is restored, a Bridge when N tiles
    of an exact variant next to it are restored. All three use the data-model special condition and effect; these
    conditions are chosen defaults (the docs name the objects, not their rules) and need the product owner's review;
  - Connected Triple (L400, optional, Hard and Super Hard only): three pods at the same depth commit together and need
    three free slots (`LevelMechanics` maps a group of 3+ to it; a test covers the commit rule);
  - the Level 8 choice: `UnlockRoadmap.MysteryPodAtLevel8` (CLI `--level8 mystery_pod`) moves the Key to L14; the Key
    stays the default until the fairness review of Mystery Pod levels;
  - the variant pool: `VariantPool.Default` adds Vine at `variant.pool_expansion_1` (L45) and Berry at
    `variant.pool_expansion_2` (L200), a proposal (the spec leaves the order open). No picture in the library has a
    lime, red, indigo or gold role yet, so the generator cannot introduce them; the validator warns;
  - practice levels (FR-031 showcase → practice → combination): the level after each showcase uses its mechanic again,
    alone; the Key's practice is the roadmap row at L14; the triple's is the first Hard or Super Hard level after L400.
    The generator places them, rejects a practice candidate that cannot carry its mechanic, and the validator checks
    them;
  - `generate`, `validate` and `publish` take `--level8`.

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

## Owner review: walkers set off on their own; auto 2× (2026-10-04)

- [X] T159 Each Bloomling sets off as soon as its own route and target are clear, not with its whole wave; the wave
  ends after its last arrival, its end events in the rules' order (`playtest/android/LevelAnimator.cs`,
  `playtest/design/BoardPainter.cs`, `playtest/android/TesterView.cs`,
  `client/Assets/Bloomlings/Gameplay/Timeline/TimelinePlayer.cs`); `playtest/check` checks the owner's L1 (the leaf pod
  sets off while the first water pod works) and `EventTimelineTests.EachWalker_SetsOffWhenItsOwnRouteIsClear` (research R4).
- [X] T160 While no exposed pod can be tapped the animation plays at 2× and the speed pill shows it, the saved setting
  unchanged (`playtest/design/LevelScreen.cs` `RefreshSpeed`, `client/Assets/Bloomlings/Gameplay/GameplayController.cs`
  `RefreshSpeed`, `client/Assets/Bloomlings/UI/Screens/GameplayHud.cs` `ShowAutoSpeed`; FR-069).

## Owner review: slower clearing, bigger boards, taps wait for a free slot (2026-10-05)

- [X] T161 The clearing pace at 1× halved again: 0.36 s a step in the playtest, 0.28 s in Unity, waves of 1.2–6.4 s and
  1.2–5.6 s; the backlog speed-up from 12 s (`fx.backlogThresholdMs` 12000, 2000–20000)
  (`playtest/android/LevelAnimator.cs`, `client/Assets/Bloomlings/Gameplay/Timeline/TimelinePlayer.cs`,
  `client/Assets/Bloomlings/Services/Config/RemoteConfigKeys.cs`, `backend/remote-config/defaults.json`; research R4).
- [X] T162 Bigger boards from Level 1 (FR-008 and the Level Band Guidelines as amended): 11×12–12×12 at L1–10 growing to
  13×14–14×16 at L51–100, never under 11×12 (`core/src/Bloomlings.Generator/Profiles/BandGuidelines.cs`, the FR-008
  check of `CatalogValidator`, `content/profiles/*.json`); the ten curated pictures redrawn and Levels 1–10 rebuilt on
  them with the same lessons (L2's blocked entry, L5's pot, L8's key, L10's flower bed), version 2, their golden cases
  rebuilt; the sketched library at the new sizes (T094); the showcases and the playtest's Levels 11–100 regenerated
  (T095); the tests follow (`ContentFolderTests`, `BandGuidelinesTests`, `GeneratorTests`, `ProgressionRulesTests`,
  `GuideTourTests`).
- [X] T163 A pod tap goes in only when a slot shows no pod on screen, one per pod of a connected group; before that it
  gets the no-free-slot feedback and the rules never see it (FR-014 as amended; `playtest/design/LevelScreen.cs`
  `Tap`, `LevelAnimator.FreeOnScreen`, `client/Assets/Bloomlings/Gameplay/GameplayController.cs` `OnPodTapped`,
  `SlotRowView.FreeOnScreen`); `playtest/check` checks that right after L1's first tap the rules have five slots free and
  the screen four.
- [ ] T164 The owner looks at the ten redrawn curated pictures (approved provisionally, as before, "pending the owner's
  look and playtest") and playtests the new Levels 1–10 and the new pace (FR-084, SC-001: Level 1 within 2 minutes).
- [X] T165 Booster prices as in the reference game (the owner, 2026-10-05): Extra Slot and Shuffle 1200 Petals, Return
  1500, Bloom Burst 1800 (Remote Config, `backend-services.md`; they were 40 / 40 / 50 / 60); the Store's names keep
  clear of the wider price pills (both builds); the economy tests and preview frames 10, 14, 17 at the new prices.
- [X] T166 The profile's avatars as profile cosmetics (FR-063 as amended; spec 005 FR-037, its tasks T113–T118): four
  free, ten bought once for 300 / 600 / 1200 Petals (Remote Config `economy.price.avatar*`), the save's `profile`
  section and `cosmetics.equipped.profile.avatar` (both schema copies, data-model §3.1, the merge).
- [ ] T167 The owner: set the Petal packs' sizes for the new booster prices (120 / 400 / 1000 now buy less than one
  booster, and a clearing style costs 5000) with the server work (`ProductCatalog.json`,
  `backend/cloud-code/ValidatePurchase.js`). Deferred by the owner (2026-10-06) until purchases come in as a whole.
- [X] T168 The board's clearing styles as board cosmetics (FR-063 and FR-051 as amended, research R4 amendment of
  2026-10-06; spec 005 FR-038, its tasks T119–T124): Blossom and Munchers free by level, five styles for 5000 Petals
  each from L40 (Remote Config `economy.price.clearing`) in the Store's Animations tab with live previews from L12;
  the save's `clear.<name>` in `cosmetics.owned` and `cosmetics.equipped.board.clearing` (both schema copies,
  data-model §3.1, the merge); one trip time for every style, a line of Bloomlings from each arch, rounds not waiting
  for each other, the backlog speed-up at 60 s (`fx.backlogThresholdMs` 60000), both builds.

## Owner review: regular and big boards, the board look in the level data (2026-10-06)

- [X] T169 Board limits 22×28 (FR-008 as amended): `CellPos.MaxWidth`/`MaxHeight` 22/28, `BasePicture`'s limits follow,
  `BoardBuilder`'s message, both copies of `level-definition.schema.json` (cells x ≤ 21, y ≤ 27) and
  `base-picture.schema.json` (width ≤ 22, height ≤ 28), `PictureValidator`; the hash audit (research R3: nothing
  iterates in hash order) and every golden replay byte-identical, never regenerated; the timelines' wave cap 40 → 60 s
  so the 22×28 board's longest straight trip is not squeezed (`TimelinePlayer`, `LevelAnimator`, research R4).
- [X] T170 The board rule by level (Level Band Guidelines as amended): `BandGuidelines` `BoardRule` (widths, heights,
  cells), `RegularBoard` 224–288 cells from L11, `BigBoard` 289–616 cells, `IsBigLevel` (every milestone from L525),
  `For`/`Board`, the new pods, work and durations and the big levels' row; `PicturePicker` picks big pictures for big
  levels only; `CatalogValidator`'s band check; `content/profiles/band-*.json` (regular sizes, the bands from L501 up to
  22×28); big levels' own thresholds (`difficulty-thresholds.json` `big`, `ProfileLoader.ReadBigThresholds` /
  `ReadThresholdsFor`, the generator's `BigLevelThresholds`, `generate` and `score`); big levels are Normal
  (`DifficultySchedule` moves a Hard due on one to the next level: the tuner cannot make a big board Hard).
- [X] T171 The board look in the level data (FR-036 as amended): `BoardLook`, `BoardLooks` (`MaxPeekCells` 288),
  `LevelDefinition.BoardLook` (optional; absent peeks), `DefinitionJson` reads and writes `boardLook` (a stated `peek`
  is kept), the schema and data-model §1.3, the generator always writes it (`gen-1.3.0`), `CatalogValidator`'s board
  check fails a look that disagrees with the cell count, `LevelView.BoardLook` and `CellInfo.Next` null on an icons
  board.
- [X] T172 Hidden-layer fairness on icons boards (research R8b): `HiddenLayerFairness` (12 sampled worlds of the same
  hidden layers, a visible-information player that plans on model worlds and replans on surprises, fixed budgets),
  `FairnessChecker` (`Uncovered` for mystery on an icons board, `OverCap` over 72 hidden layers), the generator's
  acceptance and slack (`OverlayPlanner` 4–9% layered tiles on icons boards, no mystery, big levels' lower buffer
  pressure), `CatalogValidator`'s `player-info-fair`.
- [X] T173 Both builds skip the next-layer chip on an icons board (`playtest/design/BoardPainter.cs`,
  `playtest/android/TesterView.cs`, `client/Assets/Bloomlings/Gameplay/Board/BoardView.cs`); a scratch render of a
  generated 22×28 big level through the full playtest's screens at the preview's three phone shapes (no chip, no render
  check problem; cells about 3% of the width); `ReferenceLayoutTests` fit 14×16, 16×18 and 22×28.
- [X] T174 Tests: `HiddenLayerFairnessTests` (Solver), `BigLevelTests` with a generated 22×28 big level and a blind-guess
  level as fixtures (`core/tests/Bloomlings.Generator.Tests/Fixtures/`), `BandGuidelinesTests`, `GeneratorTests`,
  `DefinitionJsonTests` (the look, 22×28 coordinates, the embedded schemas equal the contracts), `BoardBuilderTests`.
- [X] T175 The showcases on the new boards: L251 (Statue/Bridge practice) and L400 (connected triple showcase)
  regenerated on approved 14×16 pictures with the documented `generate` commands (L150, L250 and L401 already stood on
  14×16 boards and pass); L151 (chest practice, Super Hard) moves to T177, with the levels the new pictures need.
- [X] T176 The picture library for the new boards: every level from L11 needs a picture of 224–288 cells (14–16 ×
  16–18), and the 180 big levels (L525–5000, every 25th) pictures of 289–616 cells up to 22×28. Drawn in another
  session and merged: 400 approved regular pictures (224–288 cells) and 97 approved big ones (340–616 cells), beside the
  111 onboarding-size ones (`content/pictures/lib`, approved by the automated picture checks).
- [X] T177 With those pictures: L151 and the showcase and practice levels L11–91 (17 levels, still on 12×12–14×15 boards; FR-083
  wants 17 distinct pictures in Levels 1–100 while the library has 12 of 224+ cells) and the playtest's Levels 11–100
  regenerated on regular boards; then the catalog (`generate --jobs N`), whose big levels take minutes each.
  Status (2026-10-06): the 17 showcase and practice levels L11–91 regenerated with `gen-1.3.0`, seed 1, in level order on
  approved 224–288-cell pictures, all Normal; L151 regenerated after T180 (Hard 2367, rabbit_04 16×16);
  `validate --catalog content/showcase --context content/curated` 0 errors (`content/showcase/README.md`). Timing on
  this machine (one core per process): about 20 s for a Normal or Hard level, 10–20 min for a Super Hard one (most of its
  60 candidates fail the tuner), 25 s for a big level; `validate` 0.5 s per level. Status (2026-10-07): with the faster
  search (T182) and the expansion pictures (T183), `tools/catalog/` built Levels 1–100 here (L51–100 in 106 s on 3
  threads, 0 errors), and the playtest's Levels 11–100 are copied from the catalog; L101–5000 follow on the owner's PC
  (T153).
- [ ] T178 The owner: confirm the "rare" default (every milestone level from L525, always Normal), whether "icons only" should also
  drop the candy tile behind the icon (today: the candy tiles stay and only the chip goes), the big levels' pods (24–56)
  and thresholds (`big`, 1500 over the band's) after playing big levels, and a glance test on a 22×28 board (SC-003
  names 14×16).
- [X] T179 `generate --segments N` (pipeline, contracts/pipeline-cli.md): the range is cut into N contiguous segments
  (at most one per 50 levels; default `--jobs`, the behaviour before) generated in parallel on `--jobs` threads, so the
  levels depend on the segments and never on the machine's cores (`GenerateCommand.Segments`, `GenerateRange`;
  `GeneratorTests.Segments_FixTheLevels_WhateverTheThreads`). The tray tuner's rejection names the score it reached
  (`tray:class-normal-not-superhard:score-1776`; `GeneratorTests.TrayRejections_NameTheScoreTheTuningReached`).
- [X] T180 Practice levels are never Super Hard (the owner, 2026-10-06; FR-059 and the Level Band Guidelines as amended):
  with the roadmap, `DifficultySchedule` moves a Super Hard due on a practice level (FR-031) to the nearest later level
  that is not a showcase, a practice or a milestone level, whose next level is Normal and that follows no Super Hard, and
  the practice level takes that level's class; the rest of the schedule is unchanged (with the catalog's seed only L151,
  now Hard, and L153, now Super Hard). `generate` builds its schedule with the roadmap; `CatalogValidator` refuses a
  practice level stored as Super Hard (check `practice`). The thresholds stay (`difficulty-thresholds.json` note: the
  mixed levels' tuned scores are bimodal, the single-mechanic practice tops out at about 2550). Tests:
  `GeneratorTests.Schedule_MovesASuperHardOffAPracticeLevel_ToTheNextFreeLevel`,
  `Schedule_NeverPutsSuperHardOnAPracticeLevel_AndKeepsItsRules`,
  `TheCatalogSchedule_Holds15To25HardAnd6To10SuperHard_InEveryBlockOf100`,
  `ProgressionRulesTests.APracticeLevel_IsNeverSuperHard_ForTheValidator`. L151 regenerated on it (T177).
- [X] T181 The seam repair of `generate` redoes one level per clash (pipeline, contracts/pipeline-cli.md): seam by seam
  in level order, the later segment's first levels that break a repetition rule with the levels before them are
  generated again between their neighbours, and the earlier segment keeps its levels; it regenerated both levels of a
  clashing pair before, and with segments under 100 levels a level could be listed twice. A last pass over every seam
  level against both sides stays as a safety net. Test: `GeneratorTests.SeamRepair_RedoesOnlyTheLaterLevel_AndKeepsTheEarlierSegment`
  (it fails on the old repair). No multi-segment band had been generated yet.
- [X] T182 Faster generation, the same levels (the owner, 2026-10-06): a solver step is
  `LevelSession.SearchChild` (no events or command log; one reused session per depth), the board keeps its
  reachability and updates it as cells open, and states copy less (byte cells, shared tables, packed targets);
  rules, profiles and Apply's events are unchanged. About 10× on one core (`generate --jobs 1 --seed 1`: L20–22
  746 → 75 s, L1001–1003 949 → 84 s, L550 21.5 → 2.2 s, L1004–1013 1061 → 110 s, L2001–2010 576 → 58 s), so
  L1001–5000 at `--jobs 3` take about 3 h instead of about 35 h. Proven identical against the code before: golden
  replays unchanged, all those batches and mechanic and big-level ones (definitions, validation records,
  rejections, logs) byte for byte, a solver dump of 759 level and tray variants equal, and
  `SearchPathProperties` testing the fast paths against the ordinary ones.
- [X] T183 Vine and Berry can be drawn (the owner, 2026-10-07; FR-084 as amended): the automated picture checks accept a
  role whose color group has an expansion variant of the pool (`PictureChecks.HasPoolVariant`, `VariantPool.Expansions`),
  and `PicturePicker.Candidates` keeps a picture out of a level until every role's group has a variant there
  (`PicturePicker.Drawable`, `LevelGenerator` passing `VariantPool.ExpansionsAt`; no expansions, as for the Daily pool,
  keeps them all out), so the levels before a variant joins are unchanged. 439 pictures from
  `content/pictures/tools/sketch_pictures.py --expansions` (`expansions.py`): 178 regular with a lime role, 164 with a
  red one, 97 big (89 lime, 82 red), all approved on import; band 26–50 regenerated (L26–44 unchanged, L45 introduces
  Vine; L11–25 checked identical). Test: `BigLevelTests.ExpansionPictures_AreApproved_AndKeptOutUntilTheirVariantJoins`.
- [X] T184 Far picture reuse in the generator (the owner's catalog run of 2026-10-07: L437 reused rainbow_08 from L326
  with the same look or Source design, two segments apart, and `validate` stopped band 0251-0500): `LevelGenerator`
  refuses a candidate whose picture repeats an earlier use beyond 50 levels with the same mirroring and mapping
  (`similarity:reuse-same-look`) or the same Source design (`similarity:reuse-same-source`), FR-083 as the validator
  judges it (`FarReuses`, `SameLook`, shared with `CatalogValidator`); `Conflicts` reports it too, and
  `GenerateCommand.GenerateRange` ends with a pass over the whole range in level order that generates the later level of
  such a pair again (or the earlier one when the later is kept). Bands that passed `validate` come out the same: each
  attempt has its own seed, so the new refusal only changes a level that broke the rule. Test:
  `CatalogRulesTests.APictureUsedAgainFarAway_MustDifferInLookAndSource_AtTheLaterLevel`. *(2026-10-07, the owner's run
  of band 2001-5000: L3063 repeated L2741's alarm_clock_09.)* A candidate is now judged against later uses of its
  picture too (`LevelGenerator.ReusesLook`, `ReusesSource`). A gap filled after its band, or a seam repair, sees the
  later levels, and a repeat of one of them broke the rule there. Test:
  `CatalogRulesTests.ALevelGeneratedBeforeAKnownLaterLevel_MustNotRepeatItsLookOrSourceEither`.
- [X] T185 The Daily Challenge pool (R19, FR-064 as amended on 2026-10-07; the owner: "a pool for 365 days", "it must
  definitely be a new picture", "the board must be big with small cells … always maximal", "they can be medium, hard,
  super hard"): `content/daily/`, 365 entries from `daily generate --seed 1 --count 365 --segments 7 --jobs 4`, with
  validation records and a README.
  - The Daily Challenge's own pictures are 128 new subjects that the levels never draw, three each at 22×28
    (`sketch_pictures.py --daily`; `daily_subjects.py`, `daily_animals.py`, `daily_places.py`, `daily_things.py` and
    `daily_food.py`; `preview_png.py` renders their sheets). They carry the theme `daily`
    (`PicturePicker.DailyTheme`), which only a profile asking for it takes; the `daily` profile takes only them.
  - `DailyPlan` gives each entry a picture of its own, a subject only every 60 entries, and the class of its weekday:
    Hard on Wednesday and Saturday, Super Hard on Sunday (entry 1 is Thursday 2026-01-01), the others Normal.
    `daily generate --class weekly` is the default.
  - Every entry plays with the unlocks of L50 (`LevelGenerator.RulesLevel`, `CatalogValidator.RulesLevel`), on the
    icons board's buffer pressure.
  - An entry without an accepted candidate takes a spare picture.
  - `validate` and the publish gate refuse a daily picture in a level (`picture-pool`), and in the pool a picture shown
    twice (`picture-once`) or a subject within 60 entries (`subject-window`).
  - Tests: `DailyPoolTests`.
  - `publish --daily content/daily` packs it.
  - Built on 2026-10-07. The 365 entries are 209 Normal, 104 Hard and 52 Super Hard. Two runs, before and after the
    mechanics fix (T189), gave the same files. The whole release gate passed: `publish --catalog content/catalog --daily
    content/daily` gave 5000 levels, 1936 pictures and 365 daily entries in 22 packs (1.4 MB of gzipped level packs).
- [X] T186 A second set of subjects for the levels. The owner, 2026-10-07: "we need more subjects for the 5000 levels,
  spread over all of them; I can regenerate the 5000".
  - 100 subjects that neither the levels nor the Daily Challenge drew, in `more_animals.py`, `more_nature.py`,
    `more_things.py` and `more_food.py` (`more_subjects.py`).
  - Each subject gets four regular pictures and one big one, with `--expansions` its lime and red pictures too.
    `sketch_pictures.py` draws them after every earlier band, with a random stream of their own, so the 1034 earlier
    sketches stay byte-identical.
  - `--more --module more_<group>` checks a module at every regular and big size and writes its review sheets.
  - A drawing that misses its targets at the production seed is drawn again from up to 8 seeds derived from it (after
    the expansion size tries), so every subject gets its pictures and the earlier ones stay the same.
  - Imported: 892 pictures, all approved (700 regular across the 9 sizes of 224–288 cells and 192 big across the 8
    sizes of 289–616 cells). The levels' library is now 1442 regular and 386 big pictures. The regeneration is T188.
- [X] T187 Boards of every size from L11 (FR-008 as amended on 2026-10-07; the owner: "I want them to come often …
  from 10+ … all sizes"; the answers "all sizes equally", "yes, thresholds by size", "all 5000" in the playtest APK).
  - Every band row from L11 uses `BandGuidelines.AnyBoard` (14–22 × 16–28, 224–616 cells), and `PicturePicker.Pick`
    draws a size evenly before a picture of it. With one size, as in the Daily pool, it draws as before.
  - Pods, work and durations scale by cells / 288 above 288 cells (`BandGuidelines.For(level, cells)`,
    `Work(level, class, cells)`; the validator's band check uses the level's cells). The band profiles' maxima are
    scaled the same way (pods at most 56), with nesting up to 5 and a node budget of at least 400 000.
  - Big boards may be Hard or Super Hard: the milestone rule (`IsBigLevel`, `BigLevelsFrom`) is gone from
    `BandGuidelines` and `DifficultySchedule`. An icons board's peak is 1–3 slots when Normal and 3–4 otherwise
    (`BigBoardPeakSlots`).
  - Class thresholds: every band has `big` thresholds for 616 cells in `difficulty-thresholds.json`, calibrated on 22×28
    trials (research R8b, amended), and `BandGuidelines.ThresholdsFor` interpolates them with the band's own by cells
    for the generator (`LevelGenerator.BigBoardThresholds`) and `score`.
  - Tests: `BigLevelTests` (any class on any board, all 17 sizes, even size draws, thresholds by size, every band's big
    thresholds) and `BandGuidelinesTests` (scaled rows and profiles at 224–616 cells).
- [X] T189 The mechanics rule as the validator judges it (the owner's catalog run of 2026-10-07: band 2001-5000 stopped
  on L4987, "the same mechanics (mechanic.chest,mechanic.stone) 3 levels in a row (FR-083)").
  - The generator compared the planned mechanics lists, and checked them before a connected pair or triple, or a
    mystery pod, could be dropped for want of a place. The validator compares what each level's content uses
    (`LevelMechanics.UnlocksUsed`).
  - The generator now also judges the finished candidate on what it uses, against its neighbours on both sides
    (`LevelGenerator.MechanicsUsed`, which finds a level's picture with `PicturePicker.Find`; the rejection
    `similarity:mechanics-used-3-in-a-row`).
  - The seam repair judges it the same way (`Conflicts(..., mechanicsOf)`).
  - Bands that passed `validate` come out the same.
  - Test: `CatalogRulesTests.TheMechanicsRule_IsJudgedOnWhatTheLevelsUse_AsTheValidatorDoes`.
- [ ] T188 The owner regenerates the catalog L11–5000 with `tools/catalog` (T186, T187). Then validate it, copy all 5000
  levels into the playtest's levels (the owner's choice) and check that the playtest loads them. Build the APK only
  with the owner's OK.
  - Done on 2026-10-07 except the APK. The owner's build gave L1–5000 with one gap, L2068, which seed 6 filled.
    `validate` over the whole catalog gives 0 errors. Both APKs and the preview embed `content/catalog/levels` and read a
    level on first use (`ContentSet` with a level reader). `playtest/check` and the preview checks pass.
- [X] T191 At most 3 Source stacks on a Hard or Super Hard level (FR-011 as amended on 2026-10-08; the owner: "make 3
  [columns] at moments instead of 4. 4 are passed quite fast, even Super Hard", with the answer "Hard and Super Hard").
  - Before, every class drew its stacks from the band's range (2–6). The catalog's Hard and Super Hard levels had 4–6
    stacks in about half the cases (710 of 1351).
  - The generator draws a Hard or Super Hard level's stacks from `BandGuidelines.Stacks` (at most `HardMaxStacks` = 3,
    never under the profile's minimum). `validate` refuses such a level with more (`data-model`).
  - Trial on L230–233 (Hard) and L4200–4203 (Super Hard): every level still found a candidate, with fewer rejections
    (13 instead of 16, and 58 instead of 80).
  - The showcase L151 and L400 were generated again (3 stacks each). The Daily pool's Hard and Super Hard entries now
    have 3 stacks (its profile's minimum is 3).
  - Test: `BandGuidelinesTests.HardAndSuperHardLevels_HaveAtMostThreeStacks`.
- [X] T192 The owner regenerates the catalog L11–5000 with `tools/catalog` for T191. Until then `validate --catalog
  content/catalog` reports the Hard and Super Hard levels that still have 4–6 stacks.
  - On the owner's request (2026-10-08) both build scripts cut the last range into three build bands of 1000 levels,
    2001-3000, 3001-4000 and 4001-5000. Each has the profile `band-2001-5000`, seed 1 and 14 segments, so a band that
    stops costs 1000 levels instead of 3000. A level's seed depends only on the band's seed and the level number, so
    the cut changes nothing else.
  - Done on 2026-10-08. The owner's build gave L1–5000 with one gap, L4105, which seed 4 filled (Super Hard 3627).
    `validate` over the whole catalog gives 0 errors. Every Hard and Super Hard level has 2 or 3 stacks: Hard 454 and
    499, Super Hard 204 and 195. `playtest/check` passes, and the APKs read the catalog as it is.
- [ ] T190 Before the release, turn the Daily Challenge on (the owner, 2026-10-08). The steps are in
  `checklists/release.md`: publish the release content with `--daily content/daily` (the Unity APK workflow packs only
  `content/curated` today), keep `feature.dailyChallenge` on in the live Remote Config, and check it on a release build
  past L50.

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
- **Local values, Remote Config decided at the end** (product owner, 2026-09-29). FR-085 asks for rewards and store
  offers to be tunable remotely. These stay local for now, marked `REMOTE-CONFIG-DEFERRED` in the code; whether each
  becomes a Remote Config key is decided at the end:
  - the Daily Challenge reward, 30 Petals (`DailyChallengeService.RewardPetals`);
  - the milestone rewards (`MilestoneTable.Default`);
  - the store offers and grants (`Services/Purchases/Resources/ProductCatalog.json`, mirrored in
    `backend/cloud-code/ValidatePurchase.js`);
  - the free-booster ad cap and the daily-bonus cap, once per UTC day (`FreeBoosterAd`, `DailyRewardPopup`).

  Every other economy value, ad cadence and feature flag already is a Remote Config key
  (`backend/remote-config/defaults.json`).
