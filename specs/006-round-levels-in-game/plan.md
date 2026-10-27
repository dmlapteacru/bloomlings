# Implementation Plan: Round Levels in the Game

**Branch**: `006-round-levels-in-game` (committed on `claude/great-darwin-6qrpj8`) | **Date**: 2026-10-27 |
**Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/006-round-levels-in-game/spec.md`

## Summary

Make the round engine's rules the game's rules, then transfer confirmed round waves into the game, starting with
L1–L20 (L18 as a marked exception).

The approach (research R1–R15):
1. **Rules.** Port the ~400-line JS round engine line by line into a new pure-C# namespace `Bloomlings.Core.Rounds`,
   with integer microsecond model time and the same orderings.
2. **Proof.** Prove equality by reference fixtures that the JS exporter writes: the saved solution's event windows and
   state signatures, every random-test game, the owner's pilot attempts. The C# tests and `rounds validate` replay them.
3. **Boosters.** Add live-time rules for the four boosters, pinned by C# rule tests and golden replays.
4. **Formats and pipeline.** Add round formats (level, picture, proof, fixture, manifest) and `rounds
   import/validate/diff/publish` in the pipeline.
5. **Presentation.** Switch the playtest, then Unity, to round content with a shared engine-free `RoundAnimator`, the
   game's frame and cubes, the owner's presentation decisions, and a symbols mode for colour-blind players. Remove the
   legacy levels from the builds.
6. **Skill.** Write the transfer down as `bloomlings-round-transfer` after the first run.

## Technical Context

**Language/Version**:
- C# for the core (`netstandard2.1`, shared with Unity), the pipeline and tests (`net10.0`), the playtest (.NET for
  Android);
- JavaScript (Node 22) for the round tools and the exporter.

**Primary Dependencies**:
- existing only: NUnit 4, FsCheck, Newtonsoft.Json (pipeline), SkiaSharp (playtest preview);
- the reference round engine in `tools/rounds/engine/`.

No new third-party packages.

**Storage**: JSON files in the repository:
- `content/game/rounds/`: levels, pictures, proofs, conformance, `transfers.jsonl`, published packs;
- the player's save is unchanged except a `symbols` setting.

**Testing**:
- `dotnet test core/Bloomlings.sln`: rule tests, conformance fixtures, golden replays, determinism;
- `node --test` for the exporter's entry checks and format writer;
- `playtest/check` and `playtest/preview` engine-free checks.

**Target Platform**: Android phones (the full playtest APK first), then the Unity client (Android/iOS).

**Project Type**: mobile game monorepo (pure core library, content pipeline CLI, two clients, JS content tools).

**Performance Goals**:
- the largest round board (48×48 with the frame, up to ~1400 colour cells) with 5–6 places working plays as smoothly
  as today's biggest boards (SC-009, `playtest/preview -- --perf`);
- a conformance run over L1–L20 (about 2 400 random games) finishes in under 2 minutes on one core.

**Constraints**:
- deterministic integer rules (constitution III);
- offline;
- no rules in presentation;
- no new Actions triggers (manual CI only);
- the .NET SDK is not in the current container, so install it for implementation.

**Scale/Scope**:
- 20 levels now, waves of 50 up to 2500;
- 16 materials;
- 3 mechanics (hidden, links, bridge);
- 4 boosters.

## Constitution Check

*GATE: checked before Phase 0, re-checked after Phase 1.*

| Principle | Status | Notes |
|---|---|---|
| I. Colony Flow structure, Bloomlings identity | Pass | The core loop gets closer to Colony Flow (live work, link tubes on the front, Auto hiding the HUD) through a spec the owner decided (I requires exactly that). No Colony Flow assets are copied: the tube and HUD behaviour are interaction patterns, drawn in our kit. |
| II. Exact matching, unambiguous board | **Amendment required (approved)** | Exact matching per material code is kept. The text "every variant identifiable by at least hue and icon" conflicts with no icons on cubes (FR-041). The owner approved constitution 1.1.0: on board cubes, hue under normal vision (all 120 pairs pass CIEDE2000 ≥ 10) plus a symbols mode for colour-blind players. Done by `/speckit-constitution` in Phase A before code. |
| III. Deterministic simulation | **Clarification required (approved)** | The rules stay in one pure C# implementation with integer µs model time, a seeded PRNG (xorshift32 for the §5 policy) and no device time. Commands carry their model time, so 1.1.0 states this explicitly. |
| IV. Validated, fair content | Pass | Every round level carries a stored winning trace (proof); it needs no boosters; exact per-material accounting; validation refuses incomplete proofs; versioned definitions. "Losable through bad choices": the §5 random test shows losses on all non-tutorial levels. "Readable palette": normal vision passes, and the symbols mode covers colour-vision deficiencies (II amendment). L18's exception concerns rule 17's bridge rule, not the constitution. |
| V. Difficulty from decisions | Pass | Class by random test, "opening + colours", forced-decision cap, slot pressure (rules 17). No tile HP. |
| VI. Fair monetization | Pass | Boosters are the same convenience items, now in live time; no power upgrades. |
| VII. Simplicity, readability, offline | Pass | Strictly 2D; no map or chooser (the tester's jump is a dev tool); offline. |
| Workflow gates | Pass, with tasks | Rule tests, golden corpus updates, determinism checks for the new rules code. Content validated by `rounds validate`. Locked docs 01/02/03/15 changed through the owner. `CLAUDE.md` kept consistent. |

Post-design re-check: unchanged. Phase 1 adds no new deviations. The two governance items (II, III) are tracked as
Phase A tasks.

## Project Structure

### Documentation (this feature)

```text
specs/006-round-levels-in-game/
├── spec.md
├── plan.md              # this file
├── research.md          # R1–R15
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── round-level.schema.json
│   ├── round-picture.schema.json
│   ├── round-proof.schema.json
│   ├── conformance-fixture.md
│   ├── pipeline-rounds-cli.md
│   └── presentation.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
core/src/Bloomlings.Core/Rounds/              # NEW: the game's rules (port of tools/rounds/engine)
├── RoundMaterial.cs        # 16 materials: code, family, colours, symbol
├── RoundLevel.cs           # definition + validation (round-level.v1)
├── RoundState.cs           # board, remaining, columns, heads, slots, ages, revealed, bridge, status
├── RoundRules.cs           # reach, routeTo, topPods, canTap, legal, check, status, compactSlots, bridgeChoices
├── RoundTimeline.cs        # µs time: commit, advance, dispatch, steps, jobs, reservations, release, tail
├── RoundBoosters.cs        # Extra Slot, Return, Shuffle, Bloom Burst in live time
├── RoundEvents.cs          # canonical event log + state signature + SHA-256 (conformance contract)
├── RoundRandomPolicy.cs    # §5 extended policy + xorshift32
└── RoundSolver.cs          # patient search (port of idle-rules) for Shuffle's winnability check
core/src/Bloomlings.Content/Rounds/           # NEW: JSON readers/writers for round formats, manifest, packs
core/src/Bloomlings.Pipeline/Commands/RoundCommands.cs   # NEW: rounds import / validate / diff / publish
core/tests/Bloomlings.Core.Tests/Rounds/      # NEW: ordering tests (R3), booster tests, determinism, golden
core/tests/Bloomlings.Content.Tests/Rounds/   # NEW: conformance over content/game/rounds/conformance/*.json
core/tests/golden/rounds-*                    # NEW: golden replays incl. boosters

