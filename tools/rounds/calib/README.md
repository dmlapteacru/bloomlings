# Queue calibration: placing pods by a formula (2026-11-01)

The owner asked: «Can we place pods by a formula instead of randomly, land right in the right difficulty range, and then
add mechanics and tweaks where needed?» The bank cannot answer this alone. It keeps only accepted candidates near the
target, and over 506 bank candidates the correlation between `blend` and the share is 0.11. So this folder runs a
calibration experiment over the whole range of queue parameters, fits a share model to it, inverts the model into a
builder and checks that builder on placed levels.

**Experiment only.** Nothing here changes the engine, the rules, `levels.mjs` or the content. Pictures are read from a
private copy (`content/work/calib/rounds-copy`, `ROUNDS_DIR`), and every output goes to `content/work/calib/`
(gitignored).

## How to rerun

```sh
cp -r content/rounds content/work/calib/rounds-copy               # once: the private copy
node tools/rounds/calib/experiment.mjs pictures                   # 30 pictures + features → raw/pictures.json
for i in 0 1 2; do node tools/rounds/calib/experiment.mjs run $i/3 & done   # 2205 rows, ~100 min on 3 processes (resumable)
node tools/rounds/calib/fit.mjs                                   # model.json, fit.md, fit.json (5 s)
node tools/rounds/calib/plan.mjs plan P0078 Hard links+hidden      # the planned parameters for one picture
CALIB_MINUTES=15 CALIB_WANT=2 node tools/rounds/calib/plan.mjs validate L0020 L0025   # the formula builder on validation jobs
node tools/rounds/calib/plan.mjs today L0032 L0044 L0036           # today's builder on the copy, for the time comparison
node tools/rounds/calib/plan.mjs report                           # validation.md + preview/L1101… (odd: placed, even: formula)
node tools/gameplay-pilot-v12/build.mjs --levels 1101,…,1116 --preview content/work/calib/preview --out content/work/calib/pilot --name calib
```

