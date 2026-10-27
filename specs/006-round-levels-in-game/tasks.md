---

description: "Tasks for spec 006: round levels in the game"
---

# Tasks: Round Levels in the Game

**Input**: Design documents from `specs/006-round-levels-in-game/`:
- `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`;
- `contracts/`: `round-level.schema.json`, `round-picture.schema.json`, `round-proof.schema.json`,
  `conformance-fixture.md`, `pipeline-rounds-cli.md`, `presentation.md`.

**Tests**: Required by the spec and the constitution:
- conformance (FR-030–FR-033, SC-001, SC-002, SC-005);
- rule tests, golden replays and determinism checks for every rules change (constitution, Workflow gates);
- determinism with boosters (SC-007, SC-008).

Test tasks precede the implementation they cover.

**Organization**: grouped by user story (spec priorities). US1, US3 and US2 are P1, US4 is P2, US5 is P3.

The reference engine is `tools/rounds/engine/` (`rules.cjs`, `timeline.cjs`, `idle-rules.cjs`, `palette.cjs`,
`clock.cjs`). "Port" means the same behaviour and the same orderings as research R3, with model time as integer
microseconds (research R2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1–US5 from spec.md

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: tooling, governance and folders that every later task relies on.

- [X] T001 Install the .NET 10 SDK pinned by `core/global.json` in the session. Run `dotnet build core/Bloomlings.sln`
  and `dotnet test core/Bloomlings.sln`, and record the baseline result (pass counts) in
  `specs/006-round-levels-in-game/checklists/baseline.md`.
- [X] T002 Amend the constitution to 1.1.0 with `/speckit-constitution` in `.specify/memory/constitution.md`. The
  content was approved by the owner on 2026-10-27:
  - principle II: on board cubes a variant is identified by hue, readability-checked under normal vision (smallest
    CIEDE2000 ≥ 10), with a symbols mode for colour-vision deficiencies; icons are not required on cubes;
  - principle III: a command carries its model time as an integer (microseconds), and device time stays forbidden in
    rules.

  Then update the "Design invariants" summary in `CLAUDE.md` to match.
- [X] T003 [P] Add owner amendment notes (with spec 006 as the reason) to `product/01_CORE_GAMEPLAY_RULES.md`,
  `product/02_QUEUE_AND_BUFFER.md`, `product/03_TILE_SYSTEM.md` and `product/15_TECHNICAL_ARCHITECTURE.md`, plus a
  `product/CHANGELOG_v0.5.md` entry. The notes cover:
  - the live timeline (µs);
  - 16 materials;
  - round boards up to 48×48 with the frame ring;
  - hidden pods revealed on top;
  - the §8 link rule;
  - the Sprig bridge;
  - slot positions;
  - boosters in live time.
- [X] T004 [P] Create `content/game/rounds/` with `levels/`, `pictures/`, `proofs/`, `conformance/`, `export/` and
  `published/`, and a `README.md` describing each folder and linking `specs/006-round-levels-in-game/contracts/`.
- [X] T005 [P] Copy the three JSON schemas into `core/src/Bloomlings.Content/Schemas/` (`round-level.v1.json`,
  `round-picture.v1.json`, `round-proof.v1.json`) as embedded resources, as the existing schemas are.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the round materials, the level and picture models, the formats, and an export of L1–L20 that every story
reads.

**⚠️ CRITICAL**: no user story work can begin until this phase is complete.

- [X] T006 [P] Create `core/src/Bloomlings.Core/Rounds/RoundMaterial.cs`: the 16 materials in this order and with
  these data. Codes `GPBACDVYOLMRKNSE`; names Лист, Лепесток, Вода, Янтарь, Жемчуг, Земля, Слива, Солнце, Тыква,
  Лайм, Мох, Ягода, Румянец, Глубина, Небо, Кора; families sprig, bloom, drop, twig, drop, twig, bloom, sprig, twig,
  sprig, sprig, bloom, bloom, drop, drop, twig; normal colours #6DBE45 #EB799C #36B7DA #A45F28 #F5E6BD #334B48
  #7851A5 #F6CB3E #EF8A32 #CDEB5E #2F7D45 #D8394B #F9C6D6 #2C5AA8 #A8E2F5 #6A3A1E; accessible colours from
  `tools/rounds/engine/palette.cjs`; symbols ♧ ✿ ◆ ● ◇ ▰ ✦ ☀ ▲ ✚ ♣ ♥ ❀ ▼ ○ ■. Code index = channel index of
  `rules.cjs CHANNELS`.
- [X] T007 Create `core/src/Bloomlings.Core/Rounds/RoundLevel.cs`, the definition model of `data-model.md` RoundLevel:
  - number, definitionVersion, class, introduction `{mechanic, second}`, picture `{id, version}`;
  - board `{w, h, rowsBottomFirst, entries, hero?}`, capacity, pods `{code, count, mystery?, group?}`, tray, mechanics,
    exception?, engine fingerprints;
  - `MaxSide = 48`;
  - a prepared form with neighbours in the order `i-w` (row below), left, right, `i+w` (row above), the board as sbyte (open = -1,
    wall = -2, else code index), pod colours, the entries set, and groups. This mirrors `rules.cjs prepare`.
- [ ] T008 Create `core/src/Bloomlings.Core/Rounds/RoundLevelValidator.cs` (port of `rules.cjs validate` plus
  `data-model.md`). Errors:
  - "board 7–48 wide, 8–48 high";
  - row length and characters `[GPBACDVYOLMRKNSE.#]`;
  - "tray is a partition of the pods", 2–6 columns, capacity 5;
  - pod count 1–60;
  - "exact accounting: cells of each code equal the sum of counts";
  - "groups are pairs in different columns at equal start depth";
  - "hidden pods are at most 6, never in a group";
  - "bridge sites are walls with the anchor one row below";
  - mechanics match pods and hero;
  - an exception accepted only for the rule it names.
- [X] T009 [P] Create `core/src/Bloomlings.Content/Rounds/RoundJson.cs`: read and write `round-level.v1`,
  `round-picture.v1` and `round-proof.v1` per the schemas, validated against the embedded schemas from T005.
- [X] T010 [P] Create `core/tests/Bloomlings.Content.Tests/Rounds/RoundJsonTests.cs`: round-trip each format, schema
  rejections (bad code, 49-wide board, capacity 4, count 61, missing proof field), and the validator's errors from
  T008.
- [X] T011 Create `tools/rounds/export.mjs`, part 1 (contract `pipeline-rounds-cli.md`).
  - **Entry checks**, each failure named, exit 1:
    - every position from–to placed;
    - `audit.mjs` has no НЕТ, except for a level's named exception;
    - pictures approved and not franchise;
    - `content/rounds/runs/<wave>/confirmed.json` exists.
  - **Writes**: `levels/L####.json`, `pictures/P####.json` and `proofs/L####.json` in the v1 formats, plus
    `export.json`.
  - **Solution commands**: from the reference's patient replay, each command at the moment all prior work has settled,
    `atUs = round(now × 1000)`.
  - **Engine fingerprints**: SHA-256 of `rules.cjs`, `timeline.cjs`, `palette.cjs`, `clock.cjs`.
  - **L18**: carries `exception {rule: "one deciding bridge from the second introduction", reason: "owner kept both
    winning bridges", decidedBy: "owner", decidedAt: "2026-10-27"}`.
- [X] T012 [P] Add `confirm <from> <to> --sample <results.json>` to `tools/rounds/rounds.mjs`. It writes
  `content/rounds/runs/L####-L####/confirmed.json` with the wave, the sample levels, the results file and the date.
- [X] T013 [P] Add `tools/rounds/test/export.test.mjs` (`node --test`): entry-check refusals (unapproved picture,
  missing confirmation, audit НЕТ) and the shapes of the written files.
- [X] T014 Run `node tools/rounds/rounds.mjs confirm 1 20 --sample
  product/playtests/pilot-v12-owner-2026-10-27/results.json` and `node tools/rounds/export.mjs 1 20 --out
  content/game/rounds/export/L0001-L0020`. Commit the export (fixtures follow in US3).

**Checkpoint**: round formats exist, and L1–L20 are exported in them.

---

## Phase 3: User Story 1 - The owner plays L1–L20 in the game exactly as in the pilot (Priority: P1) 🎯 MVP

**Goal**: the game's rules are the round engine's rules, and the playtest plays round levels with them, including the
boosters in live time.

**Independent Test**: install the playtest with L1–L20 and play. The rules tests and `playtest/check` replay every
level's saved solution to a win in the rules' state.

### Tests for User Story 1 ⚠️ (write first, they must fail)

- [ ] T015 [P] [US1] Create `core/tests/Bloomlings.Core.Tests/Rounds/RoundRulesOrderTests.cs`. It covers every row of
  research R3:
  - neighbour order;
  - reach (entry distance 1, a target's distance is the nearest open neighbour + 1, sort by distance then index);
  - route choice (smallest-distance open neighbour, first in neighbour order);
  - `canTap` reasons `not-exposed`, `partner-not-exposed`, `pair-needs-two-slots`, `no-slot`;
  - the partner rule "on top of its column or directly under a pod of the same group";
  - `compactSlots` only for an accepted group;
  - reveal of tops after a commit (depth mode) and on commit (sealed);
  - status Won / Playing (bridge buildable) / Jammed / Stuck.
- [ ] T016 [P] [US1] Create `core/tests/Bloomlings.Core.Tests/Rounds/RoundTimelineTests.cs`:
  - the profile in µs verbatim: out 100 000 per cell (min 650 000), return 75 000 per cell (min 400 000), act 100 000,
    departure gap 250 000, release lag 300 000;
  - dispatch by age, frontier order, one departure per gap;
  - step order by time, then clear 0, arrive 1, return 2, release 3, check 4, then id or pod;
  - group release at now + 300 000 after all members reach 0;
  - the tail flag when all columns are empty and no bridge is left to choose;
  - the conservation invariants of `data-model.md`.
- [ ] T017 [P] [US1] Create `core/tests/Bloomlings.Core.Tests/Rounds/RoundDeterminismTests.cs`. The same commands give
  the same final signature when advanced in steps of ×1, ×2 and Auto ×2, and before and after a
  `Snapshot()` / `Restore()` in the middle of work (SC-007).
- [ ] T018 [P] [US1] Create `core/tests/Bloomlings.Core.Tests/Rounds/RoundBoostersTests.cs`. It covers the table of
  `data-model.md`:
  - **Extra Slot**: "capacity becomes 6 at once; the new slot is last", once per level.
  - **Return**: refused while a courier is in flight; "the pod goes to the top of its original column with its
    remaining count; its slot frees"; for a link, both members must be returnable.
  - **Shuffle**: links stay paired at equal depth in different columns, hidden stay hidden, no change when the patient
    solver finds no win.
  - **Bloom Burst**: "every unreserved cell of the code clears; the code's pods lose that many remaining (slots by age,
    then columns from the top)"; reserved cells stay with their courier.
  - Accounting reconciles after each (SC-008).

### Implementation for User Story 1

- [ ] T019 [US1] Create `core/src/Bloomlings.Core/Rounds/RoundState.cs`:
  - board, remaining, columns (a mutable copy of the tray), heads, slots (5, or 6 after Extra Slot), ages, revealed,
    bridge, status, serial;
  - `Clone`;
  - `Signature()` exactly as `rules.cjs signature` (board letters with `.`/`#`, remaining, heads, slots, active by age,
    revealed, bridge, status), joined by `|`.
- [ ] T020 [US1] Create `core/src/Bloomlings.Core/Rounds/RoundRules.cs`, the port of `rules.cjs`: reach, routeTo,
  bridgeChoices, topPods, status, compactSlots, canTap, legal (one tap per group, its first member; then bridge
  choices), check, revealTops. Use no hash-ordered collections (constitution III).
- [ ] T021 [US1] Create `core/src/Bloomlings.Core/Rounds/RoundTimeline.cs`, the port of `timeline.cjs` with `long`
  µs:
  - the profile and tail;
  - steps, jobs, reserved, inFlight, departReady;
  - `Commit(action)`: tap or bridge, reveal, dispatch;
  - `Advance(deltaUs)`: process due steps per moment, then dispatch;
  - dispatch, release, status;
  - the invariants of `timeline.cjs assert`;
  - `Snapshot()` / `Restore()`.

  Display speed is not a rules input: the client multiplies the real time it passes.
- [ ] T022 [US1] Create `core/src/Bloomlings.Core/Rounds/RoundSolver.cs`: the patient search of `idle-rules.cjs`
  (commit, then advance until nothing is pending) with a node budget. It is used by Shuffle's winnability check.
- [ ] T023 [US1] Create `core/src/Bloomlings.Core/Rounds/RoundBoosters.cs` with the four booster commands of
  `data-model.md` as `RoundTimeline` commands carrying µs time. Shuffle uses the level's seeded PRNG (the existing
  `core/src/Bloomlings.Core/Random`).
