# V9 design review: pictures, decisions and waiting

Review date: 2026-10-24. Baseline: `c6b0bfd467e4fb2ae28bc1b472a085530e8f1b9a`, current `content/pilot-v8e` catalog hash `77a54762c1b39ed7d872e6646dc8e4e65070596e847afd1f08772af24d9abd30`. This is an independent design recommendation for a separate browser pilot. It does not change the production game, old pilots, rules or content.

**Recommendation:** keep the successful v8E level identities and three-row information model. Improve a few weak rounds by changing where color becomes reachable and how its quota is packaged. Measure the time during which the player has no useful next action. Do not create the requested round length by slowing couriers or parking four of five places for minutes.

## Evidence and confidence

- **Observed, high confidence within the recordings:** the owner’s transcribed tap order, linked-lot occupancy and key outcomes in [CF reconstructed rounds](colony-flow-rounds/README.md). L20 tap timestamps are reconstructed to about ±0.5 seconds; L25/L26 use half-second frame sampling. These are individual recorded attempts, not population averages.
- **Exact static accounting:** CF L1–L4, L10 and L20. L5 has one pink/light-pink cell mismatch; L25 has 1,024 recognized cells but 1,026 units in capsules; L26 has 37 hidden board cells whose aggregate colors are known but locations are not. Exact accounting is necessary, but does not identify the original simulation rules.
- **Observed human opinion:** the [v8C owner review](../playtests/pilot-v8c-owner-2026-10-22/README.md) and [v8E playtest](pilot-v8e-playtest-2026-10-24.md). One experienced player, almost entirely at ×2. The v8E report describes a catalog before the final art, three-row and queue revisions; it is not a test of current hash `77a54762…`.
- **Our simulation:** current v8E rules, timeline, certificates and authoring metrics. Solving a CF reconstruction in that engine demonstrates a solution under stated assumptions, not an exact replay of Colony Flow.
- **Design hypotheses:** proposals and threshold targets below. No D1/D7 retention data, new-player cohort or controlled comparison is available. An increase in “interesting” ratings cannot establish a retention increase.

I visually inspected current v8E `art-all.png`, `cubes-all.png`, the CF Normal/Hard contact sheets and reconstructed CF L25/L26 frames. Contact-sheet labels and site pictures are secondary evidence: for example, the sheet’s purple castle without a filled sky differs from the recorded L10’s orange/yellow castle with sky. Use the recorded reconstruction for gameplay accounting.

## What Colony Flow actually combines

Its useful design unit is a **color front plus a commitment**, not a board size or a number of colors. A lot can clear some visible material yet keep occupying a place because its remaining quota lies behind another color. The next lot must both make progress and preserve access to the future lot that releases that commitment.

1. **Recognizable silhouette, broad material regions.** The star, heart and apple can be understood before play. Later objects use a continuous outline, a few large surfaces, small diagnostic details and a quiet background. The six-color CF L25 kitten is more legible than its color count suggests: large dark head, white eye surrounds, green eyes, red strawberry, pink accents and sky. Color count alone does not explain readability or difficulty.
2. **Background as available work.** CF L20 contains 500 pale-pink cells (38.6%). A geometric flood from the outside can clear 459 of them without crossing another color; 41 are separated highlights/interior pockets. L25 has 467 recognized sky cells (45.6%); 466 connect to the outside through sky alone. Their background can sustain progress while a small interior color holds a place.
3. **Quota often exceeds the currently reachable part.** CF L10 has 243 sky units, but only 237 are initially connected through sky. Its dark-green 8 and yellow 30 can wait behind other colors. CF L26 exposes eight colors on its outer frontier, but a single-color flood initially reaches only 2 brown cells and no blue jar cells. Thus “I see brown” is insufficient to justify a brown-25 lot.
4. **Queue order makes the commitment matter.** CF L25 begins with white-28/white-51 linked, dark-30 and red-30; sky is one level down under dark/red. To reach abundant sky, the player first parks a lot whose immediate work is absent. This is a legible investment when the sky below is visible.
5. **Hidden means unknown future, then visible choice.** CF depth mysteries reveal both color and count when they reach the top. Current v8E has the same semantics. Returning to reveal-on-commit would introduce a materially different risk. L26’s hidden *board cells* are a separate mechanism and should not be confused with hidden capsules.
6. **Links increase commitment size.** With five places, a two-lot link costs 40% of capacity. Two simultaneously stalled links leave one place to advance the entire board. CF has side-by-side, different-depth and same-column links; v8E only has same-depth pairs in different columns. Mechanical variety is real, but copying every link form is unnecessary for v9’s immediate goals.

