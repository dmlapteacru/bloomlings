---
name: "bloomlings-round-levels"
description: "Turn the owner-approved Bloomlings round pictures into solved levels: pod queues, mechanics, a saved winning solution, the random-test difficulty class, placement into the difficulty calendar, level cards, and a list of positions still needing pictures. Use after pictures are approved, or when the owner asks to generate/build/place levels or rounds, fill levels N–M, or re-check levels after an engine change."
argument-hint: "Which levels to fill or which pictures to use (e.g. 'levels 31–60' or 'all approved pictures')"
user-invocable: true
disable-model-invocation: false
---

# Round levels (step 2 of 2)

This is the second half of process §13 of
[`product/17_LEVEL_GENERATION_RULES.md`](../../../product/17_LEVEL_GENERATION_RULES.md): §13.2–§13.10, plus the
acceptance and card of §14.

Read the owner's notes log [`content/rounds/owner-notes.md`](../../../content/rounds/owner-notes.md) first: its principles (slot pressure makes a round interesting, difficulty is not just the first tap, mechanics must matter, introductions may be a breather, motif variety) steer what to draw, build and prefer, and new owner notes are added there.

Read 17 §1, §3, §5–§8, §10, §11 and §14 before building, and the section of each mechanic you use in the full catalog
[`product/design/Bloomlings-gameplay-mechanics.md`](../../../product/design/Bloomlings-gameplay-mechanics.md). The pictures come from the skill
`bloomlings-round-pictures` and must be `approved`. Where this skill and 17 differ, 17 wins.

**Where the pictures are.** `content/rounds/approved/` (built by `pictures.mjs publish`):
- `locked/L0001`–`L0003`: the pictures of levels 1–3, used there and nowhere else.
- `pool/P####`: every other approved picture. They are interchangeable between rounds. A picture's number, its
  `forLevel` or the order of the pool never assigns it to a level.
- Each code is **free** or **in a round** (`state`, `usedBy`; `pictures.mjs status --free`). Build and place use only free
  codes. `place` marks a code as used by writing its level, then rebuilds the folder. A code becomes free again only when
  its level file is removed or rebuilt with another picture.

## In a wave (the owner's decisions of 2026-10-27, 17 §13)

Build and placement run by script: `node tools/rounds/rounds.mjs run <from> <to> --jobs 4` (skill
`bloomlings-round-wave` leads the whole wave). It runs, in order:
- a dry placement on a copy;
- `levels.mjs build` only for the free pictures and the classes and recipes the open positions need (one process per
  picture; resumable, a searched combination is skipped);
- `levels.mjs checks`;
- `levels.mjs place`;
- `audit.mjs`;
- the summary and the owner's sample pilot.

The commands below remain the building blocks. Use them directly only to debug or to do a targeted rebuild
(`build --fresh --seed N`).

**What the generator builds** (17 §13 «Кто строит уровни», the owner, 2026-10-30): Normal levels and the Hard levels that
pass every check and your look. Every Super Hard, every bridge level and a Hard the generator does not manage are built
by hand (skill `bloomlings-round-wave`, step «Hand-built positions»): do not spend generator runs on them.

**A bridge level's class** (17 §5, the owner, 2026-10-30) is the random test with its best bridge only: the deciding one,
or the one the random games win more often when both win. `evaluate` records it as `random` (with `bridge`) and the
test over both bridges as `randomAll`; the card shows both. The export refuses such a level until the proof format
carries the restriction.

Layer B of the quality is `content/rounds/quality.json`. Its metric "pods without work" is stored per candidate
(`checks.pressure`, `measure.mjs pressure`). Placement prefers candidates at or above the class's bound; the audit notes
those below it, and fails them once the bound's mode is `hard`.

## Principle

Pictures are not tied to levels (except the locked levels 1–3).
- A picture is built into candidates of every class the rules allow, and the **measured** class decides where a
  candidate may go. A picture that turns out Normal goes to a Normal position, Hard to Hard. The calendar does not
  turn a picture into a class.
- A picture that fits no open position stays in the **bank** for the next calendar iteration.
- Positions left empty are written to `content/rounds/needs.json`. For them the picture skill draws new pictures.
- Already made pictures are not forced into a position.

## Calendar (17 §5, §9)

- Every 15th level is Super Hard, other multiples of 5 are Hard, the rest Normal.
- Mechanic introductions are two Normal levels each, alone, with a random-test target of 90–100 %: hidden pods 6/8,
  links 11/13, Sprig bridge 16/18, latch + acorn key 51/53, canal 101/103, shutters 151/153, repair 201/203, bud
  251/253, seed 301/303, well 351/353, hidden cells 401/403, stacks 451/453.
