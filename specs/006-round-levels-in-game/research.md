# Research: Round Levels in the Game (spec 006)

Phase 0 of the plan. Each item: decision, rationale, alternatives. Facts come from the code as of 2026-10-27:
- the round engine: `tools/rounds/engine/rules.cjs` (264 lines), `timeline.cjs` (119), `idle-rules.cjs`, `palette.cjs`;
- the measures: `tools/rounds/measure.mjs`;
- the game core: `core/src/Bloomlings.Core`;
- the playtest: `playtest/`.

## R1. Where the round rules live in the game

**Decision**: a new namespace `Bloomlings.Core.Rounds` inside the existing pure core assembly (`netstandard2.1`, no
engine dependencies). It is a faithful port of `rules.cjs` and `timeline.cjs`:
- `RoundLevel` (definition) and `RoundState` (board, remaining, heads, slots, ages, revealed, bridge, status);
- `RoundRules` (reach, routeTo, topPods, canTap, legal, check, status, compactSlots, bridgeChoices);
- `RoundTimeline` (commit, advance, dispatch, steps, jobs, reservations, release);
- `RoundEvents` (the canonical event log of R4).

The clients switch to it. The legacy `Simulation` path stays compiled for now, because the generator, the solver, the
legacy pipeline commands and their golden tests use it, but no client references it. Removing it is a follow-up once
nothing does.

**Rationale**: constitution III requires one pure rules implementation shared by clients, solver and tests. A
same-assembly namespace keeps the asmdef and the playtest links unchanged. A line-by-line port keeps every ordering the
reference relies on (R3).

**Alternatives**:
- Rewriting the legacy `Settler` into a timeline: rejected. Every rule differs (proposal table), so nothing would be
  reused and the diff would be unreadable.
- Embedding the JS engine in the clients: rejected. It needs a JS runtime in Unity and Android, and breaks principle III.
- A separate assembly: possible later, but it adds an asmdef and link changes now for no gain.

## R2. Model time unit

**Decision**: model time is a 64-bit integer count of **microseconds**. The profile becomes:

| Parameter | Value (µs) |
|---|---|
| out per cell | 100 000 |
| return per cell | 75 000 |
| act | 100 000 |
| minimum out | 650 000 |
| minimum return | 400 000 |
| departure gap | 250 000 |
| release lag | 300 000 |

- A command carries the model time at which it was accepted.
- Clients advance model time by real elapsed time × display speed (1, 2, or Auto 2), accumulated as integer µs.
- The epsilons of the JS (`1e-7` ms) become exact comparisons.

**Rationale**: no floating point in rules (principle III). The pilot's recorded commands carry fractional milliseconds
(335 of the 407 in the v12 results, e.g. 787.2000000178814): the display advanced time by frame deltas × 2 or 2.5.
Microseconds keep them within 1 µs, far below any rule-visible gap; the smallest profile step is 75 ms. Whole
milliseconds would round them by up to 0.5 ms and could reorder a tie. The JS reference is fed the same µs-rounded times
when it produces the conformance fixtures (R4), so both sides start from identical inputs.

**Alternatives**:
- Double milliseconds in C#: rejected by principle III.
- Whole milliseconds: rejected, for the replay fidelity above.

## R3. Orderings the port must keep

**Decision**: the C# port reproduces these orderings exactly. They decide which cell a worker takes and which event
comes first. Each one gets a rule test.

| Where | Ordering |
|---|---|
| Neighbours | `i-w` (the row below, rows are bottom first), left, right, `i+w` (the row above) — in this index order |
| Reach | breadth-first from the entries; a target's distance is the nearest open neighbour + 1 (an entry cell is 1); targets sorted by distance, then cell index |
| Dispatch | active pods by age (oldest first); each takes frontier cells of its exact material, not reserved, in frontier order (`available.shift()`) while its departure time has come; one departure per gap (`departReady = start + gap`) |
| Route | from the entry to the open neighbour of the target with the smallest distance, first such neighbour in neighbour order; then parents |
| Steps | by time, then type (clear 0, arrive 1, return 2, release 3, check 4), then courier id or pod |
| Release | a group whose members all have remaining 0 schedules one release at now + 300 ms; at that time every member's slot empties |
| Slots | a single pod takes the first free slot; release leaves a hole; only an accepted group compacts the occupied slots to the left first, keeping order |
| Reveal | after a commit, every top pod still hidden and not `sealed` reveals (depth mode); a committed hidden pod reveals on commit (sealed mode) |
| Status | Won when no colour cell remains; Playing while a bridge can be built; Jammed when no slot is free; else Playing if some top pod can be tapped, else Stuck. While steps are pending, Playing |

**Rationale**: the conformance test (R4) compares event logs. Any ordering difference shows up as the first differing
event, so these orderings are requirements, not style.

## R4. Conformance: how JS and C# are compared without Node in the .NET tests