- [ ] T024 [P] [US1] Add golden replays for round levels with and without boosters in
  `core/tests/golden/rounds-*.golden.json` and `*.events.txt`, and register them in the existing golden replay test.
  Document regeneration in `core/tests/golden/README.md`.
- [ ] T025 [US1] Make the full playtest load only round content. In `playtest/android/PlaytestContent.cs`,
  `playtest/android/PlaytestFiles.cs` and `playtest/Catalog.props`, pack `content/game/rounds/levels` and `pictures`
  as Android assets once, and stop packing the legacy catalog, curated levels and daily pool.
- [ ] T026 [US1] Run the level screen on `RoundTimeline` in `playtest/design/LevelScreen.cs` and
  `playtest/android/LevelAnimator.cs`:
  - the model clock accumulates real elapsed time × speed (1, 2; Auto 2) as integer µs;
  - taps and bridge choices are committed with the current µs;
  - jam and win come from the rules' status.
- [ ] T027 [US1] Wire the jam card and the booster bar to `RoundBoosters` in `playtest/design/EndCards.cs` and
  `playtest/design/BoosterBarPainter.cs`. Guided booster demos and free first charges stay as in spec 005
  (`client/Assets/Bloomlings/UI/Design/GuideTour.cs`).
- [ ] T028 [US1] After the last round level show a "new levels soon" card, and keep the Daily Challenge off. Files:
  `playtest/design/EndCards.cs`, `playtest/design/PlaytestMeta.cs`, a new string in
  `client/Assets/Bloomlings/UI/Localization/Resources/Strings_en.csv`.