- A type is not used before its first introduction, nor between its two introductions. From the third appearance it
  may combine with known types.
- L1–L5 have no extras. L1 must have no losing choice; L2–L3 aim at 90–100 %.
- `node tools/rounds/levels.mjs calendar <from> <to>` prints class, introduction and allowed types per level, and
  marks types the engine does not play yet.

## Engine support (17 §14: every mechanic used must really be supported)

`tools/rounds/engine.mjs` is the pilot v9 engine (v8 base, slot positions of §1) with the §8 link rule.
- **It plays:** hidden pods, linked pairs, Sprig's bridge.
- **It does not play yet:** latch + acorn key, canal, shutters, repair, bud, seed, well, hidden cells, stacks
  (`SUPPORTED` in `tools/rounds/lib.mjs`).

The builder only uses supported types. For a position that needs an unsupported type, stop and tell the owner:
- which levels need it (the `calendar` output marks them);
- that the engine and the playable pilot need that mechanic first, per its §8 contract.

Do not fake a mechanic with another one: a bridge is not a canal, shutters are not a bud. Levels up to 50 need only
the supported three.

## Steps

1. **What to fill.** The owner's range of levels; otherwise the next empty positions after the last placed level
   (`content/rounds/levels/`). Run `calendar` for the range and check engine support.
