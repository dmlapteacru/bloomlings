# Feature Specification: Round Levels in the Game

**Feature Branch**: `006-round-levels-in-game` (work continues on the session branch `claude/great-darwin-6qrpj8`)

**Created**: 2026-10-27

**Status**: Draft

**Input**: User description: "Round levels in the game (spec 006). Make the rules of the round engine (the rules the owner
tested in pilots v9–v12 and on which the round levels are built, solved and measured) the game's rules, so confirmed
round waves can be transferred into the game, starting with L1–L20 (pilot v12; L18 as is, marked as an exception), with
the owner's presentation decisions of 2026-10-27 (keep the frame and the cubes, keep the clearing animation in live time,
round palette, no variant icons on cubes, Colony Flow-style link tubes on the pods' front, Auto at 2× with the HUD
hidden, the normal speed-up kept, introduction popups with a reminder and the reveal glow)."

Background: [`product/plans/round-transfer-proposal-2026-10-27.md`](../../product/plans/round-transfer-proposal-2026-10-27.md)
(the gap between the round engine and today's game rules), [`product/17_LEVEL_GENERATION_RULES.md`](../../product/17_LEVEL_GENERATION_RULES.md)
(how round levels are built and checked), [`content/rounds/owner-notes.md`](../../content/rounds/owner-notes.md) (the
owner's notes from the pilots).

## Clarifications

### Session 2026-10-27

- Q: Icons on the cubes — constitution II asks for "hue and icon"; what now? → A: amend constitution II. On board
  cubes a variant is identified by its colour, readability-checked under normal vision, plus the symbols mode for
  colour-blind players. Hero icons on cubes come back as a separate feature. Constitution 1.0.2 → 1.1.0.
- Q: The tap's time becomes part of the command; how is that written down? → A: clarify constitution III and doc 15. A
  command carries model time as a whole number; device time stays forbidden in rules. This ships in the same 1.1.0
  amendment. The unit is whole microseconds: the pilot's recorded commands carry fractional milliseconds (335 of 407),
  and microseconds replay them faithfully.
- Q: Readability of the 16 round colours? → A (revised the same day after measuring): under normal vision all 120
  pairs pass (smallest CIEDE2000 ≥ 10). Under the protanopia, deuteranopia and tritanopia simulations 11 pairs merge
  (Слива/Глубина 0.7), and they occur in 11 of the 19 placed levels and 86 of 116 approved pictures. Sixteen colours
  cannot be told apart by colour alone for colour-blind players, so the game gets a **symbols mode** in Settings: each
  material's symbol shows on cubes, pods and places. It is off by default, so cubes carry no icons, as the owner decided.
  The palette and the levels stay as they are.
- Q: What does the game show after the last transferred round level (now L20) until more waves are transferred? →
  A: round levels only. After the last one, a "new levels soon" screen. One rule set: the old rules and the old levels
  leave the game builds. Meta at later levels (L40, L50 …) is checked through the tester's level jump.
- Q: What happens to the game's boosters (Extra Slot, Shuffle, Return, Bloom Burst) and the jam card on round levels
  with live time? → A: they move to live time in this spec: each booster's effect and when it may be used during work
  are defined on the live timeline. Levels are still validated without boosters.
- Q: How do the Bloomlings that walk the board and eat the cubes look for the 16 round colours? → A: as the hero of
  the material's family, tinted in the material's colour. No new art is needed, and the clearing styles work as now.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The owner plays L1–L20 in the game exactly as in the pilot (Priority: P1)

The owner opens the full playtest app on the phone, plays round levels L1–L20 and finds that every level behaves as it
did in pilot v12. The same taps at the same moments give the same result. The same level is as hard as it was. Hidden
pods reveal on reaching the top, links and the bridge work the same way, and work walks across the board in live time.

**Why this priority**: Without the same rules, a transferred level is a different level. Its saved solution, its
measured class and everything the owner approved in the pilots would no longer hold. Everything else in this feature
depends on it.

**Independent Test**: Install the playtest app built with L1–L20, play any level, and compare with pilot v12. Separately,
the conformance check (User Story 3) replays every level's saved solution, every random-test game and the owner's
recorded pilot v12 attempts in the game's rules and finds no difference.

**Acceptance Scenarios**:

1. **Given** the playtest app with round levels, **When** the owner plays L5 with the same taps they made in pilot v12,
   **Then** the level ends the same way (jam or win) at the same point.
2. **Given** a level with hidden pods (L6, L8), **When** a hidden pod becomes the top pod of its column, **Then** its
   colour and count show at once, before any choice, whether or not a place is free.
3. **Given** a level with links (L11), **When** the owner taps a linked pod whose partner is neither on top of its column
   nor directly under a pod of the same link, **Then** the tap is refused with the reason shown. **When** the partner is
   available and two places are free, **Then** both pods launch with one tap, and both places free together only after
   the whole pair has finished.
4. **Given** a bridge level (L16), **When** the approach to a bridge site has been cleared, **Then** that bridge can be
   built. Building it takes no pod and no place, it is permanent, and the other site can no longer be built.
5. **Given** any level, **When** pods are working, **Then** the owner can launch more pods while earlier ones are still
   walking. A place frees only after its pod's last worker has left (plus the fixed short delay), as in the pilot.

---

### User Story 2 - The level looks and feels like the game, with the owner's presentation decisions (Priority: P1)

The owner sees round levels in the game's own look: the frame and the board cubes the game already draws, the round
palette, cubes without variant icons, the cubes' clearing ("eating") animation running in step with the live work, links
shown as Colony Flow does, Auto that speeds the end up to 2× and hides the whole HUD, the normal speed-up button, and
the introduction popups with their reminder and the reveal glow.

**Why this priority**: The owner decided these on 2026-10-27. Together with Story 1, they make the transferred levels
the game the owner wants, not a port of the pilot page.

**Independent Test**: Play L1–L20 in the playtest app and check each presentation decision on screen. The
presentation-only checks (no icons on cubes, link tubes, HUD hidden under Auto, popups on L6/L8, L11/L13, L16/L18)
pass in the automated screen-render checks.

**Acceptance Scenarios**:

1. **Given** any round level, **When** it opens, **Then** the board is the game's frame with the game's cubes in the
   round palette, and no cube carries a leaf, flower or other variant icon.
2. **Given** a pod working on the board, **When** its workers reach cells, **Then** each cube's clearing animation plays
   when that cell is cleared on the live timeline, not all at once.
3. **Given** two linked pods, **When** they stand in the columns or in the places, **Then** they are joined by a tube
   attached to their front faces (not drawn above or on top of the pods). One half of the tube has the first pod's
   colour and the other half the second pod's colour.
4. **Given** the queue is empty and no bridge is left to choose, **When** Auto starts, **Then** the rest plays at 2×,
   the whole HUD disappears, and the board stays.
5. **Given** a level in progress, **When** the owner switches the normal speed-up on or off, **Then** work runs at the
   chosen speed and the outcome does not change.
6. **Given** the first introduction of a mechanic (hidden pods L6, links L11, bridge L16), **When** the level opens,
   **Then** its explanation popup shows. On the second introduction (L8, L13, L18) a short reminder shows instead. A
   hidden pod that has just revealed glows for a moment.

---

### User Story 3 - Every transferred level is proven to play the same (Priority: P1)

Before a level can be published, the content pipeline proves that the game's rules play it exactly as the round engine
it was built on. The proof covers three things:
- the saved solution wins with the same events;
- every random-test game ends the same way, so the win share is identical;
- the owner's recorded pilot attempts, where they exist, end the same way.

**Why this priority**: The owner will check only samples of thousands of levels. This automatic proof is what keeps
quality from slipping in transfer, so it is the quality gate of every future wave.

**Independent Test**: Run the conformance check on L1–L20. Change one rule parameter on purpose (for example the
release delay) and see the check fail with the level and the first differing event named.

**Acceptance Scenarios**:

1. **Given** a round level with its proof record, **When** conformance runs, **Then** the saved solution wins in the
   game's rules with the same sequence of rule events and the same state at each checkpoint.
2. **Given** the level's random-test record (policy, seeds, number of games), **When** conformance replays those games,
   **Then** each game ends the same way, and the win share equals the recorded one exactly.
3. **Given** the owner's pilot v12 results file, **When** its attempts are replayed, **Then** each attempt reaches the
   same outcome (won, jammed or left unfinished) at the same moment.
4. **Given** any mismatch, **When** conformance reports it, **Then** publishing is refused, and the report names the
   level, the replay and the first differing event.

---

### User Story 4 - A confirmed wave is transferred by a repeatable path (Priority: P2)

After the owner confirms a wave (the sample is played and its mismatches are closed), its levels are transferred into
the game by one repeatable path:
1. entry checks;
2. export to the game's level and picture formats;
3. validation in the game's pipeline, including conformance;
4. a new content version and a playtest app build;
5. the owner checks the same sample on the phone;
6. a transfer log entry.

After the first transfer (L1–L20), this path becomes the skill `bloomlings-round-transfer`.

**Why this priority**: The first transfer can be guided by hand. Every later wave of 50 levels must not be.

**Independent Test**: Run the path on L1–L20 from a clean checkout. It produces the content version and the app build
without anyone editing level files by hand, and it refuses a wave that fails an entry check.

**Acceptance Scenarios**:

1. **Given** a wave whose audit has a mandatory failure, or whose picture is not approved, or whose sample the owner has
   not confirmed, **When** transfer starts, **Then** it stops and names the failed check.
2. **Given** a confirmed wave, **When** transfer runs, **Then** every level and picture is exported, validated (schema,
   rules, conformance, version diff) and published as a new content version.
3. **Given** a level that was already published, **When** its definition changes in a later transfer, **Then** it goes
   out only as a new definition version, and the diff lists it as a deliberate change.
4. **Given** a completed transfer, **When** the owner confirms the sample on the phone, **Then** the transfer log
   records the wave, the content version, the round engine version, the date and the confirmation.

---

### User Story 5 - The Unity client plays round levels (Priority: P3)

The product client plays round levels with the same rules and the same presentation decisions as the playtest app.

**Why this priority**: The playtest app shares the game's rules core and reaches the owner fastest. The product client
follows once the rules and the formats have settled.

**Independent Test**: The client's engine-free checks play L1–L20 from the published content. On a device build, the
owner sees the same behaviour as in the playtest app.

**Acceptance Scenarios**:

1. **Given** the published content with L1–L20, **When** the client plays a level with the saved solution, **Then** it
   wins with the same events as the playtest app.

---

### Edge Cases

- **L18, the exception.** Both bridges win, by the owner's decision. The level transfers as is. Its record carries the
  exception, and validation accepts it only with that mark, so the rule "one bridge decides from the second
  introduction" stays enforced for every other level.
- **Levels that need a mechanic the round engine does not play yet** (latch and key from L51 on). They cannot be
  transferred, and the path refuses them by name.
- **A level reaching its last pod while a bridge is still to choose.** Auto does not start until no bridge is left to
  choose, as in the pilot.
- **A booster used while workers are on the way.** Its effect is defined against the live timeline (FR-012). Work
  already claimed by a walking worker is never taken from it, so a cell is never cleared twice or left half-done.
- **Speed changes in the middle of work** (normal speed-up, Auto). They change only how fast the work is shown, never the
  outcome.
- **A restored attempt** (app closed and reopened during work). It continues from the same moment of the live timeline
  with the same pending work.
- **Pictures bigger than today's game boards** (up to 44×44 inside the frame, 48×48 with it). They play and draw in full
  on a phone screen.
- **A hidden pod inside a link** does not occur in round levels (hidden pods never stand in a link), and validation
  refuses it.
- **After the last transferred level.** The game shows a "new levels soon" screen, and no old level follows. Features
  unlocked at later level numbers (Store animations at L40, milestone at L50, the Daily Challenge) are checked through the
  tester's level jump. The Daily Challenge stays off until it has round levels of its own.

## Requirements *(mandatory)*

### Functional Requirements

**Rules (the round engine becomes the game's rules)**

- **FR-001**: The game's rules MUST play round levels exactly as the reference round engine of a stated version does.
  The version is identified by the fingerprints of its rules, timeline, palette and clock. For the same level and the
  same commands at the same model times, both give the same events, states and outcome.
- **FR-002**: Work MUST run on a live model timeline with the reference parameters:
  - trip out 100 ms per cell (at least 650 ms);
  - return 75 ms per cell (at least 400 ms);
  - action 100 ms;
  - one worker departs every 250 ms;
  - a place frees 300 ms after its pod's last departure;
  - workers share the live frontier, each claiming the nearest reachable unclaimed cell of its exact material;
  - same-material pods keep independent cadences, and the oldest wins a tie.

  The player can launch pods while work is in progress.
- **FR-003**: Time on the timeline MUST be model time in whole microseconds, never device time. A command MUST carry the
  model time at which it was accepted, so the same commands always give the same outcome, at any display speed or frame
  rate.
- **FR-004**: The game MUST know the 24 materials of the round palette (6 per hero, with their names; 16, 4 per hero,
  until the owner's amendment of 2026-11-01 to rules 17 §3, which appends 8 and keeps the first 16 as they were). Each
  material is its own exact target: a pod clears only its own material, and the materials of one hero are not
  interchangeable.
- **FR-005**: A round board MUST be the approved picture (up to 44×44 cells) inside the frame of the round engine:
  - an open ring path, then a wall;
  - one gate in the middle of the bottom;
  - a stream row that closes the ring;
  - bridge sites on the stream.

  A cell's work is reachable when it touches the open path, so clearing proceeds from the outside in.
- **FR-006**: There MUST be 5 places, filled and freed as the reference does. A single pod keeps its place when another
  frees, and only an accepted pair compacts the places.
- **FR-007**: A hidden pod MUST reveal its material and count as soon as it becomes the top pod of its column, before any
  choice and whatever the free places. It stays revealed for the rest of the attempt.
- **FR-008**: A link MUST launch as one action. It needs a free place for each member, and each partner must be on top
  of its column or directly under a pod of the same link. All its places free together after the whole link has
  finished. A refused tap MUST give its reason.
- **FR-009**: The Sprig bridge MUST offer its sites. A site can be built once its approach on the near bank is open; the
  choice is permanent; building takes no pod and no place; only one bridge of a group can be built.
- **FR-010**: Win and loss MUST be decided as in the reference. A level is won when all work is done. It is lost when
  the places jam: no further action can make progress and nothing is pending.
- **FR-011**: The end speed-up (Auto) and the normal speed-up MUST be presentation only. Auto starts when the queue is
  empty and no bridge is left to choose, and then shows the rest at 2×. Neither changes any rule event or outcome.
- **FR-012**: The four boosters and the jam rescue MUST work on round levels with live time. Each is a command with its
  model time, so its effect is deterministic. While work is in progress:
  - **Extra Slot** adds a sixth usable place at once, for the rest of the level.
  - **Return** takes back a waiting pod that has no worker on the way. Its remaining count goes to the top of its
    column, its place frees, and cleared cells stay cleared. A pod with workers on the way can be returned only after
    they arrive.
  - **Shuffle** rearranges only the pods still in the columns. Links stay linked, hidden pods stay hidden, and the result
    stays winnable whenever any arrangement is.
  - **Bloom Burst** clears every remaining cell of one revealed material and settles that material's pods, keeping exact
    accounting. Cells already claimed by a worker on the way stay with that worker.

  The jam card offers the eligible boosters and the limited rescue, as today. Guided booster demos and free first
  charges work as today. No round level may need a booster to be won.

**Round level, picture and proof data**

- **FR-020**: A round level definition MUST carry:
  - level number, class, round engine version;
  - picture reference;
  - board geometry: frame, gate, stream and bridge sites;
  - pods (material, count, hidden, link);
  - columns with their order, places, mechanics list;
  - an exception mark with its reason (for example L18);
  - a definition version.
- **FR-021**: A round picture MUST carry:
  - its code (`P####`), title and version;
  - the cell matrix in the 16 materials, with empty cells, streams and bridge sites;
  - theme tags, source;
  - the owner's approval record.
- **FR-022**: Each round level MUST carry a proof record:
  - the saved solution (commands with their model times);
  - the random-test record (policy, seeds, games, wins, share, interval);
  - the class checks: "opening + colours", forced decisions, pods without work;
  - the audit result;
  - the fingerprints of the engine it was measured on.
- **FR-023**: Validation MUST refuse a round level whose definition, picture or proof is incomplete, or that breaks a
  mandatory rule of the level generation rules (17). It accepts a documented exception only for the rule that exception
  names.

**Conformance (the quality gate of transfer)**

- **FR-030**: For every round level, the pipeline MUST replay the saved solution in the game's rules. The replay must win
  with the same rule events and the same state at each checkpoint as recorded by the reference.
- **FR-031**: For every round level, the pipeline MUST replay the recorded random-test games (same policy, same seeds)
  in the game's rules. Every game must end the same way and the win share must be exactly the recorded one.
- **FR-032**: Where the owner's pilot results exist for a level, their recorded attempts MUST replay to the same
  outcomes at the same moments.
- **FR-033**: Any conformance difference MUST block publishing. The report names the level, the replay and the first
  differing event.
- **FR-034**: Rule changes MUST keep the existing determinism gates (rule tests, golden replays, determinism checks), now
  including round levels.

**Presentation (the owner's decisions of 2026-10-27)**

- **FR-040**: The board MUST use the game's existing frame and the game's existing cube drawing, coloured from the
  round palette.
- **FR-041**: Board cubes MUST NOT show variant icons (leaves, flowers and the like). Hero icons on cubes are a later
  feature. The only exception is the symbols mode of FR-047, which the player turns on.
- **FR-042**: The cubes' clearing ("eating") animation MUST be kept and play at each cell's clear moment on the live
  timeline.
- **FR-043**: Linked pods MUST be joined by a tube attached to their front faces, in the columns and in the places. Half
  of the tube is in one member's colour and half in the other's. The tube is not drawn above or on top of the pods.
- **FR-044**: When Auto starts, the whole HUD MUST disappear and leave the board. The normal speed-up control stays
  available until then.
- **FR-045**: The first introduction of each mechanic with a card (hidden pods, links, bridge, the latch since FR-068, the
  channel since FR-069 and the shutters since FR-070) MUST show
  its explanation popup. The second introduction shows a short reminder. Hidden cells (FR-066) have no popup at all (the
  owner, 2026-11-01: the player sees the «?» and the cells reveal by themselves). A hidden pod that has just revealed glows
  briefly.
- **FR-046**: The class badge (Hard, Super Hard), the finished picture in the win screen and Collection, and the rewards
  by class MUST work for round levels as for today's levels.
- **FR-047**: Every pair of the 24 round colours MUST pass the game's readability test under normal vision: the
  smallest CIEDE2000 is at least 10. Settings MUST offer a symbols mode, off by default, that shows each material's
  symbol on board cubes, pods and places, so colour-blind players can tell materials apart. Levels need not avoid colour
  pairs.
- **FR-048**: The walking Bloomlings of every clearing style MUST show the hero of the material's family (each of the 24
  colours belongs to one of the 4 heroes), tinted in that material's colour. The same holds in the Store's clearing
  previews.
  *Amended by the owner on 2026-11-04 (Settings «Ходоки», `settings.walkers`):* the player MUST be able to choose the walkers in
  Settings: **Classic** (default, and for saves without the key; the look above, byte-identical), **Creatures** (a drawn
  creature of the material's family with arms and legs, `UiRaster.Creature`: leaf, flower, drop or acorn shaded off its
  cube's colour in a dark outline, an 8-frame run, holding a cube before its belly with its hands over it) or **Heroes** (the
  owner's real modular V2 hero of the material's family as a small walker, his hands drawn again over the cube; **the
  owner's experiment of 2026-11-04**: hero frames in a level as walkers go beyond constitution principle VII, which allows
  them only as a mechanic's figure, until the owner decides). *Changed by the owner later on 2026-11-04 (the Trails):*
  Creatures and Heroes walk at a fixed size on screen (5% of the board's width, `WalkerLook.TrailsBoardShare`; a hero 1.15×
  the walker) on a contact shadow, turned a little sideways while walking (0.8 of their width, leaning 7° into the way); they
  keep to two lanes, right of the way's centre line going out and left coming home (0.45 walker boxes), each lane only where
  that side is open ground (`TrailLanes`: never over cubes; the centre line where it is not); a pod's line spreads to 1.15
  walker boxes where the figure is longer than its gap (a later, quicker set-off, the arrival unchanged). In Munchers a walker
  never stands: the cube hops into its hands at its cube (0.25 s), it carries it a little (0.45 s or 1.75 cells of its way
  home), then eats it in three bites spread up to 90% of its way home, crumbs where each bite is taken; a cube bigger than
  its hands can hold rides on its head, never smaller than on the board. The heroes wear a soft glow in their cube's colour
  (two silhouettes behind them, `WalkerLook.GlowScales`, at least 2.5 px). The other bought styles keep their acts with these
  figures, size and lanes. Presentation only: the rules' clear, return and conformance stay; the act after the clear is
  0.25 s (`WalkerLook.TrailsLinger`).
- **FR-049** (the owner's board, 2026-10-28): The board MUST stand in one fixed, roughly square box, the same position and
  size on every level, its frame always the same thickness. Inside the frame the board MUST be one solid neutral surface
  (no grass, no grid of cells under the picture), and an eaten cube MUST leave only that surface (no imprint). The
  picture stays the game's cubes; the cube size MUST adapt so the whole picture fits the box as large as possible,
  centred and in proportion (a big matrix smaller cubes, a small one bigger cubes), and everything on the board scales
  with it.
- **FR-064** (the owner's level screen, 2026-10-28: "a bigger board and picture; pods without their extra box, volumetric
  like cubes, 3 rows one after another, no indicator of how many more; as in Colony Flow"; constitution 1.2.0 VII):
  - The level screen MUST take the owner's sizes from the board constructor (`RoundScreenLook`, one named constant each:
    the board box 0.953 W × 0.44 H from 0.1 H, frame 0.042, places row 0.085 H after a 0.03 H gap, boosters 0.075 H,
    pod cubes 0.15 W with 0.02 W gaps; H is the 1080 × 2340 reference phone's), the box the same on every level. Where a
    phone is shorter, everything MUST shrink by one factor until it fits.
  - The board's cubes, the pods and the cubes in the places MUST be drawn as seen from one fixed tilted viewpoint, as flat
    pictures: tops wider than tall, and a side face (at most 0.10 of a cube at the outermost column, none in the
    middle) only where the neighbour on the side facing away from the middle is empty. Hit areas follow the cells; the
    rules keep their flat grid.
  - As amended by the owner on 2026-11-03 (look "E" of the review sheet `product/previews/board-cubes-2026-11-03/`, "in
    the middle of a big picture the cubes look flat, squares in a drawn grid", with his corrections "E2": "the front does
    not stick out from under the cubes that far", and "E3"), every board cube MUST read as a soft cube in a packed area too, as in
    the reference game: rows 0.88 of a column, a top 0.755 tall with soft corners (0.16) and a small light dot at its
    lower right, under every cube a clearly darker band of its front (0.30 deep, 0.125 of a column showing under a filled
    cell in front; the whole front where that cell is empty) and neighbours touching (body 1.0 of a column, the owner's
    "E3" after a phone check: "a strong grey strip between the cubes"), parted only by each cube's own darker edge in its
    own hue, never a grey or surface-coloured strip, with a subtle shadow only under the picture's foot on the surface. The pods and the places keep their look of 2026-10-28.
  - Each column MUST show exactly its first three pods, one after another and never overlapping, and no "+N" for the
    rest. A pod is a cube with no frame or box round it: the top row's in a white outline, its number big in the middle
    of its top (no corner count anywhere: pods, places, flights). Waiting pods MUST keep their colour clearly
    recognisable (barely lighter, a flatter base, the number half clear); a hidden pod is the neutral "?" cube.
- **FR-065** (the owner, 2026-10-31, after playing the bridge levels on the phone: "the links' tube twice as thick"; "the
  water round the pictures must stand out clearly, it blends in, especially by blue cubes"; "the bridges bigger, they
  are hard to see"; "on Normals show them over the cubes, on Hards 50-50 in the waves, on Super Hards the player looks
  for them, show one over the cubes that loses"; 17 §8 as amended on 2026-10-31):
  - The link tube MUST be twice as thick as before (0.3 of a pod cube, 0.4 of a plate's height).
  - The stream MUST read as a channel set apart from every cube colour: deep water in one tone with light ripples, and a
    cream stone bank with a dark line along every side where it meets a cube or the surface.
  - A bridge MUST be a wooden footbridge with rails, about 2.7 cells long and 1.5 cells wide, drawn over the cubes.
  - Each bridge site MUST carry, in the level data, whether it is shown from the start (`shown`, absent = shown). A
    shown site MUST be drawn faintly over the cubes until its approach opens; a site not shown MUST stay unseen until
    then. A buildable site MUST be drawn whole with its glow outline gently pulsing (swelling and brightening, the owner's
    "the outline could pulse a little" of the same day), and take the tap; a built bridge is drawn
    whole. The rules never read `shown`.

- **FR-066** (hidden cells, rules 17 §8 «Скрытые клетки», the owner's amendment of 2026-11-01; the reference contract is
  `tools/rounds/README.md` "Hidden cells"):
  - Data: a round level MAY list hidden cells (`board.hiddenCells` of `round-level.v1`, `mechanics` then includes
    `"cells"`): cell indices `y*w+x` of `rowsBottomFirst`, sorted ascending, unique, at least one, each a material cell.
    Validation MUST refuse a bad list, a hidden cell that is not a material cell, and hidden cells on a level with hidden
    pods (the two unknowns are interchangeable and never on one level). Absent means none. Hidden cells are introduced on
    L51 and L53 (17 §9).
  - Reveal: a hidden cell's material is fixed from the start; it MUST reveal exactly when it becomes a target (an
    orthogonal neighbour open and reached from an entry, or itself an entry cell). The start reveal is silent (part of the
    start state); afterwards each reveal is a `cellReveal` event right after the clear, bridge or Bloom Burst that opened
    it, at that change's model time, several in increasing cell order (conformance line `<µs> cellReveal cell=<c>`).
    Nothing else changes: targeting, dispatch, release, status, outcomes and the signature are those of the same level
    without hidden cells, and a level without hidden cells plays byte-identically. Bloom Burst may take a hidden cell of
    its material unseen; it cannot be aimed at a cube still showing "?".
  - Look: an unrevealed hidden cell MUST be drawn as the board's oblique cube in the hidden pod's neutral grey with its
    white "?" marks (`tile.round.hidden`), its side face in the same grey; when its `cellReveal` plays it pops into its
    material's cube (about 0.36 s). The walkers never go to it before. Its meetings on L51 and L53 show no card (the
    owner's amendment of 2026-11-01; FR-045).

- **FR-067** (a level's recolouring of its picture, rules 17 §3 and §6 «Акценты для давления на места», the owner's
  amendment of 2026-11-01; the reference contract is `tools/rounds/README.md` "Recolouring and accents"):
  - Data: a round level MAY carry `board.recolor` of `round-level.v1`: `[x, y, "C"]` entries in picture coordinates (x
    from the left, y from the top), each a material cell of its approved picture that this level paints another
    material; at least one entry, one entry per cell, never an empty cell or a strip, never its own material, never under
    `board.overlay`'s walls. It is applied before the overlay. Absent means none, and a level without it is written and
    read exactly as before.
  - Validation: the picture ↔ board check MUST rebuild the board from the picture with the recolouring, then the overlay,
    and refuse a bad entry (outside the picture, not a material cell, its own material, twice, under the overlay) as well
    as rows that do not show it. The rules read only the rows.
  - Look: the finished picture on the win MUST show the level's recolouring.
  - Use: the round tools paint small accents (2–4 cubes in 1–3 clusters, 1–2 materials absent from the picture, their
    pods early or mid queue) and recolour cubes into richer materials where the picture gains; the approved picture
    stays as approved.

- **FR-068** (latch + acorn key, rules 17 §8 «Засов и жёлудь-ключ», §9 as amended twice on 2026-11-01; the reference
  contract is `tools/rounds/README.md` "Latch and key"):
  - Data: a round level MAY list latches (the top-level `latches` of `round-level.v1`, `mechanics` then includes
    `"latch"`): `[{column, depth, key}]`. `depth` is how many pods of that column stand before the latch (0: it starts on
    top), `key` a cell `y*w+x` of `rowsBottomFirst` of an empty `.` cell, never a wall, strip, bridge site, material cell
    or entry; index `i` is latch `i` of the events. Validation MUST refuse a missing column, a depth outside
    `0 ≤ depth < column length` (a pod stands behind every latch), two latches at one depth of a column, a bad or shared
    key, a key that can never be reached (`LatchKeysBlocked`: the cheap flood with every material wholly behind closed
    latches blocked) and more than `max(1, columns − 2)` columns blocked at the start. Absent means none. The latch is
    introduced on L61 and L63 (17 §9).
  - Rules: a latch is no pod (no quota, no place). A pod of a column at or behind its first closed latch is no top pod:
    it cannot be tapped (`not-exposed`), is not revealed by the depth rule, and a link partner behind it is not exposed.
    A closed latch opens at the first moment it is its column's top and its key cell is reached by an open path from an
    entry: silently at create, after a tap's commits, after a bridge's (and Bloom Burst's) cell reveals, and on the
    timeline right after a clear's cell reveals at the clear's time; each is a `latchOpen` event (conformance line
    `<µs> latchOpen latch=<i>`), several in increasing latch order, followed at the same time by the depth reveals of the
    pods it exposed. The signature leaves the latches out (a function of heads and board) and a restored snapshot
    recomputes them; a level without latches plays byte-identically. The solver and the random test enumerate only the
    legal actions, which never include a held pod.
  - Boosters: a latch is no pod, so no booster takes, moves or removes it; it opens only by its rule. Return puts a pod
    back on top of its column, before a closed latch there; an open latch stays open (a restored snapshot keeps it).
    Shuffle moves only the pods before each column's first closed latch. Bloom Burst may open the way to a key; the latch
    then opens right after the burst's cell reveals.
  - Look (the owner's look of 2026-11-02, replacing that of 2026-11-01: our hero holds the lock, "Twig hero 100%", and as the
    latch is a general mechanic it is the hero whose colour is in the key and the padlock; the key upright on its tip, not
    touching the board, turning; the key flies to the lock): while a latch is closed the pod it holds (the pod right behind
    it) is not drawn at all; in its place stands the hero of the held pod's colour (the family of its material: Sprig, Bloom,
    Drop or Twig; Twig for a hidden pod and for a latch with no pod of its own), the owner's modular V2 frames in the player's
    equipped outfit for that family (stock without one; pre-rendered flat 2D frames, constitution VII as amended to v1.3.0),
    his feet on a soft contact shadow at the place's bottom, his head a little over it, holding in front of his belly a
    padlock (`pod.round.padlock`) whose body is the colour of that pod (brass for a hidden pod, and for a latch with no pod
    of its own), his hands over its sides (fitted to each family's frames); in rows 2 and 3 he is lightened as the waiting pods. Its key (`board.round.key`) is an
    ordinary big key in the same colour standing upright on its tip over its cell (bow up, about 2.4 cells tall and at
    least 0.13 of the board surface's width, kept on the surface, over the cubes round it), hovering a little over the cell
    with a soft oval shadow on it, bobbing and turning round its own upright axis (a flat 2D squash, never thinner than
    12 %, its back half darker, about 1.8 s a turn). Only when two latches of a level hold pods of one colour do their
    padlock and key carry 1–3 white pips to tell them apart. When `latchOpen` plays (the latch is then its column's top and
    its key reached) the key lifts off, turns faster and flies on a gentle arc into the padlock's keyhole, shrinking to its
    scale; it turns a quarter there with a click ring, the shackle springs open, the padlock drops and fades and the hero
    steps aside and fades, over 0.9 s of display time; the pod is drawn behind him and is the column's top pod, taking taps from
    the event on (contracts/presentation.md). Presentation only: rules, events and timing are unchanged.
  - Popup: its first meeting (L61) shows an explanation card ("The lock": the hero of the pod's colour holding the padlock in the pod's place and the
    upright key in the pod's colour on a patch of the board, looping the key's flight and the opening; "A padlocked pod waits in its column", "It opens when the Bloomlings reach
    the key of its color on the board"); its second (L63) a short reminder.
- **FR-069** (the Drop water channel, rules 17 §8 «Drop Водный канал», §9: introduced on L101 and L103, the owner,
  2026-11-05; the reference contract is `tools/rounds/README.md` "Channel"):
  - Data: a round level MAY carry `board.channel` = `{spring, exits: [{cell, route, rideUs}, {…}]}` (`mechanics` then
    includes `"canal"`; an introduction's mechanic is `"canal"`). `spring` and each exit's `cell` are cells `y*w+x` of
    `rowsBottomFirst` of empty `.` cells, never an entry, a latch key or each other (an exit may be an enclosed pocket
    inside the picture or a cell at its edge). Each exit's `route` lists the channel's cells from the spring to that exit
    (both included, each an orthogonal neighbour of the one before, no repeat; it may pass over any cell: drawn over the
    cubes, the cubes under it stay ordinary work). `rideUs` is a walker's ride along that channel in µs, whole
    milliseconds from 1 ms to 10 s (the generator: 60 ms a step, at least 400 ms). Validation MUST refuse a bad spring or
    exit, not exactly two exits, a broken route, a bad ride, and a spring that can never be reached (`ObjectsBlocked`, the
    latch check's cheap flood extended to every board object). Absent means none.
  - Rules: the channel is ready at the first moment the spring is reached by an open path from an active entry (the same
    search the targets use); event `channelReady` (`<µs> channelReady`) at the time of the clear, bridge or burst that
    reached it, after that change's cell reveals and latches (silent when the spring is reached at the start). Once ready
    and while no exit is chosen, the player may choose either exit (command `channel exit=<i>`, fixture
    `{"type": "channel", "exit": i, "atUs": …}`): free, no place, no pause, final; refused before as `channel-not-ready`.
    The status stays Playing while the choice is open (a jam is no loss then) and the auto-tail waits for it. The choice
    is the command's own event `channelOpen` (`<µs> channelOpen exit=<i>`, the commit's time), followed by the cell reveals
    and latches the new entry causes. From then on the chosen exit is an active entry: the breadth-first reach starts from
    the level's entries in their order and then from it, so every walker departs from the nearest active entry by path
    (ties: the earlier source in that order) and the targets, reveals, latches and bridges see it. A walker whose route
    starts at the chosen exit rides the channel: `rideUs` is added before it walks (arrive = start + ride + walk) and after
    it walks back (done = clear + walk back + ride); its depart line shows those times. The unchosen exit never becomes an
    entry (its pocket may join the open area by ordinary clearing and is then a passage). The extended signature lists the
    chosen exit after the bridge (`…|bridge|channel|status`) on a channel level only; a snapshot keeps it and a restored
    one recomputes the rest; a level without a channel plays byte-identically. The solver and the random test enumerate
    each exit as an action once the channel is ready (after the taps and bridges).
  - Class (rules 17 §5, the owner, 2026-11-05): a Super Hard whose channel decides (exactly one exit wins; the wave plan's
    `channelKind: "single"`, the hand Super Hards L110, L130, L150) is measured with its winning exit only, as a bridge
    level with its best bridge: the proof's optional `randomTest.exit` (and the fixture's `random.exit`, equal to it) names
    that exit, and the random games, here and in the reference, leave every other channel action out of the bot's moves
    (`RoundRandomPolicy.KeepExit`, `Keep(bridge, exit)`); `rounds validate` refuses an exit the level's channel does not
    have and a fixture whose exit differs from the proof's. On Normal and Hard the exit stays one of the uniform actions.
  - Boosters: no booster chooses an exit. Bloom Burst may open the way to the spring; the channel is then ready right after
    the burst's cell reveals and latches. Return and Shuffle are unaffected.
  - Look (the owner's choice of 2026-11-05 from the mock `product/previews/channel-shutters-2026-11-05/`: channel A fully;
    drawn in both clients from the kit's `ChannelLook`, `UiRaster.ChannelSpring` / `ChannelStream` and friends, asset slot
    `board.channel`; stills `product/previews/channel-shutters-game-2026-11-05/`): the spring is a round well of the frame's
    cream stone at its cell, each exit a smaller well at its own; before the choice both routes are dotted, the spring's
    water is still and dark and the exits faint in a dashed white ring; at `channelReady` the spring wakes (rings, the Drop's
    white drop over it) and both exits breathe in the buildable bridge's glow ring and take a tap (a target of at least
    `ChannelLook.TouchCells` 3.2 cells and the touch token; the game keeps running) that commits `channel exit=<i>`; at
    `channelOpen` the water runs from the spring along the chosen route in `RunSeconds` 0.9 s as an open stream between cream
    stone banks (1.2 cells wide, raised on its front face, foam running on it), drawn over the cubes, the exit splashes open
    and the other exit and its dots fade in 0.6 s. A walker whose route starts at the chosen exit is drawn as a bubble of its
    material's colour running along the stream from the spring to the exit during its ride out (`RoundWalk.RideUs`, the
    animator's walk starting after it) and back after it is home (`ChannelLook.Rider`), then steps out of the exit as out of
    an arch. Presentation only: the rules, events and timing above stay.
  - Popup: its first meeting (L101) shows an explanation card ("The water channel": "A spring waits on the board", "Clear
    the way to the spring, then tap a gate: the Drops will ride the water from there", "Only one gate opens. Choose it
    well"; `RoundIntroPicture.Channel`, a small board looping the call, the choice, the running water and a bubble); its
    second (L103) a short reminder.
- **FR-070** (Twig's wooden shutters, rules 17 §8 «Twig Деревянные створки», §9: introduced on L121 and L123, the owner,
  2026-11-05; the reference contract is `tools/rounds/README.md` "Shutters"):
  - Data: a round level MAY carry `board.shutters` = `[{cells, code, count}, …]` (`mechanics` then includes
    `"shutters"`): `cells` the covered cells, sorted indices `y*w+x` forming a full rectangle of material cells (any
    materials; never an entry or a hidden cell, never under another shutter); `code` the exact material it counts; `count`
    ≥ 1 units of it. Index `i` is shutter `i` of the events. Validation MUST refuse a bad rectangle, code or count, and a
    count that can never be reached without the cells under the shutters still closed (`ObjectsBlocked`: no shutter waits
    for its own cells or for another in a cycle). Doses (17 §7, §8: area share, counter share, multiples of 5) are the
    generator's, not the validator's. Absent means none.
  - Rules: while closed, a shutter's cells are no targets and no passage (their work waits; a pod of their material keeps
    its place). It counts the work of its material actually done anywhere since the start: a courier's clear or a Bloom
    Burst cell (work done = the material's start cells − its cells now, so the state follows from the board and the
    signature is unchanged). At the first moment that reaches `count` it opens: event `shutterOpen`
    (`<µs> shutterOpen shutter=<i>`) at the time of that clear, right after its clear line and before its cell reveals,
    several in increasing order; its cells become ordinary targets at once (a waiting pod departs at that moment). The count
    still shown is `count − done`, 0 once open (`RoundState.ShutterLeft`, the reference's `shutterLeft`/`view`). A restored
    snapshot recomputes the shutters; a level without shutters plays byte-identically.
  - Boosters: no booster opens a shutter. Bloom Burst leaves the cells under a closed shutter (the material's pods keep
    that work), but the cells it clears count: a shutter may open right after the burst, before its cell reveals. Return
    and Shuffle are unaffected.
  - Look (the owner's choice of 2026-11-05: shutters B fully; drawn in both clients from the kit's `ShutterLook` and
    `UiRaster.ShutterBase` / `ShutterBatten` / `ShutterHanger`, asset slot `board.shutter`): a crate lid over the rectangle,
    raised `LidCells` 0.14 cell, level honey slats (the signs' plank) about one a row between two dark upright side battens
    with nails, its soft shadow and dark front edge, drawn right after the cubes of its bottom row so the nearer rows stand in
    front of its edge; on it a tag hangs from a nail on two strings: the pods' own cube in the counted material with the
    count still to go (`RoundAnimator.ShutterLeft`, the rules' `ShutterLeft` at the display clock; at least 58 px of a
    1080-wide screen), swaying a little and popping at each counted cube while a spark of the material flies from that cube
    to it. At `shutterOpen` the slats pop off one after another from the top, lifting, sliding aside and turning, with dust
    and leaves, over `RoundAnimator.ShutterOpenUs` 0.95 s. Presentation only.
  - Popup: its first meeting (L121) shows an explanation card ("The crate": "A crate hides some cubes. Its tag shows a
    color and a count", "Clear the shown cubes to break the crate open", "Every cube of that color counts, anywhere on the
    board"; `RoundIntroPicture.Shutters`, a small board looping the count and the opening); its second (L123) a short
    reminder.

**Transfer path**

- **FR-050**: The transfer of a wave MUST refuse to start unless the wave passes three entry checks:
  - its audit has no mandatory failure;
  - all its pictures are approved;
  - the owner has confirmed its sample.
- **FR-051**: Transfer MUST export the wave's levels, pictures and proofs to the game's formats without hand editing.
  It then validates them (schema, rules, conformance, a diff against the previous content version) and publishes a new
  content version.
- **FR-052**: A published round level MUST change only by a new definition version, which the diff lists as deliberate.
- **FR-053**: Transfer MUST build a playtest app with the new content, and record each transfer in a transfer log: the
  wave, the content version, the engine fingerprints, the date and the owner's confirmation.
- **FR-054**: After the first transfer, the path MUST be written down as the skill `bloomlings-round-transfer`.

**Content placement**

- **FR-060**: The game's levels MUST be round levels only. The first transfer gives L1–L20, and each wave adds its
  levels. After the last transferred level the game shows a "new levels soon" screen. The old levels (curated L1–10, the
  legacy catalog L11–2500) and the old rules leave the game builds: one rule set plays the game. They stay in the
  repository as history until the owner retires them.
- **FR-063**: The Daily Challenge MUST stay switched off until it has round levels of its own (outside this spec).
  The level tester MUST be able to jump the save to any level number, so meta tied to later levels (L40 Store
  animations, L50 milestone) can still be checked.
- **FR-061**: Onboarding tied to level numbers MUST make sense on the round levels:
  - GuideTour's Level 1 entry and forced first tap;
  - Level 2's first out-of-reach pod;
  - booster demos at their unlock levels.

  Where it does not, the tour step is adapted.
- **FR-062**: The playtest level tester MUST be able to open any round level (◀ ▶) with its proof summary.

### Key Entities

- **Round level**: one position of the game (number, class) built from one approved round picture. It holds the board,
  the pods and columns, the mechanics, an optional exception and a definition version.
- **Round picture**: an approved cell matrix in the 16 materials, with its code, title, tags, source, version and
  approval.
- **Proof record**: what shows that a round level is valid and how hard it is. It holds:
  - the saved solution;
  - the random-test record;
  - class checks and metrics;
  - the audit result;
  - the engine fingerprints.
- **Round engine version**: the fingerprints of the rules, timeline, palette and clock that a level was measured on and
  that the game must match.
- **Transfer log entry**: one transferred wave, with its content version, engine version, date and the owner's
  confirmation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 20 levels L1–L20 pass conformance. Their saved solutions win with identical events, and 100 % of
  their random-test games (100 or 400 per level) end as recorded, with identical win shares.
- **SC-002**: All 23 recorded attempts of the owner's pilot v12 results replay to the same outcome at the same moment.
- **SC-003**: The owner plays the transferred L1–L20 on the phone and reports no difference in rules or difficulty from
  pilot v12. Presentation remarks are allowed and are handled separately.
- **SC-004**: Each owner decision of 2026-10-27 is met on 100 % of the round levels:
  - no variant icons on board cubes while the symbols mode is off, and every material's symbol on cubes, pods and
    places when it is on;
  - front link tubes with split colours on every link;
  - the HUD hidden whenever Auto runs;
  - introduction popups and reminders on all six introduction levels.
- **SC-005**: A deliberate change to any rule parameter makes conformance fail on at least one of L1–L20, with the level
  and the first differing event named.
- **SC-006**: Transferring a confirmed wave runs from start to the app build with no hand edits of level or picture
  files. A wave that fails an entry check is refused in every case.
- **SC-007**: For the same level and commands, the outcome and the final state are identical at normal speed, at the
  normal speed-up and under Auto, and before and after an attempt is restored.
- **SC-008**: Each of the four boosters and the jam rescue can be used on round levels during live work. A booster use
  replays to the same outcome and state from its recorded command and model time, and per-material accounting
  reconciles after every use.
- **SC-009**: The largest round board (48×48 with the frame), with every place working, plays as smoothly on the owner's
  phone as today's biggest boards. The playtest's frame-time check stays within its present budgets for that level.

## Assumptions

- **Decision A** (the transfer proposal): the round engine's rules become the game's rules. The owner asked to start
  this spec on that basis. Today's rules leave the game builds with the old levels (FR-060).
- **Recorded defaults** (the owner may change them):
  - the first transfer target is the full Android playtest app, then the Unity client;
  - the game holds round levels only, from L1, wave by wave (Clarifications);
  - the 16 materials are drawn as palette-coloured cubes now.
- **Scope of mechanics**: hidden pods, links and the Sprig bridge only. Latch and key, canal, shutters, repair, bud,
  seed, well, hidden cells and stacks need round-engine work first and are out of scope.
- **The round engine is the reference.** Its behaviour, as measured for L1–L20, defines correct. Where its code and the
  level generation rules disagree, the disagreement is reported to the owner and not silently "fixed" in the game.
- **Display speeds**: the normal speed-up keeps its present ×2. Auto changes from the pilot's ×2.5 to ×2 by the owner's
  decision, which affects presentation only.
- **The unique-art launch plan** (`content/launch-art/plan.json`, 2500 frozen slots) and the legacy catalog are
  superseded position by position as waves replace them. Whether the plan is retired as a whole is the owner's call,
  outside this spec.
- **Decided with the owner on 2026-10-27** (see Clarifications):
  - **Constitution II** is amended so that a variant on board cubes is identified by hue, readability-checked under
    normal vision, with the symbols mode for colour-blind players. Hero icons on cubes come later.
  - **Constitution III** and doc 15 say explicitly that a command carries model time in whole microseconds.
  - Both changes ship in constitution 1.1.0, through its amendment process.
  - **Readability**: under normal vision the palette passes. Colour-blind players get the symbols mode (FR-047). The
    palette and the levels stay.
  - **Locked docs**: doc 01 (rules), 02 (queue and buffer), 03 (tiles and variants) and 15 (architecture) describe
    today's rules. They change through the owner, with this spec as the reason.