- [ ] T029 [US1] Make the level tester's ◀ ▶ walk the round levels, and add a "jump to level N" that sets the save's
  progress, for meta checks at L40 and L50. File: `playtest/android/TesterView.cs`.
- [ ] T030 [US1] Check GuideTour's steps against the round levels in `client/Assets/Bloomlings/UI/Design/GuideTour.cs`:
  - Level 1: entry and forced first tap on a top pod with reachable work;
  - the first level with a pod out of reach at the start, found by scanning round levels;
  - booster demos at L3, L4, L6, L9.

  Adapt any step that does not fit.
- [ ] T031 [US1] Extend `playtest/check/Program.cs` so the animator replays every round level's saved solution from
  `content/game/rounds/proofs/` and ends on the rules' state.

**Checkpoint**: L1–L20 play in the playtest by the round rules, and boosters work in live time.

---

## Phase 4: User Story 3 - Every transferred level is proven to play the same (Priority: P1)

**Goal**: reference fixtures from JS, a replay in C#, and refused publishing on any difference.

**Independent Test**: `rounds validate` passes on L1–L20. A deliberate change of the release lag makes it fail with the
level and the first differing event.

### Tests for User Story 3 ⚠️

- [ ] T032 [P] [US3] Create `core/tests/Bloomlings.Content.Tests/Rounds/ConformanceTests.cs`. For every
  `content/game/rounds/conformance/L####.json`:
  - the solution windows (events hash and signature hash per command), outcome and `endUs`;
  - every random game's outcome, `endUs` and final signature, and the win count equal to the proof's;
  - every owner attempt's outcome, end time and final signature;
  - on a mismatch, the report format of `contracts/conformance-fixture.md`.