tools/rounds/export.mjs                       # NEW: entry checks + export + reference fixtures
tools/rounds/rounds.mjs                       # + `confirm` (owner's sample confirmation)
tools/rounds/analysis/readability.mjs         # exists: the palette gate (normal vision) + report

content/game/rounds/                          # NEW: levels/ pictures/ proofs/ conformance/ transfers.jsonl published/

client/Assets/Bloomlings/UI/Design/           # kit (linked into playtest)
├── RoundAnimator.cs        # NEW: timeline → walkers, clears, slots, glows, tubes (engine-free)
├── RoundBoardLayout.cs     # NEW: frame/gate/stream/bridge from rows; 48×48 fit
├── LinkTube.cs             # NEW: front-attached split-colour tube geometry
├── ClearStyles.cs / ClearLook.cs  # walkers from timeline times; family hero tinted (FR-048)
└── UiRaster (Tile)         # cube without icon; symbol when symbols mode is on
client/Assets/Bloomlings/Gameplay/            # Unity views switched to RoundTimeline + RoundAnimator (P3)
playtest/android, playtest/design             # level screen on RoundTimeline; HUD hides under Auto; symbols setting;
                                              # "new levels soon"; tester ◀ ▶ + level jump; round content only
playtest/check, playtest/preview              # round replays; presentation checks; --rounds; --perf on 48×48
.claude/skills/bloomlings-round-transfer/     # NEW after the first transfer
.specify/memory/constitution.md               # 1.1.0 (II, III)
product/01, 02, 03, 15                         # owner amendment notes
```

**Structure Decision**: the existing monorepo layout. The new rules sit in the pure core assembly. Formats and commands
sit in the existing content and pipeline projects. Presentation sits in the shared engine-free kit, so the playtest and
Unity draw the same thing. Generation stays in JS next to the reference engine; the exporter is the only bridge.

## Phases (for /speckit-tasks)

- **A. Governance.**
  - Constitution 1.1.0 (II, III).
  - Owner amendment notes in docs 01, 02, 03, 15.
  - The `CLAUDE.md` note.
- **B. Rules port** (P1, US1):
  - materials, level and state;
  - rules (orderings of R3);
  - the timeline in µs;
  - canonical events and signature;
  - the random policy;
  - rule tests.
- **C. Formats and exporter** (P1, US3):
  - schemas;
  - C# readers and writers;
  - `export.mjs` with entry checks and fixtures;
  - `rounds.mjs confirm`;
  - export L1–L20.
- **D. Conformance** (P1, US3):
  - fixture replay tests;
  - `rounds validate`;
  - a deliberate-break check (SC-005);
  - the owner attempts (SC-002).
- **E. Boosters in live time** (P1 for the playtest, per clarification):
  - rules;
  - jam card wiring;
  - golden replays;
  - determinism with boosters (SC-008).
- **F. Playtest client** (P1, US1 and US2):
  - round content only;
  - `RoundAnimator`;
  - frame, cubes, palette, no icons;
  - clear animation in live time;
  - link tubes;
  - speed-up and Auto ×2 with the HUD gone;
  - introduction cards, reminder, glow;
  - symbols mode;
  - "new levels soon";
  - tester level jump;
  - GuideTour checks;
  - preview and perf checks.
- **G. Pipeline publish and APK** (P2, US4):
  - `rounds diff` and `rounds publish`, manifest, packs;
  - the playtest reads published round content;
  - APK;
  - the owner's phone check;
  - the transfer log.
- **H. Transfer skill** (P2, US4): `bloomlings-round-transfer`, from the first run.
- **I. Unity client** (P3, US5): views on `RoundTimeline` and `RoundAnimator`; the `DotnetCheck` engine-free tests.

## Risks

- **Ordering drift** between JS and C#: caught by conformance at the first differing event. The R3 table is the
  checklist.
- **µs rounding of the owner's attempts** could flip a rare tie. The fixture records it, and a difference is reported
  as a fixture error rather than hidden.
- **Readability**: colour-blind players depend on the symbols mode. It is off by default, so the onboarding should
  mention it in Settings (copy task in F).
- **Legacy code still compiled** (generator, solver, legacy pipeline): there are no client references. Removal is a
  follow-up spec once nothing depends on it.
- **The tooling gap**: no .NET in this container. Install the SDK before Phase B.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Two rule implementations compiled side by side for a while (`Simulation` legacy, `Rounds` new) | The generator, solver, legacy pipeline commands and their golden tests use the legacy rules, and removing them now would widen this spec far beyond the transfer | Deleting the legacy path immediately would break the core test suite and the pipeline in the same change. Clients use only `Rounds`, so the game has one rule set (FR-060), and principle III's "one implementation shared by clients" holds for what ships. |
