# Phase 0 Research: Bloomlings Launch Game

**Feature**: `001-core-game-mvp` · **Spec**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md)
**Inputs**:
- `product/15_TECHNICAL_ARCHITECTURE.md`: the high-level direction (Unity + C#, deterministic core, offline generator and solver, lightweight backend).
- the spec's requirements;
- the design invariants in `CLAUDE.md`.

Each entry uses the format **Decision / Rationale / Alternatives considered**. Items that stay commercial or balancing
decisions are listed at the end, in *Deferred decisions*. None of them blocks design.

---

## R1. Engine, rendering and UI

**Decision**:

- **Engine**: Unity **6.3 LTS** (`6000.3.x`). The exact patch is pinned in `ProjectSettings/ProjectVersion.txt`:
  `6000.3.25f1` (2026-09-29), the newest 6.3 patch with a GameCI Android image.
- **Rendering**: URP with the 2D Renderer and sprite atlases.
- **UI**: uGUI for all screens and the HUD.
- **Orientation**: portrait only.

**Rationale**:

- Doc 15 selects Unity + C#.
- 6.3 is the current LTS, supported until December 2027. 6.0 LTS support ends in October 2026, before launch.
- The URP 2D Renderer is the maintained 2D path and batches flat sprites well.
- uGUI is the most mature runtime UI for animated game screens and has the largest tooling ecosystem.

**Alternatives considered**:

- **Unity 6.0 LTS**: support ends too early.
- **The Built-in Render Pipeline**: legacy.
- **UI Toolkit**: its runtime is viable, but animation and tooling for game UI are weaker. It can be revisited per screen later.

## R2. Code layout and sharing the core between Unity and tools

**Decision**: The gameplay core, level content model, solver and generator are **pure C# class libraries** in
`core/src/`. They target **netstandard2.1** with **C# 9**, the language level Unity supports, and have **no
`UnityEngine` references**.

- `dotnet` tools and tests (.NET 10 LTS) build them through `.csproj` files.
- Unity consumes `Bloomlings.Core` and `Bloomlings.Content` from the **same source folders** through a local UPM package.
  The package uses a `package.json` plus `.asmdef` files with `noEngineReferences: true`, referenced as
  `file:../../core/src/...`.
- `Bloomlings.Solver` is also consumed by Unity, for the runtime Shuffle guarantee (R10).
- `Bloomlings.Generator` stays tools-only.

**Rationale**:

- The solver, generator, pipeline and CI must run the *exact* same rules as the game (determinism, SC-005). They must
  also run without the Unity Editor.
- Sharing source avoids copying precompiled DLLs and keeps debugging in both environments.

**Alternatives considered**:

- **Precompiled DLLs in `Assets/Plugins`**: slow iteration and version drift.
- **Rules implemented twice** (client and tools): guaranteed divergence.
- **All code inside the Unity project**: the tools would need Unity batch mode, which makes CI slow and gives no
  headless solver.

## R3. Deterministic simulation model

**Decision**: The game uses **settle-to-fixpoint per command**, with a round-based work allocation.

**Commands.** Each accepted command is:

- a tap on a pod;
- a booster use;
- a rescue.

After a command is applied, the core resolves all automatic work until nothing more can happen. It then returns an
ordered **event log**. The next command always applies to that settled state. Animation only replays the log
(R4).

**Round algorithm**, repeated until a round changes nothing:

1. Compute reachability. Run a BFS from every Garden Entry over open cells, using orthogonal steps only. A tile-layer
   is *reachable* when it is the top layer of a cell that touches a reached open cell or an entry cell (FR-010).
   Record the route distance: the number of BFS steps from the nearest entry to the adjacent open cell, plus 1.
2. Build each active pod's candidates: its reachable tiles of the **exact variant** (FR-003), sorted by
   **(route distance ↑, row ↓ from the bottom, column ↑)**. This is the fixed, anticipatable tie-break of FR-021.
3. Allocate tiles to active pods in **slot-age order**, oldest first (FR-020). Each pod claims
   `min(remaining, candidates not yet claimed)`. This gives strict priority: a younger pod of the same variant only
   gets the leftovers.
4. Apply all claims at once:
   - decrement the pod counts (FR-017);
   - reveal the next layer, or open the cell (FR-036, FR-007);
   - collect keys (FR-033) and open their locks;
   - update special counters and trigger gates or Fountains (FR-037, FR-038);
   - reveal mystery tiles that became reachable (FR-039).
5. Pods whose remaining count is 0 leave, and their slots free up at once (FR-022).

**After the fixpoint**, the level state is checked in this order:

1. **Win** (FR-025) takes precedence.
2. **Jam** (FR-026): all usable slots are full and no active pod has a candidate.
3. **Stuck**: no active pod can progress and no tray pod can be committed. It is reported like a jam.

**Arithmetic and randomness rules:**

- All logic is integer-only.
- Collections are arrays or sorted lists; nothing depends on hash-map iteration order. *(Checked again on 2026-10-06,
  when the board limit grew from 14×16 to 22×28: `CellPos.GetHashCode` uses `CellPos.MaxWidth`, but rules and solvers walk
  cells by index, the Zobrist keys use the board's own row-major index, and no dictionary or set keyed by cells is
  enumerated in hash order, so every golden replay stayed byte-identical.)*
- There is no wall-clock time, no floating point and no `System.Random`.
- The only randomness, Shuffle (R10), uses a specified PRNG: SplitMix64 seeding xoshiro256\*\*. Its seed comes from
  `(level seed, content version, shuffle-use index, state hash)`.
- Budgets inside the core (solver nodes) are counted in **nodes, never in milliseconds**. The outcome is therefore
  identical on every device.

**Rationale**:

- The outcome depends only on the sequence of accepted commands (FR-024, SC-005).
- The player may tap while animations play (FR-016), because a tap always applies to the settled logical state.
- The solver searches over settled states, so its branching factor is the number of exposed pods, not time.
- Replays are trivial: a replay is the command list.
- Strict slot-age allocation matches the intuition "the first Water pod is served first". It also avoids two
  half-finished pods of the same variant each holding a slot.

**Alternatives considered**:

- **A real-time tick simulation**: the outcome would depend on tap timing, and the solver would have to model time.
  Violates FR-024.
- **One tile per pod per round, interleaved**: weaker priority. Two same-variant pods would both stall with partial
  counts.
- **Unordered hash sets**: platform-dependent iteration order.

## R4. Presentation: syncing animation with the logical state

**Decision**: The Unity presentation layer consumes event logs through a **timeline scheduler**.

- **Scheduling.** Each logical round becomes a visual *wave*. For each cleared tile a Bloomling walks from its entry
  along the BFS path (the positions come from the event data), then plays the family's restore animation.
- **Speed.** 2× speed only scales the timeline (FR-069).
- **Backlog compression.** If the pending visual time exceeds a threshold (default 12 s since 2026-10-05, 6 s from
  2026-10-03, 1.5 s before; remotely tunable), the scheduler speeds playback up to 4× and merges walkers. One sprite may then represent several
  tiles; this is visual only.
- **Input.** Input is evaluated against the **logical** state and gets immediate feedback within 0.1 s, independent of
  the backlog (FR-070, SC-008). A pod tapped while its target slot is still animating an exit is queued visually.
- **Amendment (2026-10-03, the owner's report: "two pods tapped one after another land in the same slot, and even in
  two slots they do not seem to work at the same time").** The rules were right: a tap settles at once, so a pod whose
  tiles are all reachable finishes and frees its slot at once (FR-022), and the next pod takes that slot (FR-014). The
  presentation was not: it played every tap's waves in one queue, and showed a pod in its rules' slot even while that
  slot still showed the finishing pod. Now:
  - the waves of one tap play one after another, as its rounds do, and the waves of different taps play side by side
    (FR-018 as the player sees it). A wave waits only as long as it must: each of its Bloomlings steps on a cell of its
    route, or reaches its target, only after that cell's earlier change has shown (a tile an earlier tap clears, a layer
    revealed under it), and a special's progress and a pod's leaving keep the rules' order. Wave ends, arrivals and
    starts play in time order (in that order at one moment), also when a booster shows everything at once, and the
    level's outcome (win, jam, stuck) shows after every wave an earlier tap still plays;
  - a committed pod shows in its rules' slot when that slot shows no pod, else in the first usable slot that shows
    none, else it waits in a queue until one frees. Where a pod shows never changes an outcome (the rules decide which
    slot it holds, FR-024); Return aims at the shown pod's slot in the rules;
  - the clearing pace is halved (the owner: "the initial board clearing speed must be halved"): 0.18 s a route step in
    the playtest (was 0.09), 0.14 s in Unity (was 0.07), waves of 0.6–3.2 s and 0.6–2.8 s (were 0.3–1.6 s and 0.3–1.4 s).
  `playtest/check` replays every golden case and showcase solution (with pauses and rapid taps) to the rules' state, and
  checks two quick taps on every level: wherever both taps have work their waves play side by side, and never in one
  slot.
- **Amendment (2026-10-05, the owner: "the board clearing speed at 1x must be halved").** The pace is halved again: 0.36 s
  a route step in the playtest (was 0.18), 0.28 s in Unity (was 0.14), waves of 1.2–6.4 s and 1.2–5.6 s (were
  0.6–3.2 s and 0.6–2.8 s); the restore keeps its time. 2× still doubles the clock, so it now plays at the old 1× pace.
  The backlog speed-up threshold doubles with it, to 12 s (Remote Config `fx.backlogThresholdMs` 12000, range
  2000–20000), so quick taps do not speed the slower clearing up again sooner than before. Presentation only: no
  outcome changes (FR-069).
- **Amendment (2026-10-05, the owner: "on hard levels I can tap quickly and the pods stack one after another; they
  must not pile up, the player must wait until a slot frees, or the jam is bypassed").** The rules settle a tap at once,
  so a finished pod frees its slot in the rules while its Bloomlings still walk on screen, and quick taps could commit
  pods the screen had no room for (they waited in the visual queue). Now a pod tap goes in only when a usable slot shows
  no pod on screen (one per pod of a connected group; `LevelAnimator.FreeOnScreen`, `SlotRowView.FreeOnScreen`); before
  that it gets the no-free-slot feedback and the rules never see it (FR-014 as amended). The input is still checked
  against the logical state first and answered within 0.1 s; the rules stay deterministic for the taps they get (the same
  definition and tap sequence give the same outcome), and the gate only decides when the player may make the next tap.
  The visual queue stays as a safety net. The level tester (instant results) keeps no gate.
- **Amendment (2026-10-04, the owner on L1: "if you pick all 3 at once, the first blue must finish before the green
  starts, though the greens could start running in the middle of the first blue").** A wave's Bloomlings set off
  together, so the leaf pod's whole wave waited for its farthest Bloomling, whose route crossed the last tiles the
  second water pod clears (about 9.7 s), while its tile beside the entry was free after the first step. Now each
  Bloomling (each walker, in Unity a merged walker) sets off on its own as soon as every cell of its route and its
  target has shown its earlier change; the wave ends after its last arrival, its end events still in the rules' order
  (both builds: `LevelAnimator`, `TimelinePlayer`). On L1 the leaf pod's first Bloomling now sets off at about 2 s, while
  the first water pod still works (`playtest/check`, `EventTimelineTests`).
- **Auto 2× (2026-10-04, the owner: "when all slots are picked, nothing more to choose from, 2× must turn on by
  itself").** While no exposed pod can be tapped (every pod picked, or the level decided), the animation plays at 2×
  (at least; the player's 2× stays 2×) and the speed pill shows 2×; the player's saved choice is unchanged. Re-checked
  after every command (both builds: `LevelScreen.RefreshSpeed`, `GameplayController.RefreshSpeed`). Animation only
  (FR-069).
- **Amendment (2026-10-06, the owner: "it must be mesmerizing; in the reference game the ants carry slowly and
  beautifully, you just sit and watch", then "not too fast, or a whole level lasts ten seconds; something in
  between").** The clearing plays in one of seven styles (spec 005 FR-038). Every style takes the same time for a tile n
  route cells from its entry: `ClearStyles.TripSeconds(n)` = (1.1 s × n + 1.4 s) / 1.5, from the Bloomling leaving the
  arch to the tile's clear (the slot's count going down; the owner, later on 2026-10-06: each cell clears 1.5 times as
  fast, `ClearStyles.SpeedUp`, so a level's clearing takes two thirds of the time with the same look). Each style splits that time into legs: out, an act at the tile, an
  optional way back, and the tile's last leg into the slot (`ClearStyles.LegsOf`). The waves' length clamps at 1.2–40 s
  (was 1.2–6.4 s and 1.2–5.6 s; 1.2–60 s since 2026-10-06, so a straight route of 49 cells across the 22×28 board of a
  big level, about 37 s at the faster pace, fits too), so no trip is squeezed. A pod's Bloomlings leave each arch in a
  line, at least `ClearStyles.LineGap` (0.42 s / 1.5 = 0.28 s, so the line keeps its spacing on the board) apart,
  nearer tiles first, its later rounds and taps joining the line; different pods'
  lines run side by side (FR-018; on L1 the leaf pod's line still sets off while the first water pod works). A tap's later rounds no longer wait for
  its earlier rounds to end: each Bloomling waits only for its way, as before, and the rounds' end events keep the rules'
  order. A later Bloomling may cross a cell once its tile is gone from it (eaten, picked up, in a bubble; the style's
  out and act legs) unless a layer comes up under it. The backlog speed-up waits for 60 s of backlog
  (`fx.backlogThresholdMs` 60000, range 2000–120000), so the calm pace is kept in normal play. 2× and auto 2× still
  double the clock. Presentation only: no outcome changes (FR-069), both builds (`LevelAnimator`, `TimelinePlayer`).
- **Worker cap.** Active Bloomling sprites come from a bounded pool of about 60 on low-end devices; extra work is shown
  aggregated.

**Rationale**:

- Keeps the Colony-Flow feel of workers streaming out.
- Guarantees that the visuals never influence rules.
- Keeps large pods (100+) and low-end devices smooth (SC-008).

**Alternatives considered**:

- **Blocking input until animations finish**: kills flow, and FR-016 forbids it.
- **Simulating per frame**: non-deterministic.

## R5. Level definition format and runtime expansion

**Decision**:

**Authoring and interchange format.** Levels use JSON validated by
[`contracts/level-definition.schema.json`](contracts/level-definition.schema.json). A definition does not repeat the
picture. It stores:

- a reference to the base picture: id, version, mirroring and background treatment;
- a mapping from **role to variant**;
- sparse **overlays**: hidden layers, mystery flags, stone and hole deviations, keys and specials;
- **Garden Entries**;
- **slots**;
- the **Source Tray**, with stacks and pods listed explicitly;
- the **board look** (`boardLook`, since 2026-10-06, spec FR-036 as amended): `peek` (candy tiles with the next-layer
  chip) for boards of up to 288 cells, `icons` (icons only, the next layer hidden) for boards over 288 cells. It is
  presentation that the level data fixes, never the device. It is optional, so content written before it loads and
  peeks; the generator always writes it from the cell count (`BoardLooks.For`), the writer keeps a stated `peek` (the one
  field written although it equals its default), and validation fails a level whose look disagrees with the rule.

**Runtime expansion.** The runtime expands the definition deterministically, in this order:

1. the picture grid;
2. mirroring;
3. the role→variant mapping (which gives the visible top layer, FR-006);
4. the overlays;
5. the board.

This is the "compact config that the game expands" the product owner asked for (FR-077).

**Shipping format:**

| Item | Format |
|---|---|
| Level packs | Gzip-compressed JSON Lines, 250 levels per pack |
| Picture library | One pack |
| Manifest | [`contracts/content-manifest.schema.json`](contracts/content-manifest.schema.json), with a SHA-256 per pack |

- The solution traces and metrics live in separate **validation records**. Those are kept in the repository and CI
  artifacts and are not shipped to clients.
- Size estimate: about 0.3–0.8 KB per level gzipped, so about 2–4 MB for 5000 levels. The picture library is about
  1 MB.

**Rationale**:

- The top layer derives from the shared picture, so per-level data stays small.
- The definition is still fully explicit. Stability does not depend on freezing generator code (FR-076).
- JSON is diff-able in review and has schema tooling.

**Alternatives considered**:

- **Seed-only configs expanded by shipping the generator**: every generator change would silently re-shape levels
  unless the old code were kept forever. That is fragile against FR-076.
- **A binary format such as MessagePack**: smaller, but not needed at this size. It can be swapped behind the loader
  later.
- **One Unity asset per level**: forbidden by doc 15.

## R6. Content versioning and delivery

**Decision**:

**Identifiers.** Every level is identified by `(levelNumber, definitionVersion)`. The catalog as a whole has an integer
`contentVersion`.

**Launch.** The launch catalog is bundled in `StreamingAssets` (FR-078). It contains:

- all level packs;
- the picture library;
- the daily challenge pool;
- the manifest.

**Later content.** Packs and fixes are downloaded over HTTPS from a CDN, as listed in a remote manifest whose URL comes
from Remote Config. Each pack's hash is verified before it is activated (FR-078).

**Level pinning.** An attempt in progress always finishes on the definition it started with. A deliberately versioned
fix applies from the next attempt (spec edge case).

**Art DLC.** Unity **Addressables** is used only for art-type DLC: bespoke finished illustrations, themes and
cosmetics.

**Rationale**: Level data is plain data. A hash-checked downloader is simpler and more transparent than Addressables
for JSON, and Addressables is the right tool for the art bundles.

**Alternatives considered**:

- **Addressables for everything**: heavier, and it hides data diffs.
- **Always online**: violates offline-first (FR-074).

## R7. Picture library pipeline

**Decision**:

**Authoring.** Each base picture is an **indexed-color PNG**, whose palette index is the role, plus a sidecar JSON with
metadata ([`contracts/base-picture.schema.json`](contracts/base-picture.schema.json)). They are authored in any pixel
editor, such as Aseprite or Pixelorama.

**Import.** The pipeline imports each picture into a role grid (at most 22×28 since 2026-10-06, the big levels' largest
board; it was 14×16) and computes structure metrics: region count, nesting depth relative to a bottom entry, and
background share. The format allows 7×8 to 22×28; which sizes a level may use is the level band's board rule (FR-008):
11–12×12 for the curated Levels 1–10, 224–288 cells (14×16 to 16×18) for the regular levels from L11, and 289–616 cells
for the big levels.

**Review.** The review status is recorded in the metadata. Only `approved` pictures can be used (FR-084).

**Generated candidates.** Candidates can be produced by downscaling and quantizing owned or licensed vector or
illustration sources into roles. The source and licence are recorded (FR-091).

**Finished look.**

- By default it is **rendered automatically** in the client from the role grid and the mapped variant colors: flat
  cells, merged rounded regions, a soft light palette and no symbols (FR-007).
- An optional bespoke illustration can be referenced by the picture id and loaded through Addressables.

**Rationale**:

- PNG and a palette are the lowest-friction format for artists and scripts alike.
- Automatic rendering keeps the 1000–1500 pictures affordable (doc 12 §10).

**Alternatives considered**:

- **Hand-drawn finished art for every picture**: unaffordable at scale.
- **Vector sources used directly**: a poor fit for the small grid.

## R8. Solver

**Decision**: The solver is a deterministic **depth-first search over settled states** (R3).

**Search:**

- **Transposition table**: states are hashed with Zobrist hashing over cell layers, open cells, pod remaining counts,
  tray exposure, slots, keys and special states.
- **Move ordering**: pods that can progress at once come first; pods that would sit idle come last.
- **Symmetry pruning**: exposed pods that are identical (same variant, count and modifiers, with the same pods below
  them) are equivalent, so only one is explored.
- **Dominance pruning** on slot usage.
- **Budget**: a node budget per level. Results are `solvable`, `unsolvable` or `unknown`; `unknown` counts as a
  rejection.

**Outputs:**

- one winning trace (FR-080);
- a **jam witness** trace, which proves the level can be lost (FR-081);
- difficulty metrics from the search tree, used for scoring (FR-082, doc 06 §16):
  - dependency depth and branching;
  - unsafe-choice density;
  - dead-end depth;
  - peak and mean buffer occupancy on winning lines;
  - connected commitments;
  - the variant and special load.

**Player-information fairness (mystery mechanics).** Solving becomes an AND-OR search. At each mystery reveal, the
player's strategy must win for **every** variant assignment consistent with what the player can deduce from the
visible per-variant accounting. The solver caps mystery load per level (at most 2 mystery pods plus 3 mystery tiles)
to keep this tractable. Levels fail if only hidden knowledge avoids a forced loss (FR-039, FR-080).

**Performance targets** on a CI runner:

- median under 1 s per level;
- p99 under 30 s;
- the full 5000-level catalog solved nightly within 2 hours across parallel jobs.

**Rationale**:

- Settled states give small branching: the number of exposed pods is usually 3–6.
- Transpositions are frequent because different tap orders often converge.
- Node budgets keep the result reproducible.

**Alternatives considered**:

- **Breadth-first search**: memory-heavy at depth 30+.
- **Monte Carlo sampling**: cannot prove unsolvability.
- **SAT or constraint solvers**: complex to model reachability dynamics and harder to extract metrics from.

## R8b. Hidden-layer fairness on icons boards (2026-10-06)

**Context.** The owner (2026-10-06): a board over 288 cells (a big level) shows icons only, and a layered tile no longer
shows its next layer; the hidden layers are a surprise (spec FR-036 as amended). The player sees the top layers, the
tray and every event, and can count from the pods how many hidden layers of each variant there are, but not where they
lie. No level may force a blind guess (FR-080), so the hidden layers need the player-information check that mystery
tiles have (FR-039). R8's AND-OR search enumerates every world and caps mystery at 3 tiles and 2 pods; a big board hides
dozens of layers whose placements number in the billions, so it cannot be enumerated.

**Decision**: a sampled check with a player that uses only visible information (`Bloomlings.Solver.HiddenLayerFairness`,
which `FairnessChecker` runs for every `icons` board; the generator's acceptance and `CatalogValidator` call it, and the
validation record stores the result as `playerInfoFair` with the `player-info-fair` check).

1. **Worlds.** A world keeps the level's hidden layers, as the player counts them (each variant's pod total less its
   visible tiles), so the exact per-variant totals the pods show hold and every hidden layer is an active variant, and
   places them on random target cells, at most the level's layers per cell (FR-036: 1 before L125, 2 after). The level's
   seed draws up to 24 worlds and keeps the first 12 that a full-information search (4000 nodes) can win: a world no one
   could win is one the player rules out, since every shipped level is winnable (FR-046). If fewer than 6 of the
   wanted 12 turn up winnable, the level is unfair: whether it can be won hangs on where the hidden layers lie.
2. **The visible player.** It keeps its taps and what each one showed, and plans on a *model world*: the level with the
   layers it has seen revealed where they were and the layers it has not seen drawn at random under the cells that may
   still hold them, from a seed made of what it has seen (equal observations give equal choices in every world). It
   follows the model's winning line (a 2000-node search) while the model foretells every event; when a revealed layer
   surprises it, it draws a new model, replays its taps into it and plans again (up to 3 models, then the first tap of
   the search's move order, which reads only visible state).
3. **The verdict.** The level is fair when the visible player wins the real level and all 12 kept worlds. It is unfair
   when it loses one, when too few worlds can be won, or with more than 72 hidden layers; a mystery tile or pod on an
   icons board is "uncovered" and fails, because the check does not judge both kinds of hidden information together.
4. **Budgets.** All fixed: 4000 nodes per world, 2000 per plan, 300 000 for the whole check (every applied command
   counts), so the generator and the validator always agree, whatever their solve budgets.
5. **Generator slack.** An icons board gets fewer hidden layers (4–9% of its tiles instead of 8–17%, at most 72), no
   mystery tile or pod, and a big level a lower buffer pressure (peak 1–3 slots), which leaves room for a pod that
   waits for a layer it could not foresee. Big levels are Normal: the difficulty schedule moves a Hard due on one to
   the next level (`DifficultySchedule`), because the tray tuner's injections barely move a big board's score (a trial
   on a 40-pod big level: 2124 to 2128 in 40 attempts, the peak buffer staying at 1), and their own class thresholds
   (`difficulty-thresholds.json` `big`, 1500 over the band's) keep their scale from reading as Hard.

**This is sampled, not a proof.** Its limits, stated plainly:

- A placement that is never drawn may still defeat the visible player: 12 worlds sample a huge space. Passing shows
  that one strategy without hidden knowledge wins the real level and 12 random placements, not all of them.
- The visible player is one heuristic strategy (planning on one sampled model, replanning on surprise). A level it
  loses may be fair for a smarter player (a false rejection costs a generator candidate); a level it wins on the sample
  may still be unfair for an unlucky placement.
- Dropping the worlds no one could win assumes that the player trusts every level to be winnable.
- Peek boards are unchanged: a depth-3 tile's third layer shows only once the second is its top, as before, and no check
  judges it.

**Trial (2026-10-06).** Relabelling the 15 layered levels of the playtest's Levels 28–100 as icons boards: every
Normal and most Hard levels pass, while a Super Hard (L56) and a Hard (L78) level fail; the tighter the buffer, the
more an unforeseen layer hurts. On sketched big pictures (up to 22×28, 23–62 hidden layers) the generated big levels
passed in about 1–30 s, and the check rejected one big candidate in about ten.

**Alternatives considered**:

- **R8's enumeration**: intractable at dozens of hidden layers.
- **R8's AND-OR search over the sampled worlds**: after the first reveal that tells two worlds apart each world is alone,
  and the search then backtracks knowing that world's later hidden layers. It checks that every placement can be won,
  not that a player can find the way.
- **A greedy visible player without planning**: much weaker; it would reject most levels.
- **Showing that a tile is layered without its variant**: not what the owner chose ("icons only").

## R9. Generator (picture-first, solution-first)

**Decision**: The generator runs doc 06 §1–13 and FR-079 as a seeded pipeline.

1. **Pick a picture.** Choose an approved base picture that matches the profile: size, tags and structure targets, and
   the level's board rule (FR-008 as amended on 2026-10-06: 224–288 cells from L11, and 289–616 cells, at most 22×28,
   only for a big level, every milestone level from L525). Skip pictures blocked by the similarity window of FR-083: 100
   unique pictures in levels 1–100, and a 50-level repeat window. The level stores its board look from the cell count
   (R5), and an icons board gets the slack and the fairness check of R8b.
2. **Map roles to variants.** Enumerate the role→variant mappings allowed by the band's variant pool and color groups
   (FR-004, FR-060). Merge similar roles if there are too many. Enforce readability pairs (FR-005).
3. **Place entries and derive the dependency graph.** Compute shielding regions from the Garden Entries. Choose the
   entry placement and mirroring toward the structure target.
4. **Add optional overlays.** Add hidden layers, keys, specials and locks, only those unlocked for the band (FR-031).
5. **Plan the solution.** Simulate a planned "good play" policy on the core. It produces the waves: the order in which
   each variant's work becomes reachable.
6. **Partition into pods.** Split each variant's demand (FR-023) into pods along the planned waves, within the pod-size
   ranges.
7. **Build the Source Tray.** Place the planned pods so that the plan is feasible. Then inject difficulty: tempting
   premature pods, buried needs, connected and locked structures. Re-solve after each injection and stop when the
   difficulty score reaches the target class.
8. **Validate and score.** Run the full solver: winnable, jam witness, fairness and metrics. Then classify the level as
   Normal, Hard or Super Hard (FR-082). Reject and retry with the next seed if needed.
9. **Emit output.** Write the definition and the validation record.

The generator code is versioned. Published definitions never depend on re-running it (R5).

**Rationale**: This construction guarantees at least one solution by design and lets the solver certify it. Difficulty
is steered by the Source design, which is where doc 06 says it should come from, rather than by random painting.

**Alternatives considered**:

- **Graph-first spatialization**: superseded by the picture-first decision.
- **Random tray plus rejection only**: wastes compute and gives poor control over difficulty.

## R10. Runtime Shuffle with a winnability guarantee (FR-044)

**Decision**: Shuffle is **constructive** and runs inside the core, on a worker thread in Unity.

1. **Run the relaxed solver.** From the current state, solve a *relaxed* problem in which any remaining eligible pod
   may be committed at any time, while respecting locks and connections. The search has a fixed **node** budget.
2. **Lay out a found solution.** If the relaxed solver finds a solution, lay its pod order out **round-robin across
   the existing stacks**. Pod `i` goes to stack `i mod k` at depth `⌊i/k⌋`, and connected members are placed at the
   same depth. This makes the order exactly realizable.
3. **Verify.** Check the arrangement with the normal solver, also under a node budget.
4. **Fall back if needed.** If verification fails or runs out of budget, try the next candidate from the seeded PRNG
   (R3). The last resort is the arrangement that maximizes the number of exposed pods that can progress at once.
5. **Keep the rules.** Locked pods keep their locks, connected pods stay connected, and waiting pods are untouched.
6. **Look shuffled** *(amendment 2026-10-06, the owner: "I press it and they don't shuffle")*. The first relaxed line
   used to be the tray's own order, dealt back the same way: the same pods on top, the columns at most moved over.
   Now the relaxed search tries each step's units in a seeded order (progressing units still first), each row of the
   deal takes its stacks in a seeded order (which keeps the order realizable), up to four lines are dealt before the
   seeded candidates, and a verified arrangement is taken only if it looks shuffled as the player sees the tray
   (`ShufflePlanner.LooksShuffled`: each pod by its variant or "?", count and lock in the three shown rows; at most half
   the columns show what a column showed before, and at most half the exposed pods look like the ones before). If none
   does within the budget, the first verified arrangement is used, so the winnability rule never weakens. On the
   playtest's Levels 1–100 the old plan left 76 of 98 trays looking alike, the new one none.

**Rationale**:

- If the relaxed problem is solvable, the round-robin layout realizes it, because the exposure order matches the
  solution order.
- If the relaxed problem is unsolvable, no arrangement can be won. Both cases satisfy "if any arrangement can be won,
  the result is winnable" within the budget.
- Node budgets keep the result identical on every device (FR-024).

**Alternatives considered**:

- **Random shuffle**: violates FR-044.
- **Precomputed shuffle results per state**: the state space is unbounded.
- **A wall-clock budget**: non-deterministic across devices.

## R11. Backend services

**Decision**: **Unity Gaming Services** behind interfaces (`IAuthService`, `ICloudSaveService`, `ILeaderboardService`,
`IRemoteConfigService`), with contracts in [`contracts/backend-services.md`](contracts/backend-services.md):

- **Authentication**: anonymous sign-in on first launch (FR-087), with optional linking to Sign in with Apple and Google
  Play Games.
- **Cloud Save** for the player save.
- **Leaderboards**: UGS breaks ties by PlayerID, so the score encodes time. The score is
  `level × 10⁷ + (10⁷ − 1 − minutesSince(2026-01-01T00:00Z))`, sorted descending. The earlier completion ranks higher
  (FR-062).
- **Remote Config** for economy, ads and feature flags (FR-085).
- **Cloud Code** for:
  - purchase receipt validation (FR-089);
  - leaderboard submission sanity checks: monotonic progress, a plausible level-jump rate, a supported content version.

**Rationale**:

- UGS has first-party Unity SDKs and covers every doc 15 backend need, including leaderboards out of the box.
- It has a free tier suited to pre-launch.
- The interfaces keep vendors replaceable, which doc 15 requires.

**Alternatives considered**:

- **Firebase** (Firestore, Functions, Remote Config): no native leaderboards.
- **PlayFab**: capable but heavier for this scope.
- **A custom backend**: operations cost with no product benefit.

## R12. Ads and consent

**Decision**: An `IAdsService` sits behind a client-side **AdPolicy**. AdPolicy enforces FR-052 and FR-053:

- interstitials only on the post-win transition;
- none in levels 1–10 or right after a fail;
- caps by both time and number of levels, remotely tunable;
- no interstitials with Remove Ads.

Rewarded ads are only started by the player and are limited to one rescue per attempt.

**Consent**:

- **Google UMP** for GDPR and US-state consent.
- **Apple ATT** on iOS before any personalised ads (FR-090).

The initial integration is the **Google Mobile Ads** Unity plugin with mediation, the direction named in doc 15. The
final mediation vendor is a commercial choice (see *Deferred decisions*).

**Rationale**: Placement rules live in the game's own code, so they are testable (SC-013) whatever SDK is used.

**Alternatives considered**: Integrating AppLovin MAX or Unity LevelPlay directly. That remains possible behind the same
interface.

## R13. In-app purchases

**Decision**: **Unity IAP** (`com.unity.purchasing` v5), which covers Google Play Billing and StoreKit 2. The products
are:

| Product | Type |
|---|---|
| Petal packs | Consumable |
| Booster bundles | Consumable |
| Starter pack | Consumable, offered once, with the offer flag stored server-side |
| Remove Ads | Non-consumable |

- Purchases are validated through Cloud Code against the store APIs. A local check is a fallback only.
- Entitlements live in the local save and in Cloud Save. Restore Purchases is available in Settings (FR-073, FR-054).
- Consumable grants are recorded in a **purchase ledger** keyed by transaction id. This makes grants idempotent across
  devices and syncs.

**Rationale**: Unity IAP is the standard cross-store path. The ledger prevents both double grants and lost grants.

**Alternatives considered**: Native store plugins (twice the work) or RevenueCat (an extra vendor, not needed at
launch).

## R14. Analytics and crash reporting

**Decision**:

**Analytics.** An `IAnalyticsService` abstraction, with the event catalog in
[`contracts/analytics-events.md`](contracts/analytics-events.md). The initial provider is **Firebase Analytics**.
Per-level funnels are exported for difficulty tuning (FR-086).

**Crash reporting.** **Firebase Crashlytics** for crashes and non-fatal errors. Every report carries these custom keys:
`app_version`, `content_version`, `level_number`, `definition_version`, `picture_id`. A report can then be reproduced
through the pipeline's `replay` command (doc 15 §14).

**Rationale**: Crashlytics is the de-facto mobile standard, and the abstraction keeps the analytics vendor replaceable.

**Alternatives considered**: UGS Analytics, GameAnalytics, or Sentry for crashes.

## R15. Local save and cloud sync

**Decision**:

**Local save:**

- one JSON document ([`contracts/player-save.schema.json`](contracts/player-save.schema.json)) with a `schemaVersion`
  and forward migrations;
- written atomically (temp file plus rename) with a checksum;
- saved after meaningful events: a win, a purchase, a claim, a settings change, a booster use;
- **not** saved in the middle of a level, because a killed level restarts (spec edge case).

**Cloud sync merge**, following FR-087:

1. The base is the save with the higher `highestCompletedLevel`, then the later `updatedAt`.
2. Entitlements, cosmetics and claimed milestones are the union of both saves.
3. Petals and consumables come from the base, plus purchase-ledger entries missing from the base, which are re-applied
   idempotently.

**Rationale**: This preserves the furthest valid progress and never loses a purchase.

**Alternatives considered**:

- **Last-write-wins**: can lose progress or purchases.
- **Server-authoritative economy**: too heavy for a casual offline-first game (doc 15 §23).

## R16. Platforms and performance targets

**Decision**:

**Platforms:** iOS 15+ and Android 8.0 (API 26)+, arm64, phones in portrait. Tablets are supported by letterboxing and
scaling.

**Reference low-end devices:**

- Android: about 3 GB RAM with a Mali-G52-class GPU, in the Galaxy A1x class.
- iOS: iPhone SE (2nd gen).

**Targets:**

| Metric | Target |
|---|---|
| Frame rate on mid and high devices | 60 fps |
| Frame rate on reference low-end, largest board | 30 fps floor, no hitches over 100 ms |
| Tap feedback | ≤ 0.1 s |
| Level load | ≤ 1 s |
| Cold start to Home | ≤ 5 s on low-end |
| Install size | ≤ 150 MB |
| Memory | ≤ 350 MB |

**Rationale**: This covers the vast majority of active casual-puzzle devices while keeping the SC-008 smoothness
achievable.

**Alternatives considered**: Android 7 or iOS 13 support, which adds QA cost for negligible reach.

## R17. Testing strategy and CI

**Decision**:

**Core tests:** NUnit through `dotnet test`, including:

- rule tests, covering every doc 01 §20 pre-lock test;
- **FsCheck** property tests for invariants: accounting reconciles, counts never go negative, determinism, and a win
  never co-occurs with a jam;
- **golden replay corpus**: definitions plus command logs plus the expected final state hash.

**Unity tests:** the Unity Test Framework in EditMode and PlayMode. The same golden corpus runs in EditMode and in an
**IL2CPP device build**, to prove cross-platform determinism (SC-005, SC-011).

**GitHub Actions:**

| Trigger | What runs |
|---|---|
| Pull request | Core build and tests; validation of changed pictures, levels and profiles |
| Nightly | Full-catalog solve and statistics (SC-004, SC-012) |
| Release | Unity builds through GameCI, which needs the Unity licence secrets |

**Rationale**: This matches doc 15 §20–21: the content is validated like code, and the game is testable without
watching animations.

**Alternatives considered**: Unity Build Automation instead of GameCI (viable, paid). xUnit (equivalent; NUnit keeps
one framework with Unity).

## R18. Localization

**Decision**: The Unity Localization package, English first. All player-facing strings are table-driven.

**Rationale**: The UI has minimal text (FR-070), so adding languages later is cheap.

## R19. Daily Challenge

**Decision**:

- A pre-generated **daily pool**, built with the same pipeline and a `daily` profile, ships in the content packs.
- The challenge for a day is selected by `UTC date → pool index` through a fixed mapping. This makes it identical for
  all players (FR-064).
- It unlocks at L50 behind the remote flag `feature.dailyChallenge`, so it can be cut without a build.

**Rationale**: Deterministic, offline-capable, and it reuses validated content.

**Alternatives considered**: Server-picked puzzles (needs a connection) or generating on device (would require shipping
the generator).

**Amended 2026-10-07 (the owner, FR-064 as amended).** The pool has 365 entries, each on a picture of its own at 22×28.
Entries are Normal, Hard or Super Hard by the week.

- **Pictures.** 128 subjects that the levels never draw, three pictures each (384), carry the theme `daily`. The picker
  gives them only to a profile that asks for that theme, and the `daily` profile takes nothing else. So the 5000 levels
  and the pool never share a picture, and `validate` refuses one in a level.
- **Plan.** `DailyPlan` fixes each entry's picture before any level is generated, so the pool's segments can be built in
  parallel without ever repeating a picture. In order, each entry draws a subject from those with the most pictures
  left that it has not shown in the last 60 entries, then one of that subject's pictures (both seeded). The 19
  pictures left over are spares: an entry whose picture makes no level takes the first spare that keeps the window.
  A picker that chose freely within each segment would repeat pictures across the segments (about 150 pairs for 7
  segments of 52 among 384 pictures). The seam repair would then have to redo them all.
- **Classes.** Four Normal, two Hard and one Super Hard a week, by the weekday of the entry. Entry 1 is Thursday
  2026-01-01: Hard on Wednesday and Saturday, Super Hard on Sunday, and Normal on the Monday after it. That gives 209
  Normal, 104 Hard and 52 Super Hard entries. The weekdays move by one a year from 2027, as the pool starts over every
  365 days. A 364-entry pool would keep them fixed, but the owner asked for a year.
- **Rules level.** An entry's number is a day, not Level N, so every entry plays with the unlocks of L50, the
  challenge's own unlock: its mechanics, one layer below a top, no advanced combination or Hard pressure. Both the
  generator (`LevelGenerator.RulesLevel`) and the validator (`CatalogValidator.RulesLevel`) use it.
- **Pressure.** The board is an icons board, so the buffer pressure is that of a big level (R8b): a Normal entry peaks
  at 1–3 slots, and a Hard or Super Hard one at 3–4.
- **Thresholds.** The pool has its own: Hard from 3150, Super Hard from 3500 (`difficulty-thresholds.json` `daily`). They
  come from trial generation on the 24 big pictures of 22×28. Normal levels scored 2135–3576. The best score the tray
  tuner reached on 718 Hard and Super Hard candidates was 2760 at the median, 3477 at the 90th percentile and 4544 at
  most, so the big bands' Super Hard minimum of 4600 was out of reach. On a board this big the tuner's injections
  barely move the score (R8b). A day's class therefore follows the level the generator draws: its pods, work, layers,
  mechanics and peak pressure. About one candidate in eight reaches Hard and one in eleven Super Hard.

## R20. Economy configuration

**Decision**:

- All economy numbers live in Remote Config, with bundled defaults: Petal rewards, bonuses, booster prices, unlock
  grants, milestone bundles, ad caps and shop offers.
- The client clamps them to validated ranges.
- Starting values follow the spec assumptions: a win pays about 12–30, a booster costs about 40–60.

**Rationale**: FR-085 requires tuning without releasing a build. Clamping prevents a bad config from breaking the
economy.

---

## Deferred decisions (non-blocking)

| Item | Owner | Default until decided |
|---|---|---|
| Ads mediation vendor | Business | Google Mobile Ads + mediation behind `IAdsService` |
| Bloom Burst final semantics (doc 09) | Design + solver review | Full variant removal (FR-050) |
| Exact economy numbers and ad caps | Design (balancing) | Remote Config defaults (R20) |
| Level 8 unlock: Mystery vs Key | Design (fairness tests) | Key preview until the mystery fairness solver passes |
| L40 cosmetic, variants entering at L45/L200 | Design | Roadmap placeholders |
| Daily Challenge in launch scope | Product | Built behind a flag (R19) |

## Sources

- [Unity 6.3 LTS is now available — Unity Blog](https://unity.com/blog/unity-6-3-lts-is-now-available)
- [Unity 6 Releases & Support](https://unity.com/releases/unity-6/support)
- [What happens if multiple players have the same score on a leaderboard? — Unity Support](https://support.unity.com/hc/en-us/articles/35350951608212-What-Happens-if-Multiple-Players-Have-the-Same-Score-on-a-Leaderboard)
- [Unity Leaderboards documentation](https://docs.unity.com/en-us/leaderboards)