- [ ] T033 [P] [US3] Create `core/tests/Bloomlings.Content.Tests/Rounds/ConformanceBreakTests.cs`. With the release lag
  set to 299 000 µs in a test copy of the profile, conformance fails on at least one of L1–L20 and names the level and
  the first differing event (SC-005).

### Implementation for User Story 3

- [ ] T034 [P] [US3] Create `core/src/Bloomlings.Core/Rounds/RoundEvents.cs`:
  - the canonical event lines verbatim from `contracts/conformance-fixture.md`: commit, reveal, bridge, depart, arrive,
    clear, return, complete, with times in µs and no assign;
  - the per-window SHA-256;
  - the extended signature `board|remaining|heads|slots|activeByAge|revealed|bridge|status|now|steps` with `steps` as
    `at:type:id-or-pod`, and its SHA-256.
- [ ] T035 [P] [US3] Create `core/src/Bloomlings.Core/Rounds/RoundRandomPolicy.cs`:
  - xorshift32 (`s ^= s << 13; s ^= s >> 17; s ^= s << 5`, seed 0 → 1);
  - choice index `(ulong)s * n >> 32`;
  - the §5 policy: settle the moment, list legal actions, add one wait when a step other than arrive or return is
    scheduled, a wait advances to the next such step;
  - per-game seed `seedBase × 7919 + n × 104729 + 1`.