| File | What it does |
|---|---|
| `queue.mjs` | The picture's features, computed once: N, colours, spread of colour volumes, the access-depth profile (fitness.mjs plus each colour's first and mean depth), key holes. Also a queue built from **explicit parameters**: blend `t` (0 = access order, 1 = reverse), noise `sigma`, `cols`, pod count `P` inside the §3 budget, the §6 holding pod, links (count, `similar` or `trap` style), hidden pods (count, spread or run), hidden cells (one chunk, share of N), latch, accents. |
| `experiment.mjs` | Picks the pictures (free, approved, no `#`/`b`, stratified by size and strength). Runs the fractional design (below) and measures each configuration. |
| `model.mjs` | A binomial logistic regression (IRLS with ridge) on the quick share. Three feature sets: `picture`, `params` (controllable) and `structure` (adds the built queue's rank correlation and deep pods on top). |
| `fit.mjs` | Fits all three models, picks each one's ridge strength by cross-validated log loss, validates with pictures held out, measures the per-mechanic effects and writes `fit.md`. |
| `plan.mjs` | `plan()` is the inverse, followed by the correction loop, the Hard mechanic step and the full acceptance (`levels.mjs evaluate` plus the placer's filters of the slot). It also holds the validation jobs, the today's-builder comparison and the preview levels. |

### Design (per picture; 30 pictures, 2205 rows)

- **A (21 rows):** `t` ∈ {0, 1/6, …, 1} × P at the budget's low, middle and high end. `sigma` ∈ {.06, .16, .26}, the column choice and the holding pod rotate through a Latin square.
- **B (15–16 × 3 rows):** at `t` = 1/2, 5/6 and 1, one base queue with a fixed seed, plus overlays laid on that **same** queue:
  - links at 12 / 30 / 50 % of P, each × similar / trap;
  - hidden pods: 2 spread, 4 spread, 4 run;
  - hidden cells: 5 / 10 % of N;
  - a latch, where the picture has a key hole;
  - links 30 % + 2 hidden; trap links 30 % + 5 % cells;
  - accents.

  An effect is therefore a paired difference on one queue.
- **C (6 rows):** `t` = 1/2 and 5/6 × 3 queue seeds at equal parameters. This measures the spread that no formula of the parameters can remove.
- **Measured on every row:** the quick share (50 games of the §5 random policy), the calm share (30 games) and the greedy strategies.
- **Measured on every fourth row and the main B overlays:** the forced share and «стоят без работы» (pressure, 30 games each).

The pictures, by size and strength (fitness.mjs):
- 200–399: 2 weak, 2 mid, 2 strong;
- 400–699: 2 weak, 1 mid;
- 700–999: 3 weak, 3 mid, 4 strong;
- 1000–1400: 2 weak, 3 mid, 6 strong.

Details are in `content/work/calib/raw/pictures.json`. The validation pictures (P0078, P0166, P0060, …) are placed levels, so none of them is in the calibration set: they are held out.

## What the experiment shows

**1. The queue order alone makes almost nothing hard.**

| Pictures (part A) | Median share at t = 0 | ⅓ | ½ | ⅔ | 1 |
|---|---:|---:|---:|---:|---:|
| weak | 100 % | 100 % | 100 % | 100 % | 100 % |
| mid | 100 % | 100 % | 100 % | 100 % | 100 % |
| strong | 100 % | 100 % | 94 % | 74 % | 60 % |

- Over the whole range of `t`, P, columns and sigma, the share stays at 95–100 % on 592 of 675 weak-picture rows and 425 of 660 mid-picture rows.
- Without links, only 13 of the 30 pictures ever drop below 50 %.
- Where a picture does drop, it drops as a **cliff**. For example, P0268 goes 82 % → 62 % → 0 % between t = ⅓ and ⅔, and P0264 goes 100 % → 48 % → 6 %. The cliff sits at a different `t` on each picture.
- With links, the share **is not monotone in t**: one seed on P0060 gave 25 % at t = 0.52, 0 % at 0.27 and 50 % at 0. Changing `t` reorders the pods, which changes which pairs are linked.

**2. Links make the difficulty, above all trap links.** The overlay is laid on the same queue. The table covers pairs whose base is in 5–95 % (37 of 90) and gives the median change in points (`fit.md` has the means and their spread):

| Mechanic | Δ random share | Δ calm share | Δ forced | Greedy "most work now" wins |
|---|---:|---:|---:|---:|
| links 12 % similar / trap | 0 / 0 | 0 / −7 | +10 | 87 → 83 / 76 of 90 |
| links 30 % similar | −14 | −10 | +26 | 87 → 73 |
| links 30 % **trap** | **−32** | **−33** | +29 | 87 → 63 |
| links 50 % similar | −22 | −20 | +30 | 87 → 66 |
| links 50 % **trap** | **−36** (−68 from a 75–95 % base) | **−47** | | 87 → 50 |
| accents (one material) | −4 (mean −7) | −3 | 0 | 87 → 81; «стоят без работы» +0.4 |
| latch (9 informative pairs) | +2 (noisy) | −3 | +3 | 43 → 40 of 45 |
| hidden pods 2 / 4 / 4 in a run | **exactly 0** | **exactly 0** | 0 | no change |
| hidden cells 5 / 10 % | **exactly 0** | **exactly 0** | 0 | no change |

- **The owner's belief about hidden pods and hidden cells holds, in the strongest form.** Under the §5 protocol they change *nothing*. In all 270 + 180 paired rows the result is bit-identical, and the combinations "links + 2 hidden" and "trap links + 5 % cells" equal links alone. This follows from the rules, not from noise:
  - a hidden pod reveals when it reaches the top, and every bot (random, calm, greedy, opening) acts only on tops;
  - a hidden cell reveals exactly when it gets an open reachable neighbour, which is the moment it would become a target anyway.

  So both are pure *information* mechanics. They limit how far a human can plan ahead and stop mindless tapping, but neither the class test nor any of our strategies can see them. Measuring them would take a bot that plans over the visible queue (a lookahead policy). The class they get is the class of the queue under them.
- **Links raise the forced share sharply.** The median forced share is 34 % on rows with links against 8 % without, and 46 % of rows with links exceed the §5 limit of 35 %. Across Hard attempts this is the most common pre-screen refusal (see the validation reasons).
- **Calm share follows the random share:**

  | Random share | Median calm share | Calm ≤ 50 % | Greedy wins |
  |---|---:|---:|---:|
  | 20–50 % | 40 % | 65 % of rows | 74 % |
  | 5–20 % | 10 % | 95 % of rows | 62 % |

  So the Hard filters (calm ≤ 50 %, greedy loses, opening) cut about half of the queues that are already in the Hard band.
- **«Стоят без работы» falls as the share rises:** median 1.47 at 20–50 %, 1.18 at 50–75 %, 0.88 at 75–95 %, 0.24 at 95–100 %. Accents add +0.4 at almost no cost in share.

## The model and its accuracy

The model is `logit(share) = β·x`, with these terms:
- 14 picture features;
- the queue parameters `t`, `t²`, sigma, cols, cells per pod and hold;
- the mechanic doses: link share, link share × trap style, hidden, hidden run, cells, latch, accents;
- 17 interactions: `t` with hidden-colour share, open share, layers, log N, visible colours, deep work, first depth, shallow share, sigma, cells per pod, cols, link share, trap, hidden and cells; `t²` with hidden-colour share and open share.

Every share below is the measured quick share (50 games). Accuracy is measured with leave-pictures-out cross-validation: 5 folds, and a picture is never seen in training.

| Model | R² in sample | R² CV | CV median / 80th pct abs. error | Same, inside the 5–95 % band (742 rows) | Class hit (CV) |
|---|---:|---:|---:|---:|---:|
| picture only | 0.45 | 0.08 | 12.8 / 37.0 pts | 21.8 / 43.9 pts | 76 % |
| **params** (used by `plan`) | 0.77 | **0.33** | 6.9 / 30.0 pts | **19.2 / 38.6 pts** | 80 % |
| structure (+ rank correlation, deep pods on top) | 0.83 | 0.48 | 4.2 / 27.1 pts | 19.4 / 38.2 pts | 77 % |

The whole-sample errors look small only because most rows sit at 100 %. **Inside the band where a class is decided, the formula misses by a median of 19 points and by 39 points at the 80th percentile.** It cannot land a level in a 30-point zone without measuring.

Two noise floors limit any formula:
- the binomial noise of 50 games, about 5 points median at 50 %;
- the spread between queue seeds at equal parameters (part C, inside the band): **11.4 points median, 24.5 at the 80th percentile**.

Most of the remaining error is that seed spread: the share depends on the realized queue (which pods end up next to which), not only on the parameters.

One quick measurement on the picture helps a lot. The params model is trained without the picture, then gets a per-picture logit offset from k measured configurations:

| k | Median / 80th pct abs. error in the band | Class hit |
|---:|---:|---:|
| 0 | 19.2 / 38.4 pts | 63 % |
| 1 | 16.6 / 37.3 pts | 69 % |
| 2 | 13.6 / 29.9 pts | 70 % |
| 3 | 11.9 / 27.6 pts | 70 % |

Three measurements bring it to the seed-spread floor.

The model's largest terms in `fit.md` read as expected:
- `t` itself and `t × visible`: visible colours keep the share high;
- `t × log N` and `t × shallow share`: big and shallow pictures fall faster with `t`;
- `t × cols`: more columns make a reverse order harder;
- the colour-volume spread.

## The formula builder (`plan.mjs`)

1. **Plan.** Over a grid (`t` in steps of 0.02, P low / mid / high, the allowed columns, sigma .06 / .12), with the recipe's doses fixed by the rules (the lead mechanic at its §7 dose place, an additional one at its minimum; links trap-style on Hard), pick the parameter set with the highest predicted probability of landing in the zone. The probability uses the model's CV residual spread on the logit scale, 1.9. The zones are:
   - regular Normal: 50–75 %, aiming at 58 %;
   - first Normal after a Hard: 75–95 %;
   - Hard: 20–50 %, aiming at 33 %;
   - Super Hard: 5–20 %, aiming at 12 %.
2. **Correct (≤ 5 quick measurements, 40 games each).** Take Newton steps on `t` with the model's slope until both sides of the zone have been seen, then bisect inside that bracket. A per-picture logit offset is learned along the way. Once an attempt has measured in the zone, the next attempts keep its pods, columns and `t` (± 0.03).
   - The first version used ≤ 3 Newton steps only. On Hard it oscillated across the cliff (100 % ↔ 0 %), and in 15 minutes on L20 not one of 35 attempts reached the full test (log in `content/work/calib/validation/v1/`).
3. **Mechanic step for Hard and Super Hard.** This is the route-first prototype's: the links are chosen one pair at a time so that "most work now" gets least far, with a trap pair first. The calibration says trap links are what makes Hard, so this is the step. Unlike route-first, no route is kept: winnability is the solver's job in `evaluate`.
4. **Pre-screens and acceptance.**
   - Cheap pre-screens: forced ≤ 35 %; on Hard, also greedy loses, 40 calm games, and the opening test.
   - Then `levels.mjs evaluate` and the placer's filters of the slot (route-first's `placerFilters`), and the zone.

## Validation

Each builder gets one picture, class and recipe, the slot's filters and zone, and asks for 2 levels within a 15-minute
budget. The full table with every check is `content/work/calib/validation.md`.
- **Today:** `levels.mjs build` with its 64-candidate budget. The numbers are from `content/work/proto-route/out/*-old.json`, or run here on the copy for L32, L44 and L36.
- **Route-first:** `content/work/proto-route/out/*-proto.json`. It was run only on L20, L25 and L27.

**Time.** "per level" is the total time divided by the levels accepted (in the zone and passing every filter).

| Job | Today's builder | Route-first | **Formula + correction** |
|---|---|---|---|
| L20 P0078 Hard links+hidden | 0 of 64 in 306 s | 2 in 806 s: 403 s/level, first at 558 s | 1 in 962 s; first at attempt 24, 466 s |
| L25 P0166 Hard links+hidden | 0 of 54 in 177 s | 2 in 438 s: 219 s/level, first at 152 s | 2 in 841 s: 420 s/level, **first at attempt 4, 101 s** |
| L40 P0165 Hard links+hidden | 0 of 64 in 236 s | not run | **0 in 937 s** (19 of 43 attempts refused on forced > 35 %, 20 never measured in the zone) |
| L27 P0060 Normal links+hidden | 1 in 245 s | 2 in 893 s: 446 s/level, first at 371 s | 2 in 787 s: 394 s/level, first at attempt 3, 138 s |
| L22 P0015 Normal none | 2 in 94 s: 47 s/level | — | 2 in 130 s: 65 s/level, first at 89 s |
| L32 P0012 Normal links+hidden | 1 in 209 s (4 found, 3 above 75 %) | — | 2 in 262 s: 131 s/level, first at 61 s |
| L44 P0164 Normal links+hidden | 1 in 131 s (4 found, 3 above 75 %) | — | 2 in 255 s: 128 s/level, first at 223 s |
| L36 P0079 Normal none (322 cells) | 0 in zone (4 found at 77–84 %, placed at 81 %) | — | 2 in 18 s: 9 s/level (71 %, 69 %) |

**Checks of the first accepted formula level.** The measured class, its random share (Wilson, games), calm and forced
shares, greedy, «стоят без работы» and links (traps / links), each next to the placed level:

| Job | Placed level | Formula level |
|---|---|---|
| L20 Hard | 31 % (23–41, 100); forced 34 %; pressure 1.5; 8 links | **29 % (21–39, 100)**; calm 19 %; forced 28 %; opening 0/2, 25 %; greedy loses; pressure 1.84; 8 links (4/4 traps); accents 4 |
| L25 Hard | 33 % (25–43, 100); calm 49 %; forced 30 % | **25 % (21–30, 400)**; calm 23 %; forced 22 %; opening 0/2, 14 %; greedy loses; pressure 1.79; 8 links (4/4); accents 4 |
| L27 Normal | 60 % (55–64, 400); calm 58 %; pressure 1.37 | 63 % (53–72, 100); calm 61 %; forced 19 %; pressure 0.99; 6 links (1/3) |
| L22 Normal | 55 % (50–60, 400); pressure 1.67 | 70 % (60–78, 100); calm 62 %; forced 6 %; pressure 1.45 |
| L32 Normal | 77 % (68–84, 100); calm 89 %; forced 34 % | 69 % (59–77, 100); calm 76 %; forced 11 %; pressure 0.88 |
| L44 Normal | 56 % (51–61, 400); forced 25 % | 61 % (56–66, 400); calm 56 %; forced 32 %; pressure 1.09; 6 links (3/3) |
| L36 Normal | 81 % (72–88, 100); pressure 1.76 | 71 % (62–79, 100); calm 71 %; forced 8 %; pressure 1.07 |

Overall:
- Every accepted formula level passed `levels.mjs evaluate`: saved solution, class, opening test, calm test, forced share, fairness, doses. It also passed the placer's filters of its slot (holding pod, trap link, calm rule).
- The formula found a Hard on two of the three Hard pictures where today's builder found none. That puts it at the route-first prototype's level: faster to the first level on L25 (101 s against 152 s) and on L20 (466 s against 558 s), slower per level on L25 (420 s against 219 s).
- Neither found anything on L40.
- On Normal, the formula reached the first level in zone in 9–223 s, and its levels sit closer to the 50–75 % zone than today's (L32, L36).
- The pilot `content/work/calib/pilot/` (`Bloomlings-pilot-calib.zip`) has 15 levels: odd L1101–L1115 are the placed level, even ones are the formula's on the same picture (L1108, for L40, has none).

## Limits

- **Measurement protocol:**
  - The response is the quick share of 50 games, a noisy proxy for the §5 test (100–400 games). Accepted levels are always re-measured by `evaluate`.
  - The calm share is from 30 games; forced share and pressure come from a subsample.
- **Hidden pods and hidden cells are invisible to every bot we have.** Their zero effect is a property of the protocol, not proof that a human finds them no harder. Measuring that needs a lookahead policy that reads the visible queue, or the owner's playtests.
- **Mechanics covered:** the latch has few informative pairs (9 of 45; 15 of the 30 pictures have a key hole, most of them weak pictures that stay near 100 %). Bridges are not covered: no `#`/`b` pictures, by design.
- **The share is not a smooth function of the parameters.**
  - It is a cliff in `t`, and with links it is not even monotone.
  - A logistic model on the parameters cannot place a level in a 30-point zone (19 points median miss).
  - The structure features (rank correlation, deep pods on top) help in the whole sample but not inside the band. Better predictors would need a short simulation (one greedy game, a few calm games), and that amounts to measuring.
- **Experiment design:**
  - Accents were one material, and the overlay was not exactly paired: the pods change with the recolouring.
  - Hold, sigma and columns rotate in a Latin square, so their effects are estimated with less precision.
- **Comparison caveats:** the comparison numbers for today's builder and route-first come from earlier runs (`content/work/proto-route/out/`) on the same machine type, not from one shared run. Today's builder on L32, L44 and L36 was run here on the copy.

## Recommendation

**Can we place pods by a formula and land right in the range?** Not from the picture and the parameters alone. The
share is a cliff in the blend `t`, at a different place on every picture, and it is not monotone once links are
involved. Queues with equal parameters differ by 11 points (median) in the band that matters. The best model misses by a
median 19 points there, which is more than half a class zone. A closed-form formula is the wrong investment: more
picture features will not fix it, because the missing information is in the realized queue.

**What works is formula → measure → bisect.**
- **The formula:** the model gives the starting `t`, P and columns, and the slope.
- **Measure:** 40-game quick tests, each a few seconds and mostly faster on losing queues.
- **Bisect:** inside the bracket once both sides of the zone have been seen. The picture also remembers where it landed.

Then the mechanics follow:
- **Links** are the difficulty knob. Trap links at a 30 % dose cost about 32 points of share and 33 of calm share. For Hard, choose the pairs against the greedy strategy (route-first's step).
- **Hidden pods and hidden cells** are information overlays that the class test cannot see. Add them after the class is set, for their own checks (fairness, «влияет»); they never need the class re-measured.
- **Accents** buy pressure (+0.4 «стоят без работы») for about 4 points of share.

On the validation set, this already finds Hard where today's builder finds none, about as fast as route-first, and finds
Normals in the zone sooner.

Next steps, in order of value:
1. **Port the correction loop into `levels.mjs build`.** Today a regular Normal steers `t` with a shrinking step (`ROUNDS_STEER`), and Hard and Super Hard draw `t` uniformly. Replace both with the bracket bisection and the per-picture memory, plus greedy-chosen trap links on Hard. The candidate code and `evaluate` stay as they are.
2. **Make the link chooser respect the forced share.** Links lift the median forced share to 34 %, and 46 % of queues with links break the 35 % limit. This was the main reason Hard attempts died (L40: 19 of 43; L25: 16 of 39). Prefer pairs whose partners do not leave a single launch available.
3. **Do not count on hidden pods or cells to move a level between classes.** If the owner wants their human difficulty measured, add a lookahead policy (or use playtests); the §5 bot is blind to them by construction.