The initial outside-accessible colors vary greatly: CF L1 only orange, L3 only outline, L5 only purple; CF L4 has three frontier colors; L10 five; L20 two; L25 one; L26 eight. “More open colors” is therefore not a universal Normal/Hard rule. The relation between the front, reachable volume and queue is what matters.

## Recorded durations: the main problem is inside the round

Calculated directly from `colony-flow-rounds/levels.json`. The L25 winning attempt starts at recording second 83: its duration is **226.5 seconds**, not 309.5 seconds. “Tail” here means the interval from the final recorded tap to the recorded round end; its endpoint is not perfectly uniform across recordings (L20 uses the victory screen, L25/L26 use completion).

| Recorded successful attempt | Duration | Taps | Last-tap tail | Median intertap interval | Longest intertap interval |
|---|---:|---:|---:|---:|---:|
| CF L20 Super Hard | 397.75 s | 36 | 9.75 s / 2.45% | 10.75 s | 31.75 s |
| CF L25 Hard, attempt 2 | 226.5 s | 26 | 6 s / 2.65% | 9.5 s | 33.5 s |
| CF L26 Normal | 315.5 s | 32 | 6 s / 1.90% | 9.5 s | 30 s |

The gaps cannot by themselves separate thinking, voluntary waiting, forced waiting and attention outside the game. The annotated slot events provide stronger evidence:

- **CF L20:** the prior video audit identifies about 180 seconds with only one useful place while two links hold the other four. The green-8 and dark/purple pair are long commitments. Different source notes disagree on the green lot’s exact wait length (about 250 seconds versus roughly five minutes), so do not turn that number into a precise benchmark.
- **CF L25:** the white pair occupies two places from recording second 102.5 until completion at 309.5: **207 seconds, about 91% of the winning attempt**. Its color quota covers external sparkles, facial details and seeds that open much later. The dark/green pair then occupies two more places from 137 to 233.5; its dark half is done at 184, its green starts at 213. This makes the one-free-place bottleneck understandable, but unnecessarily long.
- **CF L26 Normal:** blue-25 has no reachable work for 31 seconds, then finishes in about eight. One brown-25 remains active from 132.5 almost to the end. An explicitly annotated nine-second stall occurs at 205–214 despite three occupied places and apparent lack of helpful tops.

CF’s small final tail is not evidence of a lively middle. Copy the readable commitments and eventual release; shorten the interval before their payoff. Longer rounds are not automatically better for retention. Monetization explanations for CF’s duration are hypotheses; the recording does not establish their design intent, and speed state is not uniformly known.

## Defensible solutions of the reconstructed boards

I independently adapted a temporary copy of v8E’s patient live model. Changes: map CF color letters into the existing alphabet, add a ring of open ground, and permit a same-column linked capsule immediately below its linked partner to join the tap. The latter is one explicit admission-rule patch, consistent with the previous CF-v8D audit. Board `#` is a CF color, not a wall. No timing constants were fitted to CF. Each command is allowed to settle fully before the next.

The existing three-row planner (`rows=3, depth=4, weight=12`) solved L1, L2, L3, L4, L10 and L20. Four rows also solved each. These column sequences are reproducible witnesses in the adapted model; a linked tap takes the whole eligible link:

| CF level | Winning columns, one-based; wait for the model’s work to settle between taps |
|---|---|
| L1 | 1,1,2,1,2,2,3,3,3 |
| L2 | 1,1,2,2,1,2,3,3,3 |
| L3 | 1,1,1,2,1,2,2,2,3,3,3,3 |
| L4 | 1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,1,2,3,3,3,3,3,3 |
| L10 | 1,1,1,1,1,1,2,2,2,1,1,1,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3 |
| L20 | 1,2,3,2,1,3,1,1,3,1,2,3,2,2,2,2,2,3,2,1,1,1,3,3,2,1,3,4,3,3,4,4,4,4,4,4 |

The owner’s recorded **36-column L20 order also wins** when replayed patiently in this adapter. That is independent support for the reconstruction and general causal model. It is not confirmation that the recorded tap times, allocation of same-color work or intermediate occupancies match CF.