- [X] T036 [US3] Create `tools/rounds/conformance.mjs`, a canonical event emitter and signature for the reference
  timeline, identical in format to T034. Add part 2 to `tools/rounds/export.mjs`, which writes
  `conformance/L####.json`:
  - solution windows;
  - every random game of the proof's run count with its seed;
  - owner attempts from `--owner-results` files with `atUs = round(modelTimeMs × 1000)`, replayed by the reference
    with those µs times; a changed outcome versus the results file is reported as a fixture error.
- [ ] T037 [US3] Create `core/src/Bloomlings.Pipeline/Commands/RoundCommands.cs` with `rounds import <export-dir>` and
  `rounds validate [--rounds content/game/rounds]`, registered in `core/src/Bloomlings.Pipeline/Commands/Cli.cs`.
  `validate` checks:
  - schemas, the validator of T008;
  - picture ↔ board inner rows;
  - proof completeness;
  - conformance (the replays of T032);
  - equal engine fingerprints across the set.

  It exits non-zero on any error.
- [X] T038 [US3] Add the normal-vision readability gate to `rounds validate`, using
  `core/src/Bloomlings.Pipeline/Readability/ColorScience.cs`: every pair of the 16 normal colours has smallest
  CIEDE2000 ≥ 10 (FR-047). The simulation numbers are written as a report, not a failure.
- [ ] T039 [US3] Re-export L1–L20 with fixtures (`--owner-results
  product/playtests/pilot-v12-owner-2026-10-27/results.json`), then `rounds import` and `rounds validate`. Commit
  `content/game/rounds/` (levels, pictures, proofs, conformance) when 20 solutions, all random games and 23 owner
  attempts match (SC-001, SC-002).

**Checkpoint**: the game's rules are proven equal to the reference on L1–L20.

---