**Decision**: the JS side exports **reference fixtures**, and the C# tests replay and compare them. A fixture per level
(contract `contracts/conformance-fixture.md`) holds:
- **saved solution**: the commands with µs times; the canonical event log, hashed per command window; the state
  signature hash after each command and at the end; the outcome;
- **random test**: for each recorded game (100 or 400, the §5 policy and seeds), the outcome and the final signature hash;
  the win count;
- **owner attempts** (where pilot results exist): each attempt's commands with µs-rounded times, outcome, end time and
  final signature hash.

The C# test runs the same inputs through `RoundTimeline` and the same random policy (R5). It requires:
- identical hashes, outcomes and win counts;
- on a mismatch, the canonical log of the first differing window.

**Rationale**: `dotnet test` stays self-contained, and CI needs no Node. A fixture is regenerated only by the JS
exporter from the reference engine version it names, so a C# change can never quietly move the reference.

**Alternatives**:
- Running Node from the C# tests: rejected. Two runtimes in every test run, and it breaks the manual-CI budget rule's
  simplicity.
- Comparing only outcomes: rejected. It is too weak; the owner relies on identical difficulty.

## R5. The random policy and its PRNG in C#

**Decision**: port the §5 extended random policy (`measure.mjs randomGame`) and its PRNG exactly.
- **PRNG**: xorshift32 as in `level-stats/strategies.mjs rng`: `s ^= s << 13; s ^= s >>> 17; s ^= s << 5` on uint32,
  with seed 0 replaced by 1.
- **Choice**: index = `floor(r × n)` with `r = s / 2³²`, computed exactly as `(ulong)s × n >> 32`. This is identical,
  since `s × n < 2⁵³`.
- **Policy**:
  - settle the events of the moment;
  - list the legal actions (taps of available groups, each buildable bridge);
  - add one "wait" when a state-changing step is scheduled (any step but arrive and return);
  - pick uniformly;
  - a wait advances to the next state-changing step.
- **Seeds**: per game, as `randomTest` (`seed × 7919 + n × 104729 + 1`).

**Rationale**: FR-031 requires the same random games and the same win share.

## R6. The 16 materials and readability

**Decision**: a `RoundPalette` with the 16 materials of `palette.cjs`. Each material has:
- its code letter (G P B A C D V Y O L M R K N S E), the wire id in round JSON;
- its name;
- its family hero (sprig, bloom, drop, twig);
- normal and accessible colours;
- its symbol (♧ ✿ ◆ ● ◇ ▰ ✦ ☀ ▲ ✚ ♣ ♥ ❀ ▼ ○ ■).

The legacy `VariantCatalog` stays for the legacy path.

**Readability**, measured with the game's own method (`ColorScience` ported to `tools/rounds/analysis/readability.mjs`):
- under normal vision every pair passes (smallest CIEDE2000 ≥ 10);
- under protanopia, deuteranopia or tritanopia simulation, 11 pairs merge (Слива/Глубина 0.7 under protanopia). These
  pairs occur in 11 of the 19 placed levels and in 86 of 116 approved pictures;
- the accessible palette also has 11 merging pairs.

The owner chose a **symbols mode** in Settings (off by default) that shows each material's symbol on cubes, pods and
places (FR-047). The readability gate checks normal vision for the palette. Pipeline validation records the simulation
numbers as a report, not a failure.

**Alternatives**:
- Keeping failing pairs out of one level: rejected by the owner. It would rebuild 11 of 19 levels and empty the pool.
- A separate colour-blind palette: it still merges 11 pairs.
- Hero icons now: they would not separate same-family pairs such as Солнце/Лайм.

## R7. Board geometry and limits

**Decision**: a round board has its own size limit of 48×48 (`RoundLevel.MaxSide`), separate from the legacy
`CellPos.MaxWidth = 36`. The board rows are those of the level JSON: bottom first, `.` open, `#` wall, colour letters.
The frame (ring and wall) and the gate are part of the rows, as the round levels already store them (`boardOf` in
`tools/rounds/lib.mjs`). Bridge sites are `hero.choices` with cell and anchor.

For drawing, the kit's `BoardLayout` gets a round mode:
- the outer ring and wall of the rows are drawn as the game's existing stone frame (owner: "the frame is already drawn");
- the gate is the existing entry arch;
- streams are water cells;
- inner cells are the game's soft cubes.

**Rationale**: the rows are the reference's input, so changing them would change the rules. The frame look stays the
game's.

## R8. Presentation pipeline in live time

**Decision**: the round timeline already schedules every trip (depart, arrive, clear, return) with its route and times,
so presentation needs no separate trip planner. Two parts:
- **`RoundAnimator`** (engine-free, in the kit so both builds share it) reads the timeline's events and courier
  positions per frame: the walkers, the cubes' clear animation at each clear time (FR-042), slot fills and releases,
  reveal glows, and the link tubes.
- **The clearing styles** (`ClearStyles`, `ClearLook`) keep their looks. They take each walk's start and arrival from
  the timeline instead of `TripSeconds`. The walker shows the family hero tinted in the material colour (FR-048).