For L5, a hypothetical single-cell change `l → P` at zero-based `(14,11)` balances the quotas. The same planner then wins with columns `2,2,1,3,1,3,2,2,2,2,1,1,1,1,3,3,3,3`. This correction selects an adjacent pink boundary pixel; it has not been verified against a clearer original frame. Label the result **a solved normalized hypothesis**, not an exact solution of the uncorrected image.

Do not certify exact L25/L26 model replays from current data. For L25, assigning two extra sky cells or removing two sky quota units changes the input. For L26, allocating known color totals among 37 unknown positions invents connectivity and reveal order. Their recorded wins and their queue legality remain useful evidence. The original CF L10 failure/win comparison is particularly strong: a free place at tap 16 can take orange-30, which opens the route toward white-49, or pink-25, which jams. Its first fifteen lots are broadly the same investment set in different orders; the critical distinction is readable when the choice arrives.

Temporary analysis was performed in `/tmp/astra_v9_metrics.py` and `/tmp/astra_cf_replay*.mjs`; no source datasets were changed. The inputs, adapter description and full column witnesses above preserve the substantive result without relying on temporary files.

## What changed across v8, v8C, v8D and current v8E

The older audits should be read as a sequence, not as simultaneously current facts:

- v8’s bot audit found easy Normals and harsh early Hard traps, but also several enjoyable difficult levels. It already warned that bot wins are diagnostic rather than human win rates.
- v8C introduced roles and reveal-at-top mysteries. Its owner test exposed the whole-queue visibility problem: human planning could trivialize a level that a top-only bot found hard. Five rounds were rated boring; the tail consumed 23% of winning time.
- v8D restricted visibility and checked a visible-horizon planner, removed ordinary capsule icons and accelerated the tail. Its four-row certificates cannot be reused as three-row evidence.
- v8E replaced artwork, introduced more colors and updated paths. The owner test contains seven “interesting” levels and three “boring” ones, but it is one player and a different build. Current v8E subsequently changed L1/L2/L3/L11, made exactly three rows visible, increased L10 mysteries to five, moved the tail to ×2.5 and regenerated queues. It has not received the same human test.

Current **L1 tree, L2 flower, L3 ice cream and L11 grapes must not be judged as the old cactus/pig/cupcake/turtle**. The replacement icons are readable in the inspected cube sheet. The six-color grapes use dark outlines and purple masses well; the owner’s “cannot recognize the turtle” criticism is already addressed.

Current work budgets, calculated from capsules rather than bounding-box area:

| Role | Median colored cells | Median colors | Largest capsule in the role |
|---|---:|---:|---:|
| Tutorial | 121 | 4 | 38 |
| Relief | 242 | 7 | 55 |
| Normal | 550 | 11 | 116 |
| Normal with a task | 696 | 11.5 | 121 |
| Hard | 840 | 13 | 122 |
| Super Hard | 924 | 14 | 174 |

The nominal `34×36` includes fence/path cells: L22/L29 have 924 units of colored work, versus 1,296 on CF L20. Do not compare their nominal board area as if all cells were work.

Current v8E is **already more colorful than the small reconstructed CF sample**. Many images have 13–15 colors, several of them used for tiny flowers, highlights or decorative particles. Adding colors is not the next quality step. CF L25 gets a clear subject and meaningful capacity pressure from six colors.

The quota distribution is a bigger actionable difference. CF L20/L25/L26 have median capsule sizes 30/34/35 and maxima 38/51/47. Current v8E uses large background lots beside tiny decorative lots: L8 121, L10 154, L12 106, L13 102, L17/L20 116, L25 122, L27 118, L29 174. At one departure every 250 ms, an immediately available 174-unit lot needs **at least 43.25 seconds between first and last dispatch**. It may overlap other useful play, but cannot itself free faster. This is a mechanical reason to investigate long low-concurrency stretches, independent of subjective difficulty.

Two measurement defects must be fixed in v9 before changing duration targets:

1. v8E `fix.mjs` still divides the final tail by **4**, while actual `TAIL.speed` is **2.5**. Its advertised 1× duration is therefore understated for that portion.
2. If the brisk action schedule fails, `secondsAt1x` falls back to a scaled patient duration (`patient × .25 / .6`). Current L18 uses this fallback. It is not a completed live replay or a proven human-time estimate.

## Concrete v9 scope