## Phase 5: User Story 2 - The level looks and feels like the game, with the owner's decisions (Priority: P1)

**Goal**: the presentation contract (`contracts/presentation.md`) in the playtest, shared engine-free in the kit.

**Independent Test**: play L1–L20. `playtest/preview -- --rounds` passes the presentation checks.

### Tests for User Story 2 ⚠️

- [ ] T040 [P] [US2] Add `--rounds` to `playtest/preview` (`playtest/preview/Program.cs`). Its frames and checks:
  - no icon on cubes with symbols off, symbols on cubes with symbols on;
  - a link tube on a linked pair in the columns and in the places;
  - HUD absent under Auto;
  - introduction cards on L6/L8, L11/L13, L16/L18;
  - a 48×48 board fitting the phone frame.

  Plus a `--perf` run on the largest round board (SC-009).

### Implementation for User Story 2

- [ ] T041 [P] [US2] Create `client/Assets/Bloomlings/UI/Design/RoundBoardLayout.cs`:
  - from `rowsBottomFirst`, draw the outer ring and wall as the game's existing stone frame and the gate as the
    existing entry arch;
  - a stream `#` inside the frame is water, and a built bridge is a plank;
  - inner cells use the existing soft cubes;
  - fit up to 48×48.
- [ ] T042 [P] [US2] Create `client/Assets/Bloomlings/UI/Design/LinkTube.cs`, the geometry of a tube attached to the
  front faces of two linked pods, whose half nearer each pod has that pod's colour. Add its picture to
  `client/Assets/Bloomlings/UI/Design/UiRaster` (a new `LinkTube` raster) and register an asset slot in `AssetSlots`.
- [ ] T043 [US2] Create `client/Assets/Bloomlings/UI/Design/RoundAnimator.cs` (engine-free). Per frame, it gives from a
  `RoundTimeline`:
  - courier positions;
  - each cube's clear moment;
  - slot fills and releases;
  - reveal glows (about 1.6 s);
  - link tubes in columns and places.
- [ ] T044 [US2] Make `client/Assets/Bloomlings/UI/Design/ClearStyles.cs` and `ClearLook.cs` take each walk's start,
  arrival, clear and done from the timeline instead of `TripSeconds`. The walker is the family hero of the material
  (`RoundMaterial.family`) tinted in the material colour, for every style and in `ClearPreview` (FR-048).
- [ ] T045 [US2] Draw cubes without the variant icon in the kit's tile drawing (`client/Assets/Bloomlings/UI/Design/`,
  `UiRaster` Tile board style). When the symbols mode is on, draw the material's symbol on cubes, pods and places.
- [ ] T046 [US2] Add the Settings switch "symbols mode" (off by default) to the save's settings
  (`client/Assets/Bloomlings/Meta/`, engine-free) and to the playtest Settings screen (`playtest/design/MenuCards.cs`).
  Add its strings to `client/Assets/Bloomlings/UI/Localization/Resources/Strings_en.csv`.
- [ ] T047 [US2] Speed and Auto in `playtest/design/LevelScreen.cs` and the kit:
  - the speed-up control ×1 / ×2;
  - Auto starts on the timeline's tail flag, shows ×2 and hides the whole HUD (top bar, buttons, column chrome, booster
    bar), leaving the board.
- [ ] T048 [US2] Introduction cards: the first meeting shows the explanation card, the second (L8, L13, L18) a short
  reminder, and a just-revealed hidden pod glows. Files: `playtest/design/MetaCards.cs`, `playtest/design/PodPainter.cs`,
  strings in `Strings_en.csv`.
- [ ] T049 [US2] Draw the round level in the playtest: `playtest/design/BoardPainter.cs`, `PodPainter.cs` and
  `ClearPainter.cs` use `RoundBoardLayout`, `RoundAnimator` and `LinkTube`, with the round palette colours from
  `RoundMaterial`.
- [ ] T050 [US2] Run the localization tests (`client/DotnetCheck`, `LocalizationTests`) and `playtest/preview --
  --rounds`, and fix any failure.