Display speed:
- the normal speed-up multiplies the model-time advance by 2;
- Auto (tail) by 2 and hides the HUD (FR-044).

Neither changes rule events, because model time is the same, only shown faster.

**Rationale**: the pilot already proves this structure, since it draws couriers straight from the timeline. One shared
animator keeps the playtest and Unity identical.

## R9. Boosters in live time (no JS reference)

**Decision**: boosters are new rules on `RoundTimeline`, defined in the data model (FR-012). Each is a command with its
model time:
- **Extra Slot**: capacity 5 → 6 at once, once per level; the new slot is the last.
- **Return**: allowed for a slot pod with remaining > 0 and no courier in flight (`inFlight == 0`). Its remaining count
  goes back to the top of its original column, which needs a mutable tray (R10). Its slot frees, and a pending check
  step for it is dropped. A finished pod waiting for its pair cannot be returned. A member of a link returns with its
  partner only if neither has couriers in flight.
- **Shuffle**: reorders the pods still in the columns with the seeded PRNG of the level. Links keep their members at
  equal depth in different columns, hidden pods stay hidden, and the result must keep a winning continuation. It is
  checked with the patient solver; failing that, the identity order.
- **Bloom Burst**: for one revealed material, clears every cell of it that no courier has reserved, and reduces the
  remaining counts of that material's pods (slots first by age, then columns from the top) by the cleared count,
  keeping exact accounting. Pods at 0 release by the normal release rule.

The jam card and the ad rescue use these. Guided demos and free charges stay as in spec 005.

**Rationale**: the round engine has no boosters, so their correctness is pinned by C# rule tests and golden replays,
not by conformance. Levels stay validated without boosters (constitution IV).

## R10. Mutable tray order

**Decision**: `RoundState` keeps its own per-column pod lists, copied from the definition, so that Return and Shuffle
can change them. Without boosters they never change and equal the reference's fixed tray. Conformance holds
unchanged, because no fixture uses boosters.

## R11. Data formats and where content lives

**Decision**:
- **Formats** (contracts):
  - `round-level.v1`: definition;
  - `round-picture.v1`;
  - `round-proof.v1`: saved solution, random-test record, class checks, audit result, engine fingerprints;
  - the conformance fixture;
  - `round-content-manifest.v1`;
  - the transfer log (JSON lines).
- **The game's round content**: `content/game/rounds/`, with `levels/L####.json`, `pictures/P####.json`,
  `proofs/L####.json`, `conformance/L####.json`, `transfers.jsonl`.
- **Published packs**: under the existing publish output, as the legacy pipeline does.
- **The exporter** (`tools/rounds/export.mjs`, JS, next to the reference engine): writes a wave from `content/rounds/`
  (levels, approved pictures, audit) into an export folder in these formats. It runs the reference to produce the
  fixtures.
- **The C# pipeline** gains `rounds import`, `rounds validate`, `rounds publish`, `rounds diff`
  (`contracts/pipeline-rounds-cli.md`).

**Rationale**: generation stays in JS, next to the reference engine and the bank. The game side only imports, proves and
publishes. This boundary is the transfer.

## R12. Engine fingerprints

**Decision**: the round engine version is the SHA-256 of each of `rules.cjs`, `timeline.cjs`, `palette.cjs`,
`clock.cjs` (the pilot already shows them as hashes). They are recorded in every proof and fixture.
- `rounds validate` refuses a level whose fingerprints differ from the fixture set's.
- A change of the reference engine requires re-running the random tests of placed levels (17 §5) and regenerating
  their fixtures before transfer.

## R13. Content placement in the game builds

**Decision**:
- The playtest APKs carry only `content/game/rounds/` (levels, pictures). The legacy catalog, curated levels and daily
  pool are no longer packed.
- After the last level, a "new levels soon" card.
- The Daily Challenge stays off (Remote Config / flag).
- The tester's ◀ ▶ walk the round levels, and a "jump to level N" sets the save's progress for meta checks (L40 Store
  animations, L50 milestone).
- GuideTour's steps are checked against the round levels, and a step is adapted where it does not fit:
  - Level 1: the entry and the forced first tap, which need a top pod with reachable work;
  - "the first level with a pod out of reach at the start": now found by scanning the round levels, not assumed to be
    L2;
  - booster demos at their unlock levels L3, L4, L6, L9.

## R14. Governance items

- Constitution 1.1.0 through `/speckit-constitution`: II (hue under normal vision on board cubes, plus symbols mode;
  icons not required on cubes) and III (commands carry integer model time). Owner-approved on 2026-10-27.
- Locked docs 01, 02, 03 and 15 get owner amendment notes for the round rules, with this spec as the reason.
- `CLAUDE.md` gets a short note about the round rules in the core and the transfer skill.

## R15. Tooling prerequisite

The current container has Node 22 but no .NET SDK. Implementation needs the .NET 10 SDK pinned by `core/global.json`
(install in the session, or run `core-tests.yml` manually). Fixture export and the JS side need only Node.
