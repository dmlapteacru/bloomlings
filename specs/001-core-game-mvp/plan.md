# Implementation Plan: Bloomlings Launch Game (Colony Flow–style buffer puzzle)

**Branch**: `001-core-game-mvp` (development branch `claude/great-darwin-6qrpj8`) | **Date**: 2026-09-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-core-game-mvp/spec.md`

> **Development gate** (from `product/LOCKED_CONCEPT_v0.5.md`): this plan is design only. Full implementation starts
> after the product docs (`product/01`–`15`, v0.5 "draft for lock") are locked.

## Summary

Bloomlings is a strictly 2D, portrait mobile puzzle with Colony Flow's structure. The player commits numbered Spirit
Pods from a stacked Source Tray into 5 Waiting Slots. Bloomlings then clear reachable tiles of the pod's **exact target
variant**:

- 4 families and 8 launch variants;
- every level is a picture-first mosaic whose finished picture is revealed beneath the tiles;
- the level is lost when the slots jam.

The launch product covers:

- a linear sequence of **5000+ deterministic, solver-validated levels**;
- boosters, the Petals currency, the Store, ads and the Daily Reward;
- Leaderboard, milestones, Wardrobe, Daily Challenge and Collection;
- no lives.

**Technical approach** (per doc 15 and [research.md](research.md)):

1. **Deterministic rules core.** The rules live in pure C# libraries shared by the Unity client and the offline
   tools (R2). Each command settles to a fixpoint and returns an event log. The allocation is round-based and gives the
   oldest slot priority (R3).
2. **Presentation.** The Unity 6.3 LTS client (URP 2D, uGUI) is presentation only. It replays the event logs with a
   timeline scheduler that supports 2× speed and compresses a backlog (R4).
3. **Content.** Levels are compact JSON definitions. Each one references a base picture, a role→variant mapping and
   sparse overlays, and the client expands it deterministically. Versioned packs with a SHA-256 manifest are bundled
   for offline play, and later packs are delivered from a CDN (R5, R6).
4. **Content pipeline.** An offline **picture-first + solution-first generator** (R9) and a **solver**. The solver
   uses a DFS over settled states with a transposition table and gives winning traces, jam witnesses, fairness and
   metrics (R8). A CLI drives generate → validate → score → review → publish, and CI gates the catalog (R17).
5. **Services.** The services are Unity Gaming Services, Unity IAP, Google Mobile Ads with UMP/ATT, and Firebase
   Analytics/Crashlytics, all behind replaceable interfaces (R11–R14):
   - UGS: Auth, Cloud Save, Leaderboards with a time-encoded score, Remote Config, and Cloud Code for validation;
   - Google Mobile Ads with mediation, plus UMP/ATT for consent;
   - Firebase Analytics and Crashlytics.

## Technical Context

- **Language/Version**:
  - Rules and tools: C# 9 on netstandard2.1 for the shared libraries (R2).
  - Tools and tests: .NET 10 LTS.
  - Client: Unity 6.3 LTS (`6000.3.x`) with the IL2CPP backend for release builds.
- **Primary Dependencies**:
  - Rendering and UI: Unity URP (2D Renderer), uGUI, Unity Localization, Addressables (art DLC only).
  - Platform services: Unity IAP v5, UGS (Authentication, Cloud Save, Leaderboards, Remote Config, Cloud Code), Google
    Mobile Ads with mediation, Google UMP, Firebase Analytics and Crashlytics.
  - Testing: NUnit, FsCheck and the Unity Test Framework (R1, R11–R14, R17).
- **Storage**:
  - A local JSON save, atomic and with a schema version.
  - UGS Cloud Save for sync, with ledger-based merging (R15).
  - Content as gzip JSON-Lines packs with a manifest, in `StreamingAssets` and on a CDN (R5, R6).
  - The authoring repository keeps pictures, profiles, curated levels, the catalog and validation records under
    `content/`.
- **Testing**:
  - `dotnet test` for rules, property tests, the golden replay corpus, the solver and the generator.
  - Unity Test Framework EditMode and PlayMode tests.
  - IL2CPP device runs of the golden replays.
  - Pipeline validation of the full catalog in CI (R17).
- **Target Platform**: iOS 15+ and Android 8.0 (API 26)+, arm64, phones in portrait. Tablets are supported by scaling
  (R16).
- **Project Type**: a mobile game (Unity client), shared rules libraries, an offline content pipeline (.NET CLI) and
  managed backend services (UGS Cloud Code and config).
- **Performance Goals** (R16, R8):
  - 60 fps on mid and high devices, with a 30 fps floor and no hitches over 100 ms on reference low-end devices at the
    largest board;
  - tap feedback ≤ 0.1 s (SC-008);
  - level load ≤ 1 s, and cold start to Home ≤ 5 s on low-end devices;
  - solver median under 1 s and p99 under 30 s per level;
  - the full 5000-level catalog validated nightly in under 2 hours.
- **Constraints**:
  - The simulation is deterministic, with integer logic, no Unity APIs, no wall clock and node-count budgets (R3).
  - The game is offline-first (FR-074).
  - The board is at most 14×16, with 3–6 active variants typically.
  - Animated workers use a bounded pool of about 60 on low-end devices (R4).
  - Install size ≤ 150 MB and memory ≤ 350 MB.
  - Interstitial placement rules apply (FR-053).
  - Consent comes before personalized ads or analytics (FR-090).
  - No Colony Flow assets are used (FR-091).
- **Scale/Scope**:
  - 5000+ levels at launch, plus later content packs;
  - about 1000–1500 base pictures;
  - 8 variants, extensible to 12+;
  - 4 family rigs;
  - about 12 screens: Home, Gameplay, Pause, Win/Next, Jam/Recovery, Store, Settings, Leaderboard, Wardrobe, Daily
    Reward, Daily Challenge and Collection;
  - 7 user stories and 91 functional requirements.

No **NEEDS CLARIFICATION** remains. All technical unknowns were resolved in [research.md](research.md). Commercial and
balancing choices are listed there under *Deferred decisions* and are non-blocking.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Checked against the **Bloomlings Constitution v1.0.1** (`.specify/memory/constitution.md`, ratified 2026-09-29, amended 2026-09-29).

| Principle / section | How the design satisfies it | Pre-research | Post-design |
|---|---|---|---|
| **I. Colony Flow Structure, Bloomlings Identity** | Core loop kept as-is: tray → 5 slots → reachable exact-match work → jam, no timer, linear Level N (spec US1, US2). Identity comes from families and variants, picture-first levels and garden mechanics. FR-091 forbids Colony Flow assets | PASS | PASS |
| **II. Exact Matching on an Unambiguous Board** (NON-NEGOTIABLE) | Allocation is keyed by `VariantId` (R3, [simulation-api](contracts/simulation-api.md)). Family is presentation-only metadata ([data-model §1.1](data-model.md)). Each cell is a single tagged state with exactly one top layer ([data-model §2.1](data-model.md)). Variant readability pairs are enforced by the pipeline (FR-005) | PASS | PASS |
| **III. Deterministic Simulation** (NON-NEGOTIABLE) | One pure C# rules core shared by the client and all tools (R2). Settle-to-fixpoint model; integer-only logic; sorted collections; a specified seeded PRNG; node budgets (R3, R10). Presentation replays the event log (R4). A golden replay corpus is checked on .NET and IL2CPP (R17) | PASS | PASS |
| **IV. Validated, Fair Content** (NON-NEGOTIABLE) | Picture-first + solution-first generator (R9); hand-curated levels (L1–100 tier, showcases) start from approved pictures and pass the same validation. The solver produces a winning trace, a jam witness and a fairness check (R8). The FR-080 invariants run in PR and nightly CI; `publish` refuses failing catalogs, and `diff` refuses unversioned changes ([pipeline-cli](contracts/pipeline-cli.md)) | PASS | PASS |
| **V. Difficulty from Decisions, Not Grind** | No HP, timers, move limits or spawning exist in the model. Layers are distinct exact-variant work units. Difficulty is steered through Source design and structure (R9). Class cadence and waves are enforced by `score` (FR-059) | PASS | PASS |
| **VI. Fair Monetization, No Power Creep** | Boosters are one-shot commands with fixed semantics, and the core has no stat modifiers (FR-049). No lives (FR-040). AdPolicy enforces the interstitial and rewarded rules (R12, FR-052, FR-053). UMP/ATT consent comes before personalised data use (FR-090) | PASS | PASS |
| **VII. Simplicity, Readability and Offline-First** | uGUI screens follow the Home → Play → Level → Next flow with no map (FR-057). One-tap input. The bundled catalog and local save give offline play (R6, R15, FR-074) | PASS | PASS |
| **Technical and Content Constraints** | Unity 6.3 LTS for presentation only, with a netstandard2.1 core. Levels are versioned data with hash-verified packs. The backend is limited to meta functions (R11). Every vendor sits behind an interface. Remote Config cannot change rules or definitions | PASS | PASS |
| **Development Workflow and Quality Gates**: development gate | This command produces design only. `/speckit-tasks` must put a gate-check task first. Rule changes carry tests and golden-corpus updates; content changes pass CI validation (R17) | PASS for planning; implementation BLOCKED until the docs are locked | PASS for planning; implementation BLOCKED until the docs are locked |

Result: **no violations**. The development gate is a scheduling constraint, not a design violation.

## Project Structure

### Documentation (this feature)

```text
specs/001-core-game-mvp/
├── spec.md              # Feature specification (/speckit-specify)
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 decisions R1–R20
├── data-model.md        # Phase 1 entities, validation rules, state transitions
├── quickstart.md        # Phase 1 validation guide
├── contracts/           # Phase 1 interface contracts
│   ├── README.md
│   ├── level-definition.schema.json
│   ├── base-picture.schema.json
│   ├── content-manifest.schema.json
│   ├── player-save.schema.json
│   ├── simulation-api.md
│   ├── pipeline-cli.md
│   ├── backend-services.md
│   └── analytics-events.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (/speckit-tasks; NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
core/                                   # Pure C# (no UnityEngine); shared by client and tools (R2)
├── Bloomlings.sln
├── src/
│   ├── Bloomlings.Core/                # Rules: board expansion, reachability, allocation, mechanics, boosters, events
│   │   ├── Bloomlings.Core.csproj      #   netstandard2.1, C# 9
│   │   ├── Bloomlings.Core.asmdef      #   noEngineReferences: true (consumed by Unity via local UPM package)
│   │   └── package.json
│   ├── Bloomlings.Content/             # Level/picture/manifest models, JSON (de)serialization, schema checks, pack IO
│   ├── Bloomlings.Solver/              # DFS + transposition table, jam witness, fairness, metrics; also used by runtime Shuffle (R10)
│   ├── Bloomlings.Generator/           # Picture-first, solution-first generator (tools only)
│   └── Bloomlings.Pipeline/            # CLI: pictures import/validate, generate, solve, validate, score, review, publish, replay, diff, daily
└── tests/
    ├── Bloomlings.Core.Tests/          # Rule tests (doc 01 §20), FsCheck properties
    ├── Bloomlings.Content.Tests/       # Schema/serialization/expansion tests
    ├── Bloomlings.Solver.Tests/        # Known solvable/unsolvable/fairness cases
    ├── Bloomlings.Generator.Tests/     # Invariants, seed reproducibility
    └── golden/                         # Golden replay corpus (definitions + command logs + expected hashes)

client/                                 # Unity 6.3 LTS project
├── Packages/manifest.json              # references file:../../core/src/Bloomlings.Core (and .Content, .Solver)
├── Assets/Bloomlings/
│   ├── App/                            # Boot, flow (Home → Play → Level → Win/Next), unlock roadmap, milestones
│   ├── Gameplay/                       # Board view, tile/finished-picture rendering, Bloomling workers, tray, slots, event timeline (R4)
│   ├── Meta/                           # Store, Daily Reward/Challenge, Leaderboard, Wardrobe, Collection
│   ├── Services/                       # Save + cloud merge, content service, economy, AdPolicy, adapters (UGS, IAP, Ads, Firebase)
│   ├── UI/                             # uGUI screens, localization tables
│   ├── Art/                            # Family rigs, variant skins, tiles, pods, backgrounds (atlased)
│   └── Tests/{EditMode,PlayMode}/      # incl. golden replays in EditMode and device test scene
├── Assets/StreamingAssets/content/     # bundled manifest + packs (from `publish`)
└── ProjectSettings/                    # ProjectVersion.txt pins 6000.3.x

content/                                # Authoring data (see contracts/pipeline-cli.md)
├── pictures/{src,lib}/
├── profiles/
├── curated/                            # hand-authored levels 1–100
└── catalog/                            # accepted definitions + validation records

backend/
├── cloud-code/                         # ValidatePurchase, SubmitProgress, GetStarterPackOffer
└── remote-config/                      # default keys (backend-services.md)

.github/workflows/                      # core-tests, content-validate (PR), catalog-nightly, unity-build (GameCI)
```

**Structure Decision**: The repository is a monorepo with four roots:

- `core/`: pure C# rules and tools. It is the single source of truth for game rules, used by the client, the solver,
  the generator, the pipeline and the tests.
- `client/`: the Unity presentation and services.
- `content/`: the authoring data, validated like code.
- `backend/`: thin managed-service code and config.

The product docs stay in `product/` and the specs in `specs/`.

## Delivery Phasing (input for `/speckit-tasks`)

| Phase | Scope | Stories / requirements | Exit criteria |
|---|---|---|---|
| 0 | Gate check. Repo scaffolding (`core/`, `client/`, CI skeleton). The constitution is already ratified (v1.0.1) | Constitution: development gate | Docs locked; CI green on empty projects |
| 1 | Rules core + golden corpus + a minimal Unity board that plays hand-made levels | US1; FR-001 to FR-030 | Quickstart §1 and §5 (steps 1–6) pass |
| 2 | Content model, picture import, solver, validate/publish; curated levels 1–100 | US3 (part), US2; FR-075 to FR-084 | Quickstart §2 and §4 pass on L1–100 |
| 3 | Mechanics per the roadmap: keys, locks, connected pods, stones, layers, gates, Fountain, locked slot, mystery (fairness) | US4; FR-031 to FR-039 | Mechanic showcase levels pass; fairness solver in place |
| 4 | Boosters (with Shuffle R10), Petals, Jam recovery | US5; FR-040 to FR-050 | Booster scenarios pass; SC-005 holds with boosters |
| 5 | Generator + profiles for 101–5000+, similarity control, scoring and cadence | US3; FR-079, FR-082, FR-083, FR-059 | Full catalog passes nightly (SC-004, SC-012) |
| 6 | Store, IAP, ads + consent, Daily Reward, remote config, analytics, crash reporting | US6; FR-051 to FR-056, FR-085, FR-086, FR-089, FR-090 | Quickstart §7 monetization scenarios pass (SC-013) |
| 7 | Leaderboard, milestones, Wardrobe, Daily Challenge, Collection, themes, cloud save | US7; FR-057 to FR-066, FR-087, FR-088 | Quickstart §7 service scenarios pass |
| 8 | Performance, accessibility, localization, device QA, playtests | FR-067 to FR-074; SC-001 to SC-003, SC-006 to SC-010, SC-014, SC-015 | Targets from Technical Context met on reference devices |

## Complexity Tracking

> No constitution violations. The table records the one structural choice reviewers might question.

| Choice | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| Rules in separate pure C# libraries (`core/`) consumed by Unity through a local package | The solver, generator, pipeline and CI must run the exact same rules headlessly (Constitution III, IV) | Keeping the code inside Unity would need Editor batch mode for every validation and risk a second rules implementation in the tools |