2. **Build candidates.**
   - Command: `node tools/rounds/levels.mjs build --approved` (or a list of `P####` ids). Options:
     `--classes Normal,Hard,"Super Hard"`, `--recipes none,links,hidden,links+hidden[,bridge,bridge+links]`,
     `--candidates 64` (the start budget per picture and class, 17 §13.5), `--seed`, `--overlay up,left,right,two`
     (build on the generator's wall over the picture; `none` = its own board), `--pairs Normal:links,Hard:bridge+links@up`
     (only these class:recipe pairs, `@side` for a wall), `--no-intro`.
   - **Recipes put the lead first** (17 §7): `none`, `links`, `hidden`, `bridge`, `links+hidden`, `hidden+links`,
     `bridge+links`, `bridge+hidden`, `hidden+bridge`, `links+bridge`, `links+bridge+hidden`, `bridge+links+hidden`.
     A board with a strip takes bridge recipes only (its far part needs a bridge), a board without one no bridge recipe.
   - **Bridges on any strip** (17 §8 "Где стоит мост", 2026-10-28). The crossing is up (a stream across), left or right
     (a wall from top to bottom), or `two` (two sites crossing different ways, an L wall, or on different strips). A
     wave plan's `strip: overlay` position is built on a free pool picture with the generator's straight wall
     (`wallOverlay`: 25–45 % of the colour cells behind it, two sites with colour approaches, the entry on the open side);
     the level stores it as `definition.overlay` (`{walls, sites}`, picture coordinates), N and W count the board with
     the wall, the audit rebuilds the board from the picture plus the overlay, and the exporter writes `board.overlay`.
     A `strip: gap` position (the owner, 2026-10-29: 17 §8 «Одна цельная картинка») takes only a level on an approved
     **split picture** (`bridge-scene.mjs make --mode split`: one whole pool picture with an inserted neutral strip and
     two sites; its record's `scene.mode: 'split'`): `boardOf` gives such a board `strip: 'gap'`, the candidate's
     definition carries `strip: 'gap'` for renderers, and `fitsPlan` refuses any other bridge level for the position. The
     split search writes the found candidate level into its evidence (`<out>.json`); `bridge-scene.mjs preview <module>
     --out <dir>` turns it into a preview level for the pilot (`build.mjs --preview <dir>`) once the picture is rendered.
     A `strip: new` position needs a picture with its own strip of the planned side: since 2026-10-28 mostly a
     **bridge scene** (`tools/rounds/bridge-scene.mjs`, the owner: "взять картинку поменьше и вокруг неё строить мосты
     и стены"). `make --side … --kind single|better --cls … --recipe … --level n --free --out <module>` composes a
     scene from a smaller free pool picture (the far side behind a wall framed by `#`, a strip behind each site with a
     `#` divider, a bed before each site and a meadow on the near bank), runs the builder's candidates through
     `levels.mjs evaluate` (the checks of `build`, the planned kind tested first) and writes the passing scenes as a
     draft picture module with the evidence (`<out>.json`); the owner approves them on the sheet. On an approved scene
     the builder leans the queue to the winning site (the record's `scene.win`) and keeps every pod of the other strip's
     colour (the gate) in the last rows (`sceneOpts`, `candidate` opts `gate`, `intended`, `delay`).
   - Run it in the background for big batches; it logs per picture/class/recipe how many candidates were found and
     the reasons for the rest.
   - For each candidate the tool:
     - splits the work of each material into pods by §3. Numbers are 1–60; a colour of 20+ gets at least 2 pods; the
       largest pod is min(60, ceil(volume/2)); the split is near even; the budget of pods follows W. L1–L3 use the
       fixed splits of §4.
     - orders the pods by the real access depth of their cells (a bridge site counts as a path) with class-dependent
       variation. Hard/Super Hard candidates blend that order towards its reverse by a share drawn per candidate (§6.1:
       outer colours deeper, inner ones earlier, so their pods wait in places and a careless tap jams them). A
       properly reversed queue makes even an object on an empty background hard (measured 2026-10-26: the owl 40 %,
       the frog 0 %, both solvable), so the measured class decides. At the start at least one top pod has reachable
       work (§6). It deals the pods into 3 or 4 columns by N, with start heights within 1.
     - adds the recipe's mechanics at the doses of §7 (one type: full range; two: the additional at its minimum
       admissible dose, links an even count; three: the lead in the lower half of its range, the others at minimum;
       the lead's dose at the place of the §7 cycle; small queue under 16 pods: one type). Introductions use teaching doses: 2–3 hidden pods (the first in row 2), 1 pair, 1 bridge choice.
       Hidden pods start from the second row and are never in a group.
     - checks the exact balance of every material and the pod rules.
     - screens with 40 random games, then saves a winning solution: a full-information search, replayed on the live
       timeline. It also records the same actions tapped at once, as the lower bound of time.
     - runs the §5 random test: 100 games, widened to 400 when the Wilson interval crosses a border; uniform over
       legal actions plus waiting before a scheduled event; ×1.
     - rechecks the doses against the **measured** class.
     - checks fairness of hidden pods: every world where the hidden pods swap material and count must still win after
       following the saved solution until a differing pod shows.
     - runs the simple strategies of §11 (left/right hero choice) and the rhythm by fifths of W of §10.
   - Accepted candidates go to `content/rounds/bank/P####.json` with all measures. A picture whose pods cannot fit
     §3 is recorded with its reason.
   - **The owner's amendment of 2026-10-27** (17 §2, §5, §7): Normal candidates blend the queue lightly so a regular
     Normal lands in 50–75 % (median 50–60 %); lead doses follow the cycle 50/80/40/70/100/60 % of the range
     (`DOSE_CYCLE`, recorded as `dosePos`); Hard and Super Hard candidates that the greedy strategies win ("most work now";
     for Super Hard also "release a place fastest") are refused. `place` requires a mechanic on Hard (except L5), a lead and
     an additional one on Super Hard, a mechanic in two of three regular Normals, 3+ hidden colours for Hard / Super Hard
     pictures, and no weak picture without a mechanic. Build per class: Normal `none,hidden,links,links+hidden`, Hard
     `hidden,links,links+hidden`, Super Hard `links+hidden`.
   - **The owner's second amendment of 2026-10-27** (after pilot v11; 17 §5, §6, §8, §9):
     - Hard and Super Hard pass **"opening + colours"** (`openingTest`): no first launch followed by the greedy
       strategies wins, and at most 35 % of three-launch openings win. This replaces the greedy check from the start.
     - A Normal search also looks for a candidate that "most work now" loses; `place` keeps about one in three regular
       Normals on L4–24 (one in two from L25) lost by it.
     - A Normal candidate moves a deep-colour pod into row 2; `place` requires that holding pod (`holdPod`) in a
       regular Normal.
     - Every level from L4 offers a single launch in at most 35 % of decisions (`forcedShare`).
     - Bridges: the first introduction keeps "both win, one 10+ points better" (`bridgeKind: first`). The second
       introduction needs exactly one winning bridge, with both sites reached in random games (`bridgeSingle`,
       `bridgeKind: single`), and with the other bridge random games still launch pods after the choice (the median at
       least one: `bridgeLoss`, "проигрыш наступает не сразу при выборе"). Later bridge levels take the wave plan's
       `bridgeKind`: `single` as above (three quarters), `better` both winning and one 10+ points better by the
       random test (a quarter; without a plan, single). Each bridge is solved with a fresh solver memo
       (`bridgeChoicesWin`): before 2026-10-28 one memo served both and every bridge level measured [x, x], so no
       candidate could ever be `single`.
     - Hidden-pod introductions carry 2–3 hidden pods, the first in row 2. One of them must matter (`hiddenMatters`):
       it reveals in the first half with a new colour and is launched within two launches.
     - All of these are stored in the candidate's `checks` and recomputed by the audit.
3. **Read the reasons.**
   - "Не тот класс" or "ниже 5 %": that class did not come out of this picture within the budget. This is normal: it
     stays available for other classes.
   - "Решение не найдено за бюджет" means **not checked**, not impossible (17 §1, §13).
   - Do not widen the board, add every mechanic or weaken a check to fill a number (17 §13).
4. **Place.**
   - Command: `node tools/rounds/levels.mjs place <from> <to>`.
   - It fills each empty position with a banked candidate of exactly that class whose picture fits the level's stage
     (§2). Only allowed types are used, introductions only by their own introduction candidates, L1–L3 only by their
     fixed pictures.
   - A position of an approved wave plan (`content/rounds/runs/<wave>/mechanics.json`) takes only a candidate with its
     planned recipe and, for a bridge, its planned crossing side and bridge kind; an open one is listed in `needs.json`
     with the recipe, side and strip it needs.
   - It keeps the §11 variety rules (no lead mechanic or identical recipe more than 2 levels in a row), at most 2
     pictures with 10–11 colours in any 20, and prefers the class's preferred zone (75–95 % for the first Normal after
     a Hard or Super Hard) and the size mix per 30.
   - It writes `content/rounds/levels/L####.json`: the definition in the pilot format plus the card data. It marks
     the picture used and lists empty positions in `content/rounds/needs.json`.
5. **Look at the result** (17 §13.9).
   - **Audit the placed range against 17** (the owner, 2026-10-27: "в конце перепроверь, что правила были соблюдены"):
     `node tools/rounds/audit.mjs <from> <to> --out content/rounds/audit-L####-L####.md`. It recomputes every level from
     its file and picture (stage, balance, pods, columns, the engine's own validation, calendar and introductions, the
     bridge approach and choices, class and protocol, the saved solution replayed, budget, sequence and motifs) and lists
     soft notes apart. Any НЕТ is fixed (`unplace`, rebuild, `place`) before the levels are shown; the report goes to the
     owner with the levels.
   - Check each card with `node tools/rounds/levels.mjs card <n>`.
   - Theme variety (`product/design/Bloomlings-picture-theme-catalog.md` §7, soft): in any 20 levels at least 8 families and 4 directions once the pool is
     large enough, one family at most twice in a row, one subject not again within 20, no run of the same container.
     Pictures carry `family` / `direction`; the tool does not enforce this yet, so check the placed run and swap free
     pool pictures of the same class and stage when a run breaks it. The same holds for motifs (`pictures.mjs motifs`:
     fish, robots, cups …): one motif at most once in any 10 levels. Build only a varied set of pictures for a range, so
     place cannot pick three fish in a row.
   - Watch for: a dull or overloaded finish (launches after 80 % W should be 1–4, up to 6 on a long Super Hard; last
     launch at about 80–92 % W), empty waiting, and Hard or Super Hard won by all simple strategies (check the
     marking).
   - When a level should be played to judge it, say so. Bot results are not human win rates.
6. **Report and loop.**
   - Report to the owner the filled levels with class, picture, recipe and random-test share, then the empty
     positions from `needs.json`.
   - Suggest `bloomlings-round-pictures` for those needs, with each need's class, stage and required mechanic.
   - Leftover banked candidates are used in the next calendar iteration.
7. **Commit** `content/rounds/bank`, `content/rounds/levels`, `content/rounds/needs.json` and the manifest. The levels
   are not installed into the production catalog or the Unity client by this skill.

## Acceptance (17 §14) — what must hold for every placed level

- **Picture:** approved and recognisable.
- **Balance:** exact per material; pods and groups within the rules.
- **Mechanics:** supported by the round engine.
- **Solution:** a complete solution without boosters at the real places (5; 4 → 5 once repair exists), replayed by the
  engine.
- **Fairness:** no forced guessing of hidden values; no short time window.
- **Class:** confirmed by the single random test, or a borderline case (`borderline: true`) shown to the owner.
- **Introductions:** the introduction order is respected.
- **Card:** carries everything §14 lists: picture, number, class, N and W, materials, P, columns, recipe and doses,
  random test, saved solution, buffer by fifths, simple strategies, launches after 80 % W, time.

After any change of movement, slot release or bot policy, re-run the random test of the placed levels (17 §5). A level
whose class moves is re-placed.
