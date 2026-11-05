# Round tools

Tools of the skills `bloomlings-round-wave`, `bloomlings-round-pictures` and `bloomlings-round-levels`. The rules are in
[`product/17_LEVEL_GENERATION_RULES.md`](../../product/17_LEVEL_GENERATION_RULES.md) and the content in
[`content/rounds`](../../content/rounds/README.md). Node only: no install, PNGs are written without an image library.

```sh
# a wave (17 §13): plan → pictures → run → the owner's sample → review
node tools/rounds/rounds.mjs plan 51 100                 # mechanics breakdown, positions, ONE picture per non-bridge position (picture-first), supply, order → runs/L0051-L0100/plan.{md,json}
node tools/rounds/rounds.mjs run 51 100 --jobs 3         # dry place, build each position's picture × class × recipe (swap on failure), place, audit, summary, sample pilot
node tools/rounds/rounds.mjs run 51 100 --limit 10       # only the first 10 open positions (timing per position: runs/…/timing.json)
node tools/rounds/rounds.mjs run 51 100 --jobs 2 --rebuild auto   # a built wave: take back and rebuild what the rules no longer accept (wave.mjs rebuildSet → runs/…/rebuild.json)
node tools/rounds/rounds.mjs run 21 70 --sweep           # the old path: every free picture for every open position
node tools/rounds/rounds.mjs review results.json         # owner's ratings next to the metrics → content/rounds/reports/
node tools/rounds/rounds.mjs bridges 21 50                # which bridge sites each bridge level shows from its start (17 §8, 2026-10-31)
# pictures from SVG drafts and the screen before the sheet (17 §13)
node tools/rounds/svg2cells.mjs drafts.mjs --out content/rounds/pictures/src/batch-0120.mjs --sheet /tmp/s.png --level 30
node tools/rounds/screen.mjs content/rounds/pictures/src/batch-0120.mjs --level 30
# pictures (17 §2)
node tools/rounds/pictures.mjs next
node tools/rounds/pictures.mjs verify content/rounds/pictures/src/batch-0031.mjs --out /tmp/check.png   # skill bloomlings-board-picture
node tools/rounds/pictures.mjs render content/rounds/pictures/src/batch-0031.mjs
node tools/rounds/pictures.mjs check --status draft
node tools/rounds/pictures.mjs sheet --status draft --out content/rounds/pictures/sheets/batch-0031.png
node tools/rounds/pictures.mjs approve 31 32 33          # also rebuilds content/rounds/approved/ (locked/ L1–3, pool/)
node tools/rounds/pictures.mjs publish
node tools/rounds/pictures.mjs status --free           # approved codes not yet used by a placed level
node tools/rounds/pictures.mjs motifs                   # recurring motifs (fish, robots …): «ХВАТИТ» at 3+, not drawn again
node tools/rounds/pictures.mjs reject 34 --note "too small details"
node tools/rounds/pictures.mjs rights P0425 P0465 --owner "владелец 01.11.2026: взять в волну 51–100"   # a franchise picture the owner cleared
node tools/rounds/pictures.mjs replace P0034 content/rounds/pictures/src/batch-0031-fix.mjs
# levels (17 §3–§14)
node tools/rounds/levels.mjs calendar 1 60
node tools/rounds/levels.mjs build --approved --candidates 64
node tools/rounds/levels.mjs build P0012 --overlay up,left --recipes bridge,bridge+links --classes Normal --no-intro   # on the generator's wall
node tools/rounds/levels.mjs build P0042 --classes Hard --recipes links --accents on   # accents (17 §6): auto (default) | on | off
node tools/rounds/levels.mjs build P0165 --classes Hard --recipes links+hidden --pairs Hard:links+hidden --want 1 --minutes 15   # the generator (tune.mjs)
node tools/rounds/levels.mjs build P0341 --classes Normal --recipes none --slot breather --want 1   # a breather (17 §5)
node tools/rounds/levels.mjs bank
node tools/rounds/levels.mjs place 1 30
# split pictures (17 §8 «Одна цельная картинка», 2026-10-29): one whole free pool picture split by an inserted neutral strip,
# searched (resumable, chunks of --minutes) until the planned bridge kind and class hold; drafts for the sheet, preview levels
node tools/rounds/bridge-scene.mjs make --mode split --from P0074 --side up --kind better --cls Normal --recipe bridge+hidden --level 23 --minutes 35 --out content/rounds/pictures/src/split-L0023.mjs
node tools/rounds/pictures.mjs render content/rounds/pictures/src/split-L0023.mjs
node tools/rounds/bridge-scene.mjs preview content/rounds/pictures/src/split-L0023.mjs --out content/work/bridge-preview-w2
# bridge scenes (17 §8, superseded by split pictures): a smaller free pool picture behind a wall, searched until exactly one bridge wins
node tools/rounds/bridge-scene.mjs show P0012 --side left --x K --y S --z . --ground L --fill
node tools/rounds/bridge-scene.mjs make --side up --kind single --cls Normal --recipe bridge --level 26 --free --minutes 30 --out content/rounds/pictures/src/scenes-0130.mjs
node tools/rounds/pictures.mjs verify content/rounds/pictures/src/scenes-0130.mjs --out content/rounds/pictures/sheets/scenes-0130.png
node tools/rounds/levels.mjs card 15
# transfer into the game (spec 006): the owner's sample confirmation, then the export with the conformance fixtures
node tools/rounds/rounds.mjs confirm 1 20 --sample product/playtests/pilot-v12-owner-2026-10-27/results.json
node tools/rounds/export.mjs 1 20 --out content/game/rounds/export/L0001-L0020 --owner-results product/playtests/pilot-v12-owner-2026-10-27/results.json
node tools/rounds/transfer-log.mjs exported content/game/rounds/export/L0001-L0020   # then validated / published / confirmed
node tools/rounds/conformance.mjs solution content/game/rounds/export/L0001-L0020/levels/L0005.json --window 7   # the reference's log of one window
node --test tools/rounds/test/*.test.mjs
```

`ROUNDS_DIR=<folder>` points every command at another content folder, for trial runs.

**Speed** (2026-11-02): the measures play on a verified fast twin of the reference engine and on worker threads, byte-identical to the reference (`perf/README.md`: how it is verified, `ROUNDS_ENGINE=reference`, `ROUNDS_THREADS`, early stopping for screening only).

## The generator (2026-11-02)

The owner approved one generator (2026-11-02): today's builder plus the calibration's queue tuning (`calib/README.md`),
links chosen against the greedy strategy and under the forced-decision limit, information overlays after the class,
breathers and a picture-first wave. `levels.mjs build` runs it for every non-bridge level of a free picture
(`tune.mjs tuneJob`); `--legacy` keeps the old random / steered blend, bridge levels and L1–3 keep it too.