Preserve all previous pilots and their saves, with a separate v9 catalog and browser page. Keep exact v8E level definitions for the human-supported anchors L15, L17, L20, L21, L22, L27 and L29 unless the new timing evidence identifies a specific defect worth testing separately. The report’s positive judgments identify subjects and puzzle concepts, not proof that the latest regenerated queue is unchanged or already validated by humans.

| Level | Recommendation | Specific experiment and intended effect |
|---|---|---|
| L1–3, L6, L11 | Keep current icons | Already revised after the old test. Test recognition without titles; do not redraw them based on obsolete feedback. |
| L4 | Smooth tutorial step | Current work jumps from L3’s 136 cells/12 lots to 440/18. Aim roughly 280–340 colored cells and 14–16 lots, retain the butterfly’s two symmetric wings and six-color vocabulary. Teach an outer/inner remainder once, with prompt release. These are authoring targets, not promises of a particular duration. |
| L5 | Keep Normal | Current 366-cell acorn, 44% random / 75% visible-color bots, accepted as normal/okay by the owner. No strong evidence supports changing its role to Hard merely to copy CF’s numbering. It is a useful first capacity lesson. |
| L12 | Highest-priority gameplay upgrade | Night color N is 326/624 cells (52.2%), split into 93/27/100/106. Add a second visible dependency around the owl face/body after the first outline phase. Split oversized quota only if the packets become available at different useful phases. One purposeful hidden key lot or short-lived pair can support this, but adding a symbol alone is not a fix. Keep the owl silhouette and eye contrast. |
| L13 | Upgrade fronts and simplify decoration | Sky B is 270/572 (47.2%); there are 13 colors and one pair. Preserve the pink canopy and hooked handle, reduce irrelevant flower/rain shades, and give canopy clearance a second consequence for the handle or lower front. Target two understandable choices separated in board progress, not repeated taps through sky. |
| L16 | Improve the relief payoff | Only 146 colored cells and 12 lots; the earlier owner run was 12 seconds and boring. Enlarge the kite’s colored body/diagonal panels to roughly 200–260 cells, keep a clear tail, use roughly 12–14 moderate lots and one modest order dependency. A short satisfying relief round is valid; do not require Hard-like danger. |
| L18 | Measure first | Its published 122-second duration is a failed-brisk-replay fallback. Obtain a valid timed winning schedule before using duration to accept or redesign it. |
| L25 | Keep as Hard control initially | Current kitten reads correctly; current visible-color rate is 28% versus random 22%. Inspect exact linked-pair waiting and large sky-122 before changing it. If a long forced bottleneck is verified, shorten the link’s dependency distance or split the large lot across useful phases. |
| L30 | Keep readable finale initially | The bunny is legible, and a short finishing round can be a reward. Only enlarge or requeue if direct timing/ratings identify a weak payoff; its short duration alone is insufficient. |
| L10 / L22 / L29 | Preserve distinct difficulty concepts | L10 information/reveal; L22 linked commitments plus crossing choice; L29 quota/route planning. Verify current timed traces before rebalancing. More hidden lots or lower random win rate alone does not prove more meaningful decisions. |

Do not add same-column links, different-depth links, hidden board blocks, locks and new reward systems in the same experiment. The reconstructed examples justify future prototypes, but the immediate question can be answered using existing pairs, mystery lots and front layout. This keeps the effect of changed content reviewable.

The v9 implementation plan additionally selects **L10 and L25 as queue-only experiments**, retaining their pictures and keeping the other named anchors as exact controls. This is reasonable given the 154/122-unit maxima and the owner’s earlier “normal” difficulty ratings. A 60-unit maximum on these two levels is an experimental authoring constraint, not a global rule. Keep the old definitions available for comparison.

For these two replacements, require a valid brisk live win as well as the three-row patient witness; inspect at least two capacity decisions separated in board progress; preserve reveal-at-top mysteries; and avoid a link whose release depends on the final few decorative cells. L10 should keep its information/reveal identity rather than become a string of concealed compulsory investments. Test candidate bot rates on a held-out random seed after selection: searching and validating against the same 300 trials can fit incidental outcomes. Keep the existing early-error safeguard, but report exhausted searches separately: current `earlyRisk()` counts an unresolved `winnable()` result as winnable, so a reported low risk is optimistic unless all relevant branches were resolved.

## Pacing and decision acceptance for the new pilot

The existing bot corridors remain useful guardrails. They need a second axis measuring the *experience along a successful route*. All times below must use a valid live trace, actual ×1/×2/×2.5 changes and explicit assumed player reaction time.