**Checkpoint**: every owner decision of 2026-10-27 is visible in the playtest (SC-004).

---

## Phase 6: User Story 4 - A confirmed wave is transferred by a repeatable path (Priority: P2)

**Goal**: diff, publish, a manifest and packs, the playtest reading published content, the transfer log, the APK, the
owner's confirmation, and the skill.

**Independent Test**: from a clean checkout, the path runs from export to APK without hand edits, and refuses a wave
that fails an entry check (SC-006).

- [X] T051 [P] [US4] Add `rounds diff --from <git-ref|published-dir> --to content/game/rounds` to
  `core/src/Bloomlings.Pipeline/Commands/RoundCommands.cs`. A changed published level must carry a higher
  `definitionVersion`, and a changed picture a higher `version` (FR-052). Add tests in
  `core/tests/Bloomlings.Pipeline.Tests/RoundDiffTests.cs`.
- [X] T052 [US4] Add `rounds publish --out <dir>` to `RoundCommands.cs`. It refuses on any validate error, then writes
  `round-content-manifest.v1` (content version, levels, pictures, SHA-256 per pack) and the packs. Add the manifest
  schema `core/src/Bloomlings.Content/Schemas/round-content-manifest.v1.json` and tests in
  `core/tests/Bloomlings.Pipeline.Tests/RoundPublishTests.cs`.