- **One job** = one picture × class × recipe × slot (`regular`, `after-hard`, `breather`, `intro`, `Hard`, `Super Hard`;
  `--slot` for a Normal, a picture of up to 550 cells is a breather by default). Its zone: a regular Normal 50–65 % aiming
  at 56 % (the owner, 2026-11-02: the middle of the median's 50–60 %, not anywhere in 50–75 %), the first Normal after a
  Hard 75–95 %, a breather 75–90 %, an introduction 90–100 %, Hard 20–50 % (aim 33 %), Super Hard 5–20 % (aim 12 %). A
  tuned combination carries the generator's revision (`wave.mjs GEN`, today `2026-11-02c`) in `bank.searched`, so a new
  revision searches it again; banked candidates and placed levels carry it as `gen`.
- **Plan from the model.** `calib/model.json` (the calibration's logistic share model, copied from
  `content/work/calib/model.json`; refit with `calib/fit.mjs` and copy again) gives the blend `t` of `candidate` (0 = the
  access order, 1 = its reverse) that lands the zone with the highest probability for the picture's features, the
  columns, the class's noise, the pods and the link dose. A picture remembers where it landed: `bank/<P####>.json`
  `tune["cls|recipe|slot"] = {t, cols, offset, quick}`; the next job starts there (± 0.03).
- **≤ 5 quick tests** per attempt (40 games of the §5 random policy each): Newton steps on `t` with the model's slope until
  both sides of the zone were seen, then bisection inside the bracket (`nextT`). Only a queue that measured in the zone
  (± 15 points) goes on. A picture that never landed in the zone and stays above it even reversed (`t` ≈ 1), or below it
  in the access order (`t` ≈ 0), in three attempts (six on Hard and Super Hard) stops the job early, so the runner swaps it.
  A regular Normal with links whose queue alone stays above the zone even reversed takes its links in the Hard style (a
  trap pair first, against the greedy strategy) at the top of its dose range from then on (`bank.tune` `trap`; the
  calibration: trap links cost about 32 points).
- **Links one pair at a time** (`chooseLinks`, every class but an introduction; the candidate is built with
  `noLinks`): pairs of two columns at one start depth (the engine's rule), never a hidden pod or one behind a latch.
  Heuristics (lower is better): Normal near access keys, Hard and Super Hard a pod useful now with a later one and a trap
  pair first (17 §8); short holds (partners reach the top at about the same time in a reference greedy game,
  `latchTiming`); a pair that leaves at most one column free at a moment costs (`maxBlocked`); a row next to a pair
  already chosen costs (links spread along the queue). Hard and Super Hard try the best five by how far "most work now"
  (and "release fastest") gets (`greedyReach`, least far first). Every class takes the first pair that keeps the forced
  share (`measure.mjs forcedShare`, 10 games) at most 35 %, up to 12 pairs tried; at the end the full 30 games of
  `evaluate` must hold too (else the choice is redone with 30 games per pair). When the dose place of 17 §7 cannot hold
  the limit on that queue, the lead's minimum dose is used (recorded as dose place 0; the placer weighs the place softly).
- **Hidden pods and hidden cells after the class** (`addInfo`, the candidate built with `noInfo`): the calibration found
  that they change none of the §5 tests, so they are added by the recipe's dose once the queue measured in the zone, with
  no re-tuning; their fairness is still checked by `evaluate`.
- **Acceptance**: `evaluate` (all checks; a breather the mandatory ones only, below) and the placer's filters of the slot
  (`slotProblems`: class, forced, opening, latch, picture fitness, holding pod, calm rule, trap link), then the zone.
  `--want n` levels (default 2), `--want-loss` also one that "most work now" loses (17 §5; it goes on past a loser whose class is borderline, up to
  six accepted, and with links every second attempt takes the top dose place on 4 columns where the size allows: the
  losers of L21–L150 were nearly all such), `--minutes m` on top of the
  64 candidates of §13.5. Every candidate that passes `evaluate` goes to the bank (with `inZone`, `slot`, `blend`, `tune`);
  the build prints `@@tune {json}` for the runner.
- **Breathers** (17 §5 «Передышка», the owner, 2026-11-02): a Normal on a picture up to ~550 cells (`BREATHER_N`) or a
  position the wave plan marks `breather: true` (`mechanics.json`). Zone 75–90 % (`BREATHER_ZONE`), zero or one light
  mechanic (`wave.mjs LIGHT`, `breatherRecipeOk`): the plan's hidden pods or hidden cells by default (never both), sometimes
  none; no holding pod, no accents; built from the access order (`t` from 0) and accepted at the first fitting layout.
  Hidden information does not move the bot test, so a breather whose queue stays above 90 % even reversed takes one plain
  link pair (no trap; `doses(…, {breather: true})` gives links [2, 2]) in place of its light mechanic from the next
  attempt (`bank.tune` `pair`; the level's recipe is then `links`, which the placer accepts on that position); if it
  still stays above, the fitting layout nearest the zone (`zoneMiss`, a soft audit note). `evaluate(…, {breather: true})` runs only the mandatory checks (validation, balance, solution,
  random test class Normal, doses, latch rules, fairness; forced is recorded, not required). The placer and the audit
  keep breathers out of the Normal median, the «по цветам» quota, the holding-pod, calm, forced and pressure rules, give
  them the 75–90 % zone and refuse one with two mechanics, a mechanic that is not light, more than one link pair or a trap
  link (`breather: true` on the level). A small picture never takes a Normal position whose recipe is not light.
- **Picture-first wave** (the owner's principle 17): `rounds.mjs plan` gives each open non-bridge, non-hand position of a
  planned wave one free picture (`plan.json` `assign`, the table «Картинки волны»; `wave.mjs assignPictures`): the size
  mix per 50 (30 × 800–1200, 10 × 700–900, 5 × 400–700, 5 × 1200–1500, scaled), each free position the band furthest
  behind its even share and never three of one band in a row, pins on their reserved positions, breathers the small
  band (pictures up to ~550 cells, 3–4 colours hidden at the start and the larger first), Hard and Super Hard pictures with 3+ colours hidden at the start
  (more first), a regular Normal 2–4 colours hidden at the start (a weaker picture stays near 100 % with any queue), a
  two-type recipe 16+ pods (§7; `reservePins` checks this too), latch recipes key holes and pods enough for a latch's
  depth and the pods behind it (`latchFits`). Hard rules, never relaxed (the owner, 2026-11-02; shared by the planner, the
  placer and the audit in `wave.mjs`): a theme family, and a motif, at most twice in the wave and never within 10 levels
  of another appearance (`repeatClash`; was once a wave and not in the 20 levels before), at most two pictures of 10+
  colours in any 20 levels counted on the pictures' own colours (`wideClash`: an accent colour of §6 does not count).
  Soft rules, relaxed in this order when no picture is left: neighbours from different packs, then the band (recorded as
  `relaxed`); a position with no picture within the hard rules is listed as missing. An earlier assignment is kept while
  its picture is free and it still keeps the hard rules (`--reassign` draws anew); a pin keeps its earlier position.
  «По цветам»: the regular positions still to build take `--want-loss` as many as the wave's placed regular Normals are
  short of about one in two from L25 (one in three on L4–24), spread evenly. `run` builds
  each assigned position (`levels.mjs build <P> --pairs cls:recipe --slot … --want 1 --minutes …`, default minutes by
  slot: regular 8, with `--want-loss` 10, after-hard 6, breather 4, intro 6, Hard / Super Hard 15) in `--jobs` processes; a
  picture with no level in the zone is swapped for a free picture of the same band (`wave.mjs replacement`, at most
  `--swaps 2` in a run), never a sweep; a pinned picture is first built once more afresh (`--fresh --seed 2`), and only
  then swapped (the placer lists the pins left unused). Accents keep the stage's palette of 10 colours (`--max-colours`
  stays for a hand build; the runner no longer lowers it).
  The placer takes a regular Normal outside the tuning zone 50–65 % (`REGULAR_ZONE`) only as a fallback (+1.5), and the
  dry placement leaves such a position open (`ROUNDS_DRY`) so it is built. It weighs a regular Normal by how far it moves
  the wave's 20-level windows out of the median's 50–60 %
  (`medianPenalty`, weight 4) and by its distance to the aim 56 %, and prefers a level «по цветам» loses while the wave is
  short of the quota (`greedyQuota`: 0.6, 0.2 more for each further level short, at most 1.2). The audit checks the median
  in every 20-level window of the range (`medianWindows`, at least 5 regular Normals; a breather, an introduction and the
  first Normal after a Hard are out of it) and the quota per part (`greedyQuota`).
- **The rebuild of a built wave** (`rounds.mjs run … --rebuild auto | 59,62,…`, the owner's decisions of 2026-11-02):
  `wave.mjs rebuildSet` picks the placed levels the rules no longer accept: picture repeats and the 10+ window (the later,
  cheaper level of a clash, on another picture: `reassign`; never a pin or a hand level), breathers out of 75–90 % or off
  their light recipe, regular Normals while a 20-level window's median is out of 50–60 % (the furthest share first, one
  «по цветам» already loses counted 5 points lower; a level sent back counted at the aim), then while «по цветам» is
  short (a level sent back counted as losing it). Levels built under the current `GEN` keep their pictures and recipes
  (repeats, the colour window, a breather's recipe), but the sequence rules take them too (2026-11-05: a wave built
  wholly under the current `GEN` could otherwise never be fixed). A level a rebuild already placed stays, unless it went
  in as a regular Normal outside its tuning zone (a fallback) or the placement put its old candidate back (taken again,
  at most 3 takes per `GEN`, `REBUILD_TAKES`; so is one that missed its purpose: still winning «по цветам», still above the median, or a breather still out of 75–90 %); a breather left out of 75–90 % at one plain link pair or its nearest
  layout goes to another small picture (`reassign`; the planner's breather prefers 3–4 colours hidden at the start and
  the larger of the small pictures, as the breathers that landed in L51–L100). Each is taken off (`levels.mjs unplace`:
  the old candidate stays in the bank as a fallback) and recorded in `runs/<wave>/rebuild.json` (why, from, later to,
  `takes`); the dry placement holds them open (`ROUNDS_HOLD_OPEN`), so they are built again, searched afresh
  (`--fresh --seed 1+takes`: the bank remembers the combination as searched), and the placement chooses among old and
  new candidates. Resumable: a recorded level not yet placed stays held, one placed again is `done`.
  Run it again after a pass to take what is still out (each pass is short: only the levels sent back are built).
- **Temporary copies**: the dry placement copies `content/rounds` without `approved/`, `daily-2027/` and the runs' logs,
  skips the publish there (`ROUNDS_NO_PUBLISH`) and deletes the copy in a `finally`; every test sandbox deletes itself
  (`node:test` `after`).
  `levels.mjs place` gives an assigned position only its picture and keeps assigned pictures off other positions. Bridge
  and hand positions are skipped and listed. `--sweep` keeps the old full sweep; `--limit N` builds the first N positions.
- **Validation** (`calib/plan.mjs gen2`, `gen2-report` → `content/work/gen2/validation.md`): the calibration's eight jobs.

**Bridges on any strip** (17 §8 "Где стоит мост", amendment of 2026-10-28). A picture's `#` strip may run in any
direction; `b` marks a bridge site on it. `boardOf(pic, overlay?)` builds the board: each `#`/`b` cell on the picture's
outer edge closes the ring cell directly outward (a full-width stream row closes both sides, a full-height wall column
the top and the bottom, an L its two edge ends), so the far part needs a bridge; each site's anchor is its one orthogonal
neighbour reachable from the entry while all sites are walls (else `BridgeAnchorError`), and its choice carries
`dir` (`up`, `left`, `right`, `down`: the crossing) and a label (`Мостик вверх слева`, `Мостик влево снизу` …). The engine
accepts an anchor above, below or beside the site in the same row. `wallOverlay(pic, side, seed)` lays the generator's
wall over an approved picture (`up`: a full-width row; `left`/`right`: a full-height column, the entry on the open side;
`two`: an L closing a top corner): 25–45 % of the colour cells behind it, two sites at least 3 apart whose near-bank
anchors are colour cells. A level built on it stores `definition.overlay` = `{walls, sites}` in picture coordinates
(x from the left, y from the top); the exporter writes it as `board.overlay` of `round-level.v1`, the audit rebuilds the
board from the approved picture plus the overlay, N and W count the board with the wall, and the pilot draws it as
stone. `bridgeSide(level)` is the crossing of the group: the common direction, or `two` for different directions or
strips.

**Retired on 2026-11-01** (17 §8 «Только контур, без разреза»: every bridge level is a hand-drawn contour picture, e.g. `content/rounds/experiments/contour-bridges-51/`); kept for the record: **Split pictures** (`bridge-scene.mjs make --mode split`, 17 §8 «Одна цельная картинка», the owner, 2026-10-29: «сделать
одну большую картинку и просто в рандомных местах разделить её и поставить мосты»). The main path for a planned bridge
position (`strip: gap`). `splitOf(src, {side, at, sites, corner})` inserts a strip line into one whole picture: `up` a row
before row `at`, `left`/`right` a column before column `at`, `two` an L (`at = [x0, y0]`, corner `tl` or `tr`) whose
corner block moves one cell out diagonally; no picture cell is overwritten (`map` says where each source cell went). The
strip is `#` with two `b` sites and reaches the ring on both ends, where `boardOf` closes the ring; `checkSplit` adds to
the scene checks: no cell lost, the far part 25–45 % (`GAP_SHARE`), nothing beyond the strip open without a bridge, and
the gap contrast (every colour cell beside the strip at least `GAP_CONTRAST` 15 from the board's surface, the game's
`board.surface` and the pilot's ground: only Жемчуг falls below). `splitsFor` ranks positions × site pairs (anchors and
far cells of different materials, far cells deep, a share near 35 %, sites apart, the strip through the subject),
spread so neighbouring cuts do not come first; `trimmed` drops empty edge rows/columns only when the board would exceed
48×48. `make --mode split` searches them with the builder (`levels.mjs candidate` with the lean, gate and delay of the
scenes, `evaluate` with the planned kind; a Normal also steered into 50–75 % with its holding pod) and writes a draft
module (`<title> — мост`, `scene.mode: 'split'`) plus the evidence with the found level; it resumes from the evidence.
`boardOf` gives a split picture's board `strip: 'gap'`, the level carries `definition.strip = 'gap'`, and the pilot draws
it as its ground with a faint edge, the bridges over it. `--share` and `--gap-tolerance` are explicit exceptions,
recorded in the scene. `preview <modules> --out <dir>` writes the found levels in the placed-level shape for the pilot.
The search steers each class's queue blend by the shares it measures, stops a Super Hard with no quick win before the
per-bridge solver (minutes on a 48×48 board), and for a single deciding bridge prefers shallow approaches near the entry
and exposes the other approach colour early (`candidate` opts `expose`). `bridgeSingle` counts which site is buildable at
the first bridge moment in the games of the §5 random test (with waiting; the coordinator's decision of 2026-10-30, 17 §8
"в части партий случайного теста"); its older games tapped whenever they could and jammed early on a Super Hard queue
(`policy: 'tap'`, kept for comparison; the placed L16 and L18 measure the same under both).
`ROUNDS_TRACE=1` logs each check's time; `--retry <regex>` tries matching splits once more after a change.

**Bridge scenes** (superseded on 2026-10-29; `bridge-scene.mjs`, the owner, 2026-10-28: "взять картинку поменьше и вокруг неё строить мосты и
стены … чтобы был только один выигрышный мост"). The main path for a planned bridge position. `scene(pic, opts)` composes
a new picture from a smaller approved one: the picture is the far side, cropped to its silhouette (its empty space kept,
`z: '.'`, or filled with one environment colour), framed by `#` on every edge that faces the ring; a wall with two `b`
sites (`up`: a full-width row; `left`/`right`: the far side in a top corner, both sites on its vertical wall, a band of
open ground below it so the entry stays in the bottom middle; `two`: an L, one site crossing up, one crossing left);
behind each site a strip (`strip` rows deep) of its own colour, a `#` divider between them; on the near bank a bed
before each site (its approach) and a meadow of the one extra colour (`ground`, `fill`, or only round the other bed:
`shield`). The winning site is the `x` bed's, its strip is `y`; the other site's bed is `w` and its strip `v` (without
`w`, `v` the pilots' crossing: `y` and `x`). The builder's queue (`levels.mjs candidate` with `opts`) leans to the winning
site, keeps every pod of the gate `v` at least `gate` rows deep (but the first when `v` also lies on the near bank) and,
with `delay`, the other bed's pods a little deeper; with the other bridge the places fill before the gate's pods come.
`make` draws scene parameters by seed, runs candidates through `levels.mjs evaluate` (the same checks as `build`, the
planned `--kind` first: `single` = with the other bridge the solver proves a loss and random games still launch pods
after the choice, `bridgeLoss`; `better` = both win, one 10+ points better) until `--count` scenes pass, and writes them as
a draft module (`<title> за мостом`, the source's family, `source: сцена моста из P####`, `scene: {from, side, kind, win,
lose, opts}`) plus the evidence `<out>.json`; the owner approves them on the sheet like any picture, and `build` reads the
record's `scene.win` (`sceneOpts`). `stripsOver(pic, {a, b, depth, win, border})` lays the two strips behind the sites
of a picture that already has its wall (a drawn strip picture or a wall overlay). `rounds.mjs plan` lists the scenes a
wave needs (side, kind, class, recipe and the `make` command), `run` takes approved free scene pictures for those
positions, and `levels.mjs place` checks the plan's `bridgeKind`. A scene is found in about 1–20 minutes on one core
(each candidate 2–10 s); `--opts '{…}'` repeats a known scene.

**The wave's mechanics plan.** `content/rounds/runs/<wave>/mechanics.json` (approved by the owner, 17 §13 step 1): per
level `recipe` (lead first), `lead`, `add`, `bridgeSide` and `strip` (`new`: a picture with its own strip, made as a bridge scene; `overlay`: the
generator's wall) and `bridgeKind` (`single`: exactly one bridge wins; `better`: both win, one 10+ points better; 75 % / 25 %). `rounds.mjs plan` prints its breakdown and orders pictures only for what it needs; `rounds.mjs run`
builds only each open position's planned recipe (`levels.mjs build --pairs cls:recipe@side --overlay …`); `levels.mjs
place` accepts a candidate on a planned position only with that recipe and, for a bridge, that crossing side and bridge kind. Recipes:
`none`, `links`, `hidden`, `bridge`, `links+hidden`, `hidden+links`, `bridge+links`, `bridge+hidden`, `hidden+bridge`,
`links+bridge`, `links+bridge+hidden`, `bridge+links+hidden`, and with hidden cells `cells`, `links+cells`, `cells+links`,
`bridge+cells`, `bridge+links+cells`, `bridge+cells+links` (never `hidden` and `cells` together), and with the latch
`latch`, `links+latch`, `latch+links`, `latch+cells`, `links+latch+cells`, `links+cells+latch` …; doses by §7 (the lead its range with the dose place cycle,
an additional one its minimum admissible dose, three types the lead in its lower half).

**Hidden cells** (17 §8 «Скрытые клетки», the owner's amendment of 2026-11-01; recipe and mechanic name `cells`,
«скрытые клетки»). The exact contract, which the C# port follows:

- *Level data.* The round level's `definition.hiddenCells` (exported as `board.hiddenCells` of `round-level.v1`, and
  `mechanics` then includes `"cells"`): cell indices `y*w+x` of `rowsBottomFirst` (row 0 at the bottom, as bridge
  choices), sorted ascending, unique, at least one, each a material cell. `R.validate` refuses anything else (`hidden
  cells`, `hidden cell not a colour cell`) and a level that also has hidden pods (`hidden cells with hidden pods`).
  A level without hidden cells has no `hiddenCells` key.
- *State.* `state.veiled` (a byte per cell, 1 = hidden and not revealed yet; `null` on a level without hidden cells) and
  `state.veiledLeft` (how many ones). The material of a hidden cell is in the board from the start; nothing else changes.
- *Reveal rule.* A hidden, unrevealed cell reveals as soon as it has an orthogonal neighbour that is open and reachable
  from an entry, or is itself an entry cell — exactly the condition for being a target (`reach().targets`). It is
  checked at create (the start reveal, no events: part of the start state) and after every board change: in
  `rules.step`/`settle` after each round's clears (before that round's `complete` events) and after a bridge (right after
  the `bridge` event); on the timeline after each single `clear` step (right after it, `at` = that clear's time) and
  after a bridge commit (in the commit's events, `at` = the commit time). All cells a change reveals come in increasing
  cell order, each `{type:'cellReveal', cell}` (`at` added on the timeline). The conformance log line is
  `<µs> cellReveal cell=<c>`.
- *Why nothing else changes.* Open cells never close, so reachability only grows and a target stays a target until it
  is cleared: a hidden cell is never a target or a passage while hidden (it is a colour cell, not open), and it is
  revealed at the very moment it would become a target. Targeting, dispatch, release, status and every outcome are the
  same as without hidden cells; `veiled` is a function of the board (a hidden colour cell that is not a target), so
  `signature()` leaves it out and a restored timeline checkpoint recomputes it (`syncCells`). A level without hidden
  cells plays byte-identically (events, states, signatures; checked by `test/cells.test.mjs` and a replay of every
  placed level).
- *Information.* `R.view` shows an unrevealed hidden cell as `R.HIDDEN` (−3); `R.known(state, i)` is false for it. The
  strategies that read colours off the board (`tools/level-stats/strategies.mjs` top-down/bottom-up) skip such cells;
  the ones that read only targets (most-now, fast-release, the opening test, holdPod, pressure) never see one. The random
  and calm tests are unchanged. `trapLinks` and `fitness` read access depth of the start board, where every start-front
  cell is revealed, so they are unchanged too.
- *Dose* (§8 table): only the cells still hidden after the start reveal count (`cellsHiddenAtStart`). Shares of N
  (the board's colour cells): lead Normal 5–10 %, Hard 10–20 %, Super Hard 15–30 % (also the small-queue table); an
  additional one 5–8 %, at its minimum; an introduction (51, 53: Normal, alone, random test 90–100 %) 5–10 cells.
- *Builder.* `pickHiddenCells(board, want, r, {intro})`: colour cells off the start front (access depth ≥ 1, so all stay
  hidden), not behind a bridge (the far bank, so the bridge choice never hangs on an unknown colour) and no bridge anchor;
  one to three compact patches grown from deep seeds (an introduction: one patch round a seed of middle depth).
  `evaluate` refuses `hidden`+`cells`, checks an introduction's 5–10, the dose for the measured class, and the fairness of
  hidden cells (`cellFairness`: up to 12 worlds where two hidden cells of different materials swap them; following the
  saved solution until one of them reveals must leave a winning continuation). Bank candidates and placed levels carry
  `cells` (the count after the start reveal).
- *Audit.* Recipe and `hiddenCells` agree, every hidden cell is a colour cell, never with hidden pods, the dose after
  the start reveal for the class and role, an introduction 5–10; cells revealed at the start are a note.
- *Pilot* (`tools/gameplay-pilot-v12`): an unrevealed hidden cell is a neutral grey cube with «?»; it reveals by the
  engine's rule; the finished picture shows its colour. No lesson or popup (the owner, 2026-11-01: the player sees the
  «?» and the cells reveal by themselves).

**Latch and key** (17 §8 «Twig Засов и жёлудь ключ», §9 as amended twice on 2026-11-01: introductions on 61 and 63;
recipe and mechanic name `latch`, «засов и ключ»). The exact contract, which the C# port follows:

- *Level data.* The round level's `definition.latches` (exported as the top-level `latches` of `round-level.v1`, next to
  `tray`; `mechanics` then includes `"latch"`; an introduction's `introduction.mechanic` is `"latch"`):
  `[{column, depth, key}, …]`. `column` is a tray column; `depth` is how many pods of that column stand before the latch
  (0 = it starts on top); `key` is a cell index `y*w+x` of `rowsBottomFirst` (row 0 at the bottom, as hidden cells and
  bridge choices) of an empty `'.'` cell of the board — never a wall, a strip or a bridge site, a colour cell or an
  entry. Index `i` of the array is latch `i` of the events. A level without latches has no `latches` key.
- *Validation* (`R.validate`): `latches` a non-empty array (`latches`); each column exists and its depth is an integer
  `0 ≤ depth < column length`, so at least one pod stands behind it, and no two latches of one column share a depth
  (`latch`); each key an empty non-entry cell, all keys different (`latch key`); then two cheap checks, only when the rest
  is sound: `latchKeysBlocked` (below) finds no latch whose key can never be reached (`latch key unreachable`), and the
  columns blocked at the start (a latch at depth 0 whose key the start board does not reach) are at most
  `max(1, columns − 2)` (`latches block too many columns`).
- *The cheap key check* (`R.latchKeysBlocked(level)`, a necessary condition: 17 §8 «ключ не должен требовать материал,
  целиком запертый его же засовом», «несколько засовов не образуют неразрешимый цикл»). Start with every latch closed.
  Repeat: in each column the first closed latch (smallest depth) hides every pod at or after its depth; a material all
  of whose pods are hidden is blocked; flood from the entries over `'.'` cells, bridge-site cells and colour cells of
  materials that are not blocked; every closed latch whose key is flooded opens. Stop when nothing opens. The latches
  left closed are returned. (Optimistic: a material with any free pod counts as wholly clearable; the solver decides
  the rest, and the builder and audit solve every level.)
- *State.* `state.latched` (a byte per latch, 1 = closed; `null` on a level without latches) and `state.latchedLeft`
  (how many ones). `blockAt(state, c)` = the depth of column `c`'s first closed latch, or ∞.
- *Blocking.* A pod of column `c` at index `k ≥ blockAt(state, c)` is behind a closed latch: column `c` has no top pod
  while `heads[c] = blockAt(state, c)` (`topPods` leaves the column out), so such a pod cannot be tapped (`not-exposed`),
  is not revealed by the depth rule, and a group partner behind it is not exposed (`canTap`'s partner test also needs
  `index < blockAt`). Everything else (slots, targeting, dispatch, release, status) is unchanged: a latch is no pod, has
  no quota and takes no place. `blockedColumns(state)` lists the columns whose top is a closed latch.
- *Opening rule* (`openLatches(state, events, at)`). A closed latch `i` opens at the first moment both hold: it is its
  column's top (`heads[column] === depth`) and its key cell is reached by an open path from an entry — the same breadth
  first search the targets use: `reach(state).distance[key] > 0` (the key is an open cell, so reached = in the searched
  set). Latches are checked in increasing index; each one that opens is `{type:'latchOpen', latch:i}` (`at` added on
  the timeline). Opening changes no reach, and a column's next latch stands at least one pod deeper, so one pass finds
  every latch that opens. It is checked after every change that can make it true:
  - `create`: before the start reveal of the tops (silently, no event: part of the start state; a latch at depth 0
    whose key is already reached starts open);
  - `rules.step` tap: after all the group's `commit` (and sealed `reveal`) events, then the depth `reveal`s of the
    exposed tops (`revealTops`), then `settle`;
  - `rules.step` bridge: after the `bridge` event and its `cellReveal`s, then `revealTops`, then `settle`;
  - `settle`, each round: after the round's `clear` events and their `cellReveal`s, the `latchOpen`s, then (only when a
    latch opened) the depth `reveal`s of the pods it exposed, then that round's `complete`s;
  - timeline `commit`: after the commits / the bridge and its `cellReveal`s, in the commit's events with
    `at` = the commit time, before the depth reveals of the commit;
  - timeline `advance`: right after a single `clear` step and its `cellReveal`s, with `at` = that clear's time; the
    pods a latch exposes then reveal by the depth rule right after it, `{type:'reveal', at, pod, mode:'depth', color}`.
  The conformance log lines are `<µs> latchOpen latch=<i>` and, for such a reveal, `<µs> reveal pod=<p> mode=depth`
  at the clear's time (`specs/006-round-levels-in-game/contracts/conformance-fixture.md`).
- *Why the signature is unchanged.* Heads only grow and open cells never close, so `latched[i] = 0` exactly when
  `heads[column] > depth`, or `heads[column] = depth` and the key is reached: a function of `heads` and the board.
  `signature()` leaves it out, a restored timeline checkpoint recomputes it (`syncLatches`: passed latches open, the rest
  closed, then the opening rule silently), and `view()` adds `latched` only on a level with latches. A level without
  latches plays byte-identically (events, states, signatures, observations; `test/latch.test.mjs` and a replay of every
  placed level against the previous engine).
- *Rules of §8 the tools hold* (`measure.mjs latchCheck`, on the saved solution replayed patiently on the timeline):
  depth by the measured class `LATCH_DEPTH` Normal 3–6, Hard 2–6, Super Hard 2–8 (the data `depth`, pods before it);
  pods behind it 1–12, wanted 2–8 (a note); every latch opens on the saved solution, the last one before 80 % of W
  (done work, `processedCells / W`); at most `latchLimit(columns) = max(1, columns − 2)` columns blocked at once on it
  (3 columns: 1, 4: 2); over 30 random games the share that block more is a note. One latch per column at a time holds by
  construction (a column's next latch stands deeper and only counts once the first opened); the builder puts at most one
  in a column. Each latch/key pair is told apart by its numeral and tag colour in the pilot, keys are distinct cells.
- *Dose* (§7 tables, counts of latch/key pairs): lead Normal 1, Hard 1–2, Super Hard 2–3; additional 1; three types the
  lead in its lower half (Hard 1, Super Hard 2); a queue under 16 pods 1; an introduction (61, 63: Normal, alone, random
  test 90–100 %) exactly 1.
- *Builder* (`levels.mjs`). `keyHoles(board)`: the 4-connected areas of `'.'` cells the start board does not reach (open
  cells only, sites closed) but clearing reaches without a bridge (finite access depth with every site a wall); each hole
  with its depth (fewest colour cells to clear on the way) and its key cell (the cell nearest its centre). A picture
  without a hole cannot take a latch. `pickLatches(board, tray, pods, want, cls, r, {intro})`: each latch in its own
  column at a depth of the class range with 2–8 pods behind, never a hidden pod behind it (§7), its key in a hole of its
  own; `latchTiming` plays the candidate's queue once without latches (patient, the launch with the most reachable work
  now) and the pair (column, depth, hole) is chosen so the hole is reached a few launches after the latch comes on top
  (`LATCH_WAIT`: an introduction 1–3, Normal 0–3, Hard 1–5, Super Hard 2–8), so the latch stands closed for a while;
  a second latch on a three-column tray stands two or more pods deeper than the first; refused when `latchKeysBlocked`
  finds a key unreachable. `evaluate` refuses an introduction without exactly one latch and any candidate whose
  `latchCheck` fails for the measured class; `checks.latch` keeps it; bank candidates and placed levels carry `latches`
  (the count). The placer prefers, on the first introduction, a latch that waits closed on top on the saved solution.
  `rounds.mjs plan` counts the free pictures with a hole for the latch positions and orders more; `run` builds latch
  recipes only on pictures with enough holes.
- *Audit.* Recipe and `latches` agree, `R.validate`, an introduction has exactly one latch, the dose for class and role,
  and every `latchCheck` problem (depth, pods behind, opening on the solution and before 80 % W, the column limit) is a
  НЕТ; its notes are notes.
- *Pilot* (`tools/gameplay-pilot-v12`): a closed latch is a wooden block in its column at its place among the pods (a
  peek row behind, a full row on top) with an acorn padlock, its numeral (I, II, …) and tag colour; its acorn key lies
  on the key cell with the same numeral and colour. On `latchOpen` the block leaves the column, the acorn pops (grows and
  fades for 0.7 s) and the status line says the latch opened. The first introduction explains the latch, the second
  reminds in one line.

**Channel** (17 §8 «Drop Водный канал», §9: introductions on 101 and 103, the owner, 2026-11-05; mechanic name `canal`,
«водный канал Drop»; spec 006 FR-069). The exact contract, which the C# port follows:

- *Level data.* The round level's `definition.channel` (exported as `board.channel` of `round-level.v1`; `mechanics`
  then includes `"canal"`) = `{spring, exits: [{cell, route, rideUs}, {cell, route, rideUs}]}`. `spring` and each exit's
  `cell` are cell indices `y*w+x` of `rowsBottomFirst` (row 0 at the bottom) of empty `'.'` cells, never an entry, a latch
  key or one another (an exit may be an enclosed pocket inside the picture, cut like a key's place, or a cell at its
  edge). `route` is the channel drawn for that exit: its cells from the spring to the exit, both included, each an
  orthogonal neighbour of the one before, no repeat; it may pass over any cell (the owner, 2026-11-05: drawn over the
  cubes; the cubes under it stay ordinary work, the rules never read the route). `rideUs` is a walker's ride along that
  channel, whole milliseconds from 1 ms to 10 s (the builder: 60 ms a step of the route, at least 400 ms). Index `i` of
  `exits` is exit `i` of the command and the events. A level without a channel has no `channel` key.
- *Validation* (`R.validate`): `channel` (a bad spring, not exactly two exits), `channel exit` (a bad or repeated exit
  cell), `channel route`, `channel ride`; then, when the rest is sound, `channel spring unreachable` from the cheap fixed
  point `R.objectsBlocked(level)` (below). The spring may be reached at the start (the channel is then ready at once); 17
  §8 wants its approach cleared first, which is the builder's rule.
- *Readiness.* The channel is ready at the first moment the spring is reached by an open path from an active entry:
  `reach(state).distance[spring] > 0`, the same search the targets use. `state.springReached` (1 once reached; a function
  of the board and the choice) only times the event `{type:'channelReady'}` (with `at` on the timeline): after every board
  change, after its cell reveals and latches — `settle` after each round's clears, cell reveals and latches; `rules.step`
  after a bridge's; the timeline after a single clear's, at its time, and after a bridge commit's, at the commit time.
  Reached at the start: silent (part of the start state). `R.channelChoices(state)` lists both exits while ready and no
  exit is chosen.
- *The choice.* The command `{type:'channel', exit}` (`R.legal` adds one per exit after the taps and bridges; `R.check`
  refuses it as `channel-not-ready` before the spring is reached, after a choice or for no such exit): free, no place, no
  pause, final. `status` stays `Playing` while a choice is open (a jam is no loss: 17 §1), and the auto-tail waits for it
  (`tailOf`). It sets `state.channel` (−1 before) and emits `{type:'channelOpen', exit}` (the commit's own event, at the
  commit time), then the cell reveals and latch openings the new entry causes, then the depth reveals and the dispatch.
- *The new entry.* `reach()` starts its breadth-first search from the level's entries in their order and then from the
  chosen exit (distance 1). So a walker departs from the nearest active entry by path; among equals the earlier source in
  that order wins (the BFS parent). A route that starts at the chosen exit rides the channel (`R.rideOf`): the timeline
  adds the exit's `rideUs` before the walk out (`arrive = start + ride + max(minOut, d × outPerCell)`) and after the walk
  back (`done = clear + max(minReturn, d × returnPerCell) + ride`); the job carries `ride` (ms) and `couriers` reports such
  a walker at the exit with `riding: true` during both rides. The ride does not count in "nearest" (the owner may want it
  to). The unchosen exit never becomes a source; its pocket may join the open area by ordinary clearing and is then a
  passage. Routes already walking are never rewritten (each is made at its departure).
- *State, signature, checkpoint.* `signature()` adds `String(state.channel)` after the bridge on a channel level only
  (`…|bridge|channel|status`); a timeline checkpoint stores `state.channel` (channel levels only) and recomputes
  `springReached` (`syncChannel`); `view()` adds `channel` and `channelReady` on a channel level. A level without a
  channel plays byte-identically (every placed level's saved solution, random games and owner attempts replayed on the old
  and the new engine, and the C# reference fixture regenerated equal).
- *Conformance lines* (`contracts/conformance-fixture.md`): `<µs> channelReady`, `<µs> channelOpen exit=<i>`; the fixture
  command `{"type":"channel","exit":i,"atUs":…}`; `round-proof.v1` commands too.
- *Boosters* (C# only): none chooses an exit; Bloom Burst may open the way to the spring (ready right after the burst's
  cell reveals and latches).
- *Builder*: «Channel and shutters in the builder» below. *Not yet*: the pilot.

**Shutters** (17 §8 «Twig Деревянные створки», §9: introductions on 121 and 123, the owner, 2026-11-05; mechanic name
`shutters`, «деревянные створки Twig»; spec 006 FR-070). The exact contract, which the C# port follows:

- *Level data.* `definition.shutters` (exported as `board.shutters` of `round-level.v1`, each `{cells, code, count}`;
  the engine's level takes `material` for `code`; `mechanics` then includes `"shutters"`) = `[{cells, material, count},
  …]`: `cells` sorted cell indices `y*w+x` forming a full rectangle of colour cells (any materials; never an entry or a
  hidden cell, never under another shutter); `material` the exact material it counts; `count` ≥ 1. Index `i` is shutter
  `i` of the events. A level without shutters has no `shutters` key.
- *Validation* (`R.validate`): `shutters` (an empty list), `shutter` (bad cells, not a full rectangle, a cell that is not a
  colour cell or is an entry, a hidden cell or under another shutter, a bad material or count); then, when the rest is
  sound, `shutter count unreachable` from `R.objectsBlocked`. Doses (§7, §8: area share, counter 20–60 % of the material
  by class, multiples of 5, at least 10, the material at least 40 units) are the builder's.
- *The cheap fixed point* (`R.objectsBlocked(level)`, which `latchKeysBlocked` now is a view of): the latch flood, with
  every cell under a closed shutter a wall; once the spring is flooded both exits flood too (either may be chosen); a
  closed shutter opens when the flooded cells of its material (never under a closed shutter) are at least its count; until
  nothing changes. Returns `{latches, shutters}` never opened and whether the spring was reached. Without shutters and a
  channel it is exactly the old latch check.
- *Blocking.* `state.shut` (a byte per shutter, 1 = closed; `null` without shutters) and `state.shutLeft`. While closed a
  shutter's cells are no targets (`reach()` skips them; they are colour cells, so never a passage either): their work
  waits and a pod of that material keeps its place.
- *Counting and opening* (`R.openShutters(state, events, at)`). Work done of material `m` = its cells on the start board −
  its cells now: a courier's clear, and in the game a Bloom Burst cell, so `shut` is a function of the board and the
  signature is unchanged (`syncShutters` recomputes it at create and on a restored checkpoint). Every closed shutter whose
  count is reached opens, in increasing order, as `{type:'shutterOpen', shutter}` (`at` on the timeline): `settle` right
  after each round's clears (before its cell reveals, latches and completes); the timeline right after a single clear, at
  its time, before that clear's cell reveals. Its cells become targets at once (the reach is dropped), so a waiting pod
  departs at that very moment. `R.shutterLeft(state, i)` is the count still shown (0 once open); `view()` adds
  `shutters` (those counts) on a level with shutters. A level without shutters plays byte-identically.
- *Conformance line*: `<µs> shutterOpen shutter=<i>`.
- *Boosters* (C# only): none opens a shutter. Bloom Burst leaves the cells under a closed shutter (the material's pods
  keep that work) but the cells it clears count, so a shutter may open right after the burst, before its cell reveals.
- *Builder*: «Channel and shutters in the builder» below. *Not yet*: the pilot.

**Channel and shutters in the builder** (2026-11-05; `lib.mjs` `SUPPORTED` lists `canal` and `shutters`; generator
revision `wave.mjs` `GEN` 2026-11-05). Both are placed once per job in `tune.mjs` `tuneJob`, before the queue is tuned,
so the quick tests and every check see them; `levels.mjs candidate` copies the board's `channel` / `shutters` into the
level (`objectsOf`) and refuses a recipe that does not match them.

- *The channel's places* (`levels.mjs placeChannel`, `CHANNEL`): the spring and each exit is one cell, either a colour
  cell enclosed by colour on its four sides that the level takes out (it joins `overlay.keys`, the list of the picture's
  colour cells the level leaves empty; no format change, the C# `RoundPictureBoard` lays them the same way) or an empty
  pocket of the picture the start does not reach. Access depth (colour cells to clear first, `depths`): the spring in
  8–35 % of the deepest colour cell's (at least 2: behind work, never ready at the start, which `evaluate` and the audit
  refuse), near the entry's column; the exits at least 50 % deep and 3 deeper than the spring, at least 0.4 × √(area)
  apart and turned at least 80° from each other as seen from the spring (two different inner regions), cells of the
  channel at least 3 apart; preferred: the deepest, farthest pair, different materials round the exits, short routes. A
  thin picture without such a pair is tried once more relaxed (`CHANNEL_RELAXED`: 40 %, 0.28 × √area, 55°). Each route
  is two straight legs (row then column or column then row; of the four combinations the pair sharing fewest cells),
  ride 60 ms a step, at least 400 ms. Latch keys never take a channel cell (`keyHoles` skips them; `carveKeySpots
  {avoid}` keeps 5 cells away).
- *The queue for an exit* (`channelDepths`): the candidate orders its pods as if one exit (drawn per attempt) were chosen
  the moment the spring is reached (a cell's depth = min(from the entries, spring depth + from the exit)), so that exit's
  region is played earlier and with the other exit its pods wait: the choice matters.
- *Shutters* (`placeShutters`, `SHUTTER`; 17 §7, §8): number by `doses` (`SHUTTER_T`: lead Normal 1, Hard 1–2, Super Hard
  2–3, additional 1, introduction 1; in the wave plan L101–L150 every shutter level has one); total area as a share of
  N (lead Normal 3–8 %, Hard 5–12 %, Super Hard 8–15 %, additional 2–5 %, introduction Normal's 3 %), each a full rectangle
  of colour cells with sides 3–8 and aspect at most 2, off the start front, mean depth near 45 % of the deepest, never
  over the channel's cells and routes or an accent, never closing another colour cell off (with it as a wall everything
  else stays reachable), 2 cells from another shutter. Its counter (`shutterCounts`): 17 §8's share of the counted
  material's work on the board (Normal 20–35 %, Hard 30–50 %, Super Hard 40–60 %), a multiple of 5, at least 10, the
  material at least 40 units, the lead at its dose place in the range, an additional one and an introduction at the
  class's lower bound, at most 85 % of the material's work outside the shutters; preferred a material scarce under the
  rectangle. Hidden cells never stand under a shutter; an accent under a shutter is dropped, one over its counted material keeps
  the accent and fits the counter again to that material's recoloured work (`refitShutters`).
- *Doses and checks* (`dosesOk`, `evaluate`, `place mechOk`, `audit.mjs`): the recipe and the objects agree; shutters'
  number and counters by class and role; the saved solution opens every shutter before 80 % W (`measure.mjs shutterCheck`;
  no cell under it reachable at once is a note); `channelExits` solves the level with each exit alone (a fresh solver
  with `keep`) and plays 60 games with each exit only (`checks.channel`: `each[{exit, win, share}]`, `decides` = exactly
  one exit wins); both introductions need both exits winning (17 §9).
- *Measurement.* The §5 random test is unchanged: the bot chooses uniformly among all legal actions, and while the channel
  is ready each exit is one of them (with the pods and the wait), exactly as `RoundRandomPolicy` (C#) and the
  conformance fixtures. *A deciding-exit Super Hard* (the owner, 2026-11-05: the wave plan's `channelKind: "single"`, exactly one
  exit wins) is measured with its winning exit only, as a bridge level with its best bridge (17 §5): `levels.mjs evaluate`
  with `channelKind: 'single'` (`levels.mjs build --channel-kind single` for a Super Hard canal recipe; only on Super
  Hard) first requires `channelExits(level).decides` (else «канал: решает не ровно один выход (single)»), then takes the
  quick estimate, the random test and the calm test with `exit` = that exit (`quickShare(…, bridge, exit)`,
  `randomTest(level, {exit})`, `calmTest(level, {exit})`; `perf/games.mjs keepOf`: the bot's channel actions keep that exit,
  every other action stays), keeps the test over both exits as `randomAll` and records `checks.channelKind: 'single'` and
  `random.exit`. `audit.mjs` holds such a level (the plan's `channelKind`, `checks.channelKind` or `random.exit`) to a
  deciding channel, `random.exit` the winning exit and Super Hard; `levels.mjs adopt` refuses a hand level of a
  `channelKind: "single"` position measured otherwise; the card shows the exit and the share with both exits. The export
  writes the exit into the proof's `randomTest.exit` and the fixture's `random.exit` (`conformance.mjs randomFixture
  {exit}`), and `rounds validate` replays those games with `RoundRandomPolicy.Keep(bridge, exit)`. Without `channelKind`
  every level measures exactly as before. The saved-solution search (`level-stats/solve.mjs` `prefer`) tries an exit as soon as one may be
  chosen, exit 0 then exit 1, so the proof shows the choice; the simple strategies take an exit at once (left the first,
  right the last; `strategies.mjs bridgeFirst`, `perf/games.mjs heroChoices`), and forced decisions count taps only.
- *Pictures.* Carving one cell for the spring and each exit is enough: no picture needs to be drawn with pockets.
  `rounds.mjs plan` gives a channel position only a picture `placeChannel` finds places on (pool entries' `channel`,
  `wave.mjs eligible`).
- *Wave L101–L150* (`content/rounds/runs/L0101-L0150/mechanics.json`, the owner's plan of 2026-11-05): the Super Hards with
  a channel (110, 130, 150) are `by: "hand"` (`channelKind: "single"`: exactly one exit wins), skipped by `place`.

**Picture selection** (the owner, 2026-11-01).

- *Pins.* `mechanics.json` may carry `pin: ['P0317', …]`: pictures the wave must use. `rounds.mjs plan` reserves each
  for one fitting open position (stage; a non-bridge position for a picture without a strip; Hard and Super Hard a
  strong picture; a latch recipe enough key holes): the pins with the fewest fitting positions first, a position costing
  more when other pins fit it, it neighbours a pin of the same pack, it is an introduction, a strong picture would go to a
  Normal, a picture with holes would miss a latch position, or another pin is within three levels. The table «Закреплённые
  картинки» and `plan.json` `pins` (`{id, pack, positions, reserved, status, reason}`; status free, placed, used, blocked
  with the reason: not approved, locked, a franchise without the owner's rights, no fitting position). `run` builds
  candidates for every free pin on its reserved position. `levels.mjs place` keeps a pin off the positions before its
  reserved one, prefers it strongly (score −3) wherever it passes the filters, exempts it from the pack rule, and lists
  the pins still unused at the wave's end (console and `needs.json` `pins`).
- *Packs.* `packOf(pic)`: the picture's `pack` field if set, else its `batch`; a combined batch of several themes
  (`packs-…`, the owner's packs-0268-0467) splits by theme (`<batch> · <theme>`). `place` never puts two neighbouring
  levels on one pack while another fitting candidate exists (hard), and costs a candidate 0.12 per level of the wave
  already on its pack and 0.15 when the level two back is on it (soft); `run --per-position` interleaves its pictures by
  pack; `plan` lists the free pool by pack.
- *Rights.* A picture whose `source` is a third-party universe or a meme (`сторонняя вселенная`, `мем`,
  `интернет-мем/тренд`) is kept out of rounds (`rounds.mjs`, `levels.mjs place`, the audit, the exporter) unless it
  carries `rights: 'owner'` (`lib.mjs franchiseBlocked`), set by `pictures.mjs rights <ids> --owner "<the owner's
  words>"` with `rightsNote` and `rightsAt`, in the picture and the manifest (`--clear` takes it back). Set for P0425 and
  P0465 («владелец 01.11.2026: взять в волну 51–100»). The exporter still writes `round-picture.v1` `rights: "own"` (its
  schema's only value).

**Recolouring and accents** (17 §3 «кубики можно докрасить или перекрасить в более насыщенный материал», §6 «Акценты для
давления на места», the owner, 2026-11-01). The exact contract, which the C# port follows:

- *Level data.* A level may carry `definition.recolor` = `[[x, y, code], …]` in picture coordinates (x from the left, y
  from the top, like the overlay): each a colour cell of the approved picture that this level paints another palette
  material — never an empty cell or a strip, never its own colour, one entry per cell, never under the overlay's wall.
  It is applied before the overlay: `boardOf(pic, overlay, recolor)` / `overlaid(pic, overlay, recolor)` (bad entries
  throw, `recolorErrors` lists them), and N, W and the colours count the recoloured board. The exporter writes it as
  `board.recolor` of `round-level.v1` (`[x, y, "C"]`); a level without one has no key and exports byte-identically. The
  game rebuilds the board from the picture with it (`RoundPictureBoard.Of(picture, overlay, recolor)`, the validator's
  picture ↔ board check) and shows the finished picture recoloured (`RoundPicture.Recolored`).
- *Builder accents.* `levels.mjs build --accents auto|on|off` (default `auto`: every Hard and Super Hard attempt, every
  third attempt of a regular Normal; never an introduction or L1–3). `pickAccents(pic, overlay, r, {materials})` picks
  1–2 materials absent from the picture (the colours stay within 10, 6 on a compact picture), ranked by how much they look
  like decorations on their ground (`decorScore`: Солнце / Снег stars on a dark or blue ground, Фуксия / Лаванда flowers
  on green, dark dots on light) and their CIEDE2000 to it (at least 10 to every neighbour, 17 §3), and paints each in 1–3
  clusters of 2–4 cells (`ACCENT`): every cluster cell and its eight neighbours in one colour area of 30+ cells (the
  silhouette never changes), access depth at least 2 and 30 % of the deepest colour cell, 3+ cells from any other accent,
  one ground colour per material. `accentPods` gives each accent material its pods (quota = the accent volume, 1–2 pods)
  and `candidate` (opts `accents`) moves them into the second row to the middle of the tray, so they come up before
  their cubes are reachable; hidden cells never take an accent cell. A picture without room is built without accents.
  The class tests, doses and checks are the same (`evaluate`); the §2 picture fitness reads the picture without the
  recolouring, so accents never make a weak picture strong.
- *Pressure.* `accentCheck` records `checks.accents` (materials, cells, clusters, their pods' rows, «стоят без работы»
  `with` and `without`); `without` plays `accentTwin(level, pic)`: the same queue with the accent cubes back in their
  ground and the accent pods playing it. The level card lists the accents and the difference; the placer prefers more
  pods without work by `quality.json` `pressure.prefer` (soft; the pass/fail bounds stay `min` and `mode`). Candidate ids
  carry `-a` (`P0042-Hlinks-a-5`).
- *Audit.* Each entry valid, the board equal to picture + recolouring + overlay, N, W and the colours of the recoloured
  board.

| File | What |
|---|---|
| `lib.mjs` | paths, manifest, palette, the rules' tables (stages, budgets, calendar, introductions, supported mechanics), franchise rights (`franchiseBlocked`), picture packs (`packOf`, `interleaveByPack`), board around a picture (recolouring, overlay, ring closure, anchors, crossing side), the generator's wall `wallOverlay`, the wave's mechanics plan (`planOf`, `fitsPlan`), PNG writer |
| `themes.json` | the theme catalog's 50 families, 24 directions and meme families (from `product/design/Bloomlings-picture-theme-catalog.md`), for the tag check |
| `pictures.mjs` | sequential numbering, render, rules check, contact sheet, approve/reject/replace |
| `engine.mjs`, `engine/` | the round engine, our own code grown from the pilot: rules, timeline, palette, clock with the §1 slot positions, the §8 link rule, boards up to 48×48, 4–5 places; the pilot v10 embeds the same files |
| `measure.mjs` | saved solution (search + live replay), §5 random test with waiting and Wilson 100 → 400, hidden-pod fairness, the latch rules on the saved solution (`latchCheck`), §11 strategies, §10 rhythm, the bridge measures (`bridgeChoicesWin` with a fresh solver per bridge, `bridgeChoices`, `bridgeSingle`, `bridgeLoss`) |
| `rounds.mjs` | the wave runner: `plan` (positions and picture order), `run` (resumable, parallel build of what is open, checks, placement, audit, summary, the owner's sample pilot), `review` (owner results vs metrics, notes draft) |
| `screen.mjs` | the screen of pictures before the contact sheet: §2 rules, strength, quick queue test, readability, similarity to approved, motifs |
| `svg2cells.mjs`, `svg-palette.mjs` | flat SVG drafts in the palette → cell pictures sized to a target cell count, small patches folded |
| `audit.mjs` | every placed level recomputed against 17 (layer A) with the layer B notes of `content/rounds/quality.json` |
| `analysis/` | one-off measures behind the pilot reviews (opening + colours on CF models, forced decisions, slot pressure) |
| `export.mjs` | spec 006 transfer: entry checks (placed, approved pictures, the owner's confirmation, audit without НЕТ but a named exception), then `round-level.v1`, `round-picture.v1`, `round-proof.v1`, `round-conformance.v1` and `export.json`, checked against the schemas of `specs/006-round-levels-in-game/contracts/` |
| `conformance.mjs` | the reference side of the conformance gate: canonical event log, extended signature, the §5 random policy game by game, owner attempts at whole µs; engine fingerprints |
| `schema.mjs` | a dependency-free checker for the keywords the round schemas use |
| `transfer-log.mjs` | `content/game/rounds/transfers.jsonl`: exported → validated → published → confirmed |
| `tune.mjs` | the generator's queue tuning (2026-11-02): model plan, ≤ 5 quick tests on the blend, forced-aware link chooser, information overlays after the class, the slot's filters (`tuneJob`) |
| `wave.mjs` | the picture-first wave: size mix, even spread, packs, pins, breathers, replacements (pure functions) |
| `levels.mjs` | §3 pod split, candidates (queue by access depth, doses of §7; a scene's gate and lean; accents of §6), `evaluate` (the checks of one candidate, shared with the scene search), build → bank, calendar placement with the §11 variety rules and the plan's bridge kind, cards |
| `bridge-scene.mjs` | split pictures (`splitOf`, `splitsFor`, `checkSplit`, `make --mode split`, `preview`) and the older bridge scenes: `scene` (a smaller picture behind a wall with two sites, strips and beds), `stripsOver`, `checkScene`, the search `make` → a draft picture module with the evidence |

The engine plays hidden pods, linked pairs, the bridge, hidden cells, the latch with its acorn key, the water channel and
the wooden shutters (the last two since 2026-11-05, rules and game port only: the builder, the pilot and the look come
next). Repair, bud, seed, well and stacks need engine and pilot work first (`SUPPORTED` in `lib.mjs` lists what the
builder builds). The search behind the saved
solution assumes each action's work settles before the next. A search that runs out of budget means "not checked",
never "impossible".