Record total wall time; time to final accepted command; post-command tail; forced no-input intervals; intervals with legal commands; number of places occupied; active pods with pending productive outbound work; blocked unsent quota; completed halves held by a link; and remaining queue length. Returning couriers and a zero-quota linked half do not count as productive places.

Report two different low-concurrency situations:

- one or two productive places **with a real available choice**: potentially useful tension;
- one or two productive places **with no legal command**: forced waiting.

A low worker count or a high occupancy count alone is not evidence of a bad puzzle. Conversely, “there is a legal tap” does not establish a useful decision: it may be an obvious fatal lot. Mark provable outcomes separately from heuristic assessments and retain `unknown` when search is budget-limited.

Provisional review targets, to test rather than present as established player preferences:

- Normal after tutorials: at least one mid-round choice whose alternatives produce visibly different commitments, then a release payoff. Relief may be easier. Aim to keep a continuous forced no-input stretch below roughly 10–12 seconds at ×1.
- Hard: at least two separated choices, including one after the opening. An early safe approach should reveal the puzzle before failure becomes possible. Investigate forced no-input runs above about 15–20 seconds.
- Super Hard: multiple phases or decisions with different reasons, such as preserving a second place before a reveal and choosing a crossing later. Investigate forced no-input runs above 20–25 seconds. The requested longer round should come from more meaningful fronts, not a single oversized background commitment.
- Final automatic tail: use actual ×2.5 and prioritize roughly 6–10 seconds or less where practical. The CF records show that brief tails coexist with long rounds; they do not justify forcing the whole round to be long.

These are review triggers, not universal hard rejection rules. Human-supported anchors should remain available as controls even if they exceed a provisional limit. Avoid optimizing only one patient certificate: replay a plausible brisk schedule and at least one alternate successful strategy. A patient certificate can be sound while briskly reusing its action order loses because live allocation and slot release differ.

For changed lots, a useful authoring target is “most of this lot works now; the small remainder releases after the next one or two relevant colors.” Avoid pairing a large total-color quota that includes end-of-round details with a deep interior quota: CF L25’s white pair demonstrates why the commitment then lasts almost the entire round. Moderate packets around 20–50 are a sensible starting range on larger pictures, with intentional exceptions; not every color must receive the same count, and blindly splitting a 120 into three consecutive 40s merely adds taps.

For mysteries, keep reveal-at-top and three visible rows. A successful horizon-planner route shows existence of a usable strategy in the authored world. It is not a proof that every observationally identical hidden arrangement is safe. Existing hidden-swap choice-invariance checks are useful for detecting information leaks, but do not establish universal fairness. Where a mystery becomes decisive, inspect the visible escape/space before reveal and test plausible alternate hidden contents without requiring the player to guess the color. Do not punish the exploratory tap before the revealing information could be obtained.

## Readability and retention test

Use actual phone-size cube renders with titles hidden. Ask for the object name before showing the answer; inspect the subject at start and after roughly 25%, 50% and 75% of work. A picture that only reads in a large contact sheet is not sufficient. Preserve a few diagnostic features long enough to make the clearing process feel intentional: owl eyes, umbrella canopy/handle, kite panels/tail. Apply larger contiguous shade regions before adding isolated colored pixels.

The current repeated sky/grass/flowers scenes can feel similar even when the subjects differ. Selective variation in silhouette, background treatment and front direction will do more than increasing all boards to 36×36. Maintain some isolated icons and some complete scenes; the CF sheets support both, not a single mandatory style.

For the next playtest compare changed L4/L12/L13/L16 against untouched controls. Capture time at ×1 and ×2 separately; longest forced wait; number and timing of consequential choices; retries; “felt like waiting”; recognition without title; and whether the player voluntarily continues. Keep current catalog hashes with exports. Use a small fresh-player sample as well as the experienced owner, because expert queue reading can hide onboarding problems. These measures can test the proposed causes of boredom. A later cohort comparison is required to make claims about retention.

## Persistent reproduction

The parent committed the adapter as `tools/gameplay-pilot-v9/compare-colony.mjs` and its complete witnesses/assumptions as [pilot-v9-colony-model-replays.json](pilot-v9-colony-model-replays.json). Run it from the repository root. It excludes L5/L25/L26 rather than silently normalizing incomplete inputs. The separate v9 experiment and tradeoffs are documented in [the Russian implementation review](pilot-v9-plan-2026-10-24.md).