- [ ] T053 [US4] Make the playtest read published round packs through the content set (`playtest/android/PlaytestContent.cs`
  and the kit's content reader), with the level and its picture read on first use, as today.
- [X] T054 [P] [US4] Create `tools/rounds/transfer-log.mjs`, which appends and updates
  `content/game/rounds/transfers.jsonl` entries `{wave, levels, exceptions, contentVersion, engine, exportedAt,
  validatedAt, apk, ownerConfirmedAt | null, note}`. States: exported → validated → published → confirmed.
- [ ] T055 [US4] Run the transfer of L1–L20:
  - export, import, validate, publish;
  - the transfer log;
  - build the full playtest APK with the manual workflow `android-apk.yml` (no new triggers), or locally;
  - send the APK to the owner for the phone check (quickstart §5);
  - record `ownerConfirmedAt` when they confirm.
- [ ] T056 [US4] Write the skill `.claude/skills/bloomlings-round-transfer/SKILL.md` from the run of T055:
  - entry checks;
  - export, import, validate, diff, publish;
  - APK;
  - the owner's phone check of the wave's sample;
  - the log;
  - what to do on a conformance failure (never edit a fixture by hand, re-export from the reference).

  Link it from `.claude/skills/bloomlings-round-wave/SKILL.md` as the step after the owner's confirmation.

**Checkpoint**: L1–L20 are in the game, confirmed by the owner, and the path is a skill.

---

## Phase 7: User Story 5 - The Unity client plays round levels (Priority: P3)

**Goal**: the product client on the same rules and presentation.

**Independent Test**: the `DotnetCheck` engine-free tests play L1–L20 from published content. On a device build the
owner sees the same behaviour as in the playtest.

- [ ] T057 [P] [US5] Extend `client/DotnetCheck` (and `client/DotnetCheck/UnityStubs.cs` as needed) with tests that
  play every round level's saved solution through the client's engine-free services to a win.
- [ ] T058 [US5] Move `client/Assets/Bloomlings/Gameplay/GameplayController.cs` and
  `Gameplay/Timeline/EventTimeline.cs` / `TimelinePlayer.cs` to `RoundTimeline` and `RoundAnimator`, with model time
  in µs × speed and Auto ×2 with the HUD hidden.
- [ ] T059 [US5] Update the Unity views `Gameplay/Board/BoardView.cs`, `TileView.cs`, `Gameplay/Tray/TrayView.cs`,
  `PodView.cs`, `Gameplay/Slots/SlotRowView.cs`, `Gameplay/Workers/WorkerPool.cs`, `BloomlingFigure.cs` and
  `Gameplay/Effects/ClearFxView.cs` per `contracts/presentation.md`: frame, cubes without icons, symbols mode, link
  tubes, walkers as tinted family heroes, introduction cards, reveal glow.
- [ ] T060 [US5] Make the Unity client load published round packs (`client/Assets/Bloomlings/Services/`), keep Daily
  off, and show "new levels soon" after the last level. Build with the manual `unity-apk.yml` for the owner's check.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T061 [P] Update `CLAUDE.md` (round rules in the core, `content/game/rounds/`, the transfer skill, symbols mode)
  and `tools/rounds/README.md` (`export.mjs`, `conformance.mjs`, `confirm`, `transfer-log.mjs`).
- [ ] T062 [P] List what still uses the legacy `Bloomlings.Core.Simulation` path (generator, solver, legacy pipeline
  commands, tests) in `specs/006-round-levels-in-game/legacy-references.md`, as the input for a follow-up retirement
  spec.
- [ ] T063 Run `specs/006-round-levels-in-game/quickstart.md` end to end:
  - `dotnet test core/Bloomlings.sln`;
  - `dotnet test client/DotnetCheck/Bloomlings.Client.DotnetCheck.csproj`;
  - `node --test tools/rounds/test/*.test.mjs`;
  - `dotnet run --project playtest/check`;
  - `playtest/preview -- --rounds`.

  Record the results against SC-001–SC-009 in `specs/006-round-levels-in-game/checklists/acceptance.md`.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: none. T002 (constitution) must land before any rules code (T019 onward).
- **Foundational (Phase 2)**: depends on Setup and blocks all stories.
- **US1 (Phase 3)**: after Foundational. It is the MVP.
- **US3 (Phase 4)**: needs US1's `RoundTimeline` (T021). Its JS side (T036) can start right after Foundational.
- **US2 (Phase 5)**: needs US1's playtest level screen (T026). The kit tasks T041–T043 can start after T021.
- **US4 (Phase 6)**: needs US3 (validate) and US2 (the playtest the owner checks).
- **US5 (Phase 7)**: needs US1–US3. It can run in parallel with US4.
- **Polish (Phase 8)**: last.

### Within each story

Tests first (they fail), then models, rules, timeline, clients, integration. Commit after each task or group, and push
on the session branch.

### Parallel opportunities

- **Phase 1**: T003, T004, T005.
- **Phase 2**: T006, T009, T010, T012, T013, alongside T007 → T008 → T011.
- **US1 tests**: T015, T016, T017, T018 together.
- **US3**: T034 and T035 together; the JS side T036 in parallel with the C# T034/T035.
- **US2**: T041 and T042 in parallel, then T043.
- **US4**: T051 and T054 in parallel.
- **US5**: T057 in parallel with T058.

## Parallel Example: User Story 1

```text
Task: "T015 RoundRulesOrderTests.cs (orderings of research R3)"
Task: "T016 RoundTimelineTests.cs (profile in µs, step order, release lag)"
Task: "T017 RoundDeterminismTests.cs (×1/×2/Auto, snapshot/restore)"
Task: "T018 RoundBoostersTests.cs (live-time boosters, accounting)"
```

## Implementation Strategy

### MVP first (User Story 1, then the proof)

1. Phase 1 and Phase 2 (governance, formats, export of L1–L20).
2. Phase 3 (US1): the rules port and the playtest on round levels.
3. Phase 4 (US3): conformance proves equality on L1–L20. Do not show the owner a build before this passes.
4. Phase 5 (US2): the owner's presentation decisions.
5. Phase 6 (US4): publish, APK, the owner's phone check, the transfer skill.
6. Phase 7 (US5): the Unity client.

### Incremental delivery

Each phase ends at a checkpoint that can be committed and shown. The owner sees the first APK after US2 (rules proven,
look as decided), and confirms the transfer in US4.
