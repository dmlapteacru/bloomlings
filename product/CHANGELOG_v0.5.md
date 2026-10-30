# Bloomlings Pre-production v0.5 — Revision Notes

## Owner decisions · 2026-10-30 — the difficulty is the pods'; Super Hard and bridge levels by hand

- [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §7: the difficulty is built by the pods (the queue's
  order, links, hidden pods and waiting for places); a mechanic on the board (the bridge, other field objects) adds a
  choice and ties the queue's task to the picture, not difficulty of its own, so Hard and Super Hard are built around
  the pod mechanics. In a bridge recipe the pod mechanics take the doses of the recipe without the bridge: "bridge +
  links + hidden" gets the doses of "links + hidden" (links in the lead range); the bridge still counts as a type.
  `tools/rounds/levels.mjs` `doses` and `doseShares` follow; the wave plan L21–L50 notes it.
- §5, the same day: a bridge level's class is the random test with its best bridge only (the deciding one, or the one
  the random games win more often when both win); the test over both bridges is kept as `randomAll`. New levels from
  wave L21–L50; L1–L20 keep their class. `measure.mjs` `randomTest`/`quickShare` take the bridge, `levels.mjs evaluate`
  measures with it; `export.mjs` refuses such a level until `round-proof` and the C# random policy carry the restriction.
- §13, the same day: the generator builds Normal and the Hard levels that pass; every Super Hard, every bridge level and a
  Hard the generator does not manage are built by hand, with the same checks, their scripts and notes kept in
  `content/rounds/experiments/`; the owner plays every Super Hard. Skills `bloomlings-round-wave` (step 5) and
  `bloomlings-round-levels` follow.
- §8 and §13, the same day ("Вот именно так я хочу… Именно супер харды"): a Super Hard with a bridge is one big
  meaningful picture split by a neutral contour of any shape: an outline round the subject (L30 «Пагода на острове»), a
  box the team starts from (one option when the picture suits it), or other contours, traps and passages, as long as
  there is no way round the bridges; the contour comes from the picture's own meaning; the queue decides which bridge
  wins. L45 is «Ярмарка на пирсе» (the pier with the wheel and the big top outlined, gangways from the beach and a boat). `levels.mjs adopt` places a hand-built level on its position
  and so locks it; P0153 approved and L30 locked on the owner's word.

## Owner decisions · 2026-10-28 — bridges on any strip, the wave plan sets the mechanics first

- [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §8: a Sprig bridge stands on any impassable strip, a
  stream or a wall laid over an approved picture (cells under the wall are not work). The crossing goes up, left or
  right; the two sites of one choice may stand on different strips.
- §13: the wave plan first assigns the mechanics to every level (lead, additional, doses by §7, the bridge's side) and
  their shares in the wave; pictures are ordered for that breakdown. The owner approves it, placement keeps it.
- Rules 17 (the owner, 2026-10-29, after the Colony Flow screenshot statistics): links dominate; hidden pods are never the
  lead; links lead or are additional; a bridge only leads (§7, §8); links may lead any number of levels in a row (§11); a
  wave's pictures repeat no theme family within the wave or the 20 levels before it (§11); a bridge stands on one whole
  picture split by a neutral strip (§8). Wave L21–L50 re-planned: `plans/wave-L21-L50-mechanics-plan-2026-10-29.md`.
- §8 (the owner, 2026-10-29): on a Super Hard the wrong bridge may lose right at the choice; the wave plan's L21 leads
  with hidden pods instead of links (§11: L19 and L20 already lead with links).
- Constitution 1.2.0, principle VII: the board's cubes, the pods and the slot pieces may be drawn as seen from one fixed
  tilted viewpoint, as in Colony Flow (foreshortened tops, front faces where the cell in front is empty, side faces by
  the column's position); the runtime stays 2D pictures with no 3D scene, camera or runtime projection, and the rules
  keep a flat grid ("и с боковыми гранями").
- §8, the bridge's choice by class (the owner's second decision of 2026-10-28, replacing three quarters / a quarter):
  Normal may have two winning bridges (one at least 10 points better) or exactly one deciding bridge; Hard half and
  half; Super Hard always exactly one deciding bridge.
- Wave L21–L50: [`plans/wave-L21-L50-mechanics-plan-2026-10-28.md`](plans/wave-L21-L50-mechanics-plan-2026-10-28.md)
  (bridge 11 levels, links 15, hidden 11, no mechanic 7 Normal; bridge pictures: about 6 new with the strip drawn in,
  5 with a wall laid over an approved picture).

## Owner decisions · 2026-10-27 — round levels in the game (spec 006)

- The round engine's rules become the game's rules ([`../specs/006-round-levels-in-game/spec.md`](../specs/006-round-levels-in-game/spec.md)).
  Owner amendment notes are added to docs 01, 02, 03 and 15.
- Constitution 1.1.0:
  - II: hue on board cubes plus a symbols mode for colour-blind players;
  - III: integer model time carried by every command.
- The game holds round levels only, from L1, wave by wave.
- Boosters work in live time.
- Walkers are the family hero tinted in the material colour.
- Presentation follows the owner's decisions:
  - frame and cubes as in the game, no icons on cubes;
  - the clearing animation runs in live time;
  - link tubes sit on the pods' front, half in each pod's colour;
  - Auto runs at ×2 and hides the HUD;
  - the normal speed-up stays.

## Owner decisions · 2026-10-27 — the round generation flow

- [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §13 gains "Волна и контроль качества", after the pilot
  v12 and the owner's choices in [`plans/round-generation-flow-proposal-2026-10-27.md`](plans/round-generation-flow-proposal-2026-10-27.md).
  - Waves of 50 levels; the owner plays a sample of about 10.
  - Pictures are a separate stage, ordered by the wave plan. The mass is drawn as flat SVG drafts and converted; L1–3
    and streams are drawn by hand. The owner approves every picture on a contact sheet.
  - Three quality layers: A, the mandatory rules; B, metrics of interest in `content/rounds/quality.json`, the main one
    "pods without work"; C, the owner's sample.
  - Long runs happen in the cloud session.
  - Three skills: `bloomlings-round-pictures`, `bloomlings-round-levels` and the new `bloomlings-round-wave`.

## Owner amendment · 2026-10-27 (second) — tuning after pilot v11

- [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md), the owner accepted all six proposals of the pilot v11
  review ([`../content/rounds/reports/pilot-v11-review-2026-10-27.md`](../content/rounds/reports/pilot-v11-review-2026-10-27.md)):
  - Hard and Super Hard pass the "opening + colours" test. No first launch followed by "most work now" wins (for
    Super Hard, "release a place fastest" is checked too). After three-launch openings at most 35 % win. This replaces
    the greedy check from the start only (§5, §11, §14).
  - "Most work now" loses about one in three regular Normals on L4–24 and one in two from L25 (§5).
  - The share of decisions with a single possible launch is at most 35 % (§5).
  - In a regular Normal, a pod of a deep colour stands in the first two rows and holds a place (§6).
  - From the second bridge introduction on, exactly one bridge is the solution, in any class (§8, §9).
  - Hidden-pod introductions use 2–3 hidden pods, the first in the second row, and one of them matters for the choice.
    The explanation shows on both introductions (§8, §9).

## Owner amendment · 2026-10-27 — tuning after pilot v10

- [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md), the owner's amendment of 2026-10-27 after pilot v10
  and the comparison with Colony Flow ([`audits/rules-17-vs-colony-flow-2026-10-27.md`](audits/rules-17-vs-colony-flow-2026-10-27.md)):
  colours up to 9 on L4–9 and 10 later (11 the rare exception); 5 columns on boards from 1000 cells; Hard and Super Hard
  must be lost by the greedy strategies; Super Hard always has a lead and an additional mechanic, Hard a mechanic except
  L5; dose positions follow the cycle 50/80/40/70/100/60 % of the range; the Normal median is 50–60 %; picture fitness
  (colours hidden at the start) decides which pictures may carry Hard; bridge sites are not free at the start and one
  bridge is clearly better on the introductions; the extended set up to 50×52 is noted.

## Owner addition · 2026-10-26 — picture theme catalog

- Added [`design/Bloomlings-picture-theme-catalog.md`](design/Bloomlings-picture-theme-catalog.md) (v0.2, the owner's proposal): 50 theme families, 24 creative directions, a franchise list for a separate catalogue with rights checks, internet culture and memes, budgets by cell range, soft variety rules and the task card. Pictures carry its tags (`family`, `direction`, `device`, `subject`, `source`); the picture skills choose subjects from it.

## Owner amendment · 2026-10-26 — picture size ranges

- Level 1 is the owner's fox cub (181 cells, 3 colours, 128 / 48 / 5) instead of the crown; levels 2–3 keep the ladybug 2C and the bird 3B (the owner's matrices). Its pod split is 26+26+26+25+25; 16+16+16; 5 (§3). The owner dropped the crown's first-open order for L1.
- Later the same day: levels 4–9 keep 500–600 cells (4–7 colours); L10–24 stay 550–1000.
- [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §2: stage ranges are now L4–9 400–650 cells,
  L10–24 550–1000, L25–60 550–1200, L61+ 550–1400 (colours unchanged). The floor is not a target: sizes spread toward
  the middle and at times the top of a range, quality first. Mirrored in `05`, the picture skill and `tools/rounds/lib.mjs`.

## Owner amendment · 2026-10-26 — level generation rules

- Added [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md): the single base regulation for generating and
  selecting levels of new rounds (base version v8). It covers the level model, picture sizes, palette and pod budgets,
  the first three levels, the difficulty calendar and the random-test classes, queue building, the 12 mechanic types
  with doses and rare four-type recipes, the introduction calendar, rhythm, pattern checks, speed, the process and
  acceptance.
- Process §13 is split into two skills: `bloomlings-round-pictures` (pictures numbered in sequence, approved by the
  owner on a numbered contact sheet) and `bloomlings-round-levels` (approved pictures → solved levels → calendar, bank
  for leftovers, needs for empty positions). Tools in `tools/rounds/`, content in `content/rounds/`.
- The full mechanic catalog of 26.10.2026 is `design/Bloomlings-gameplay-mechanics.md` (12 types); 17 refers to it.
- Amended with a pointer to 17: 01, 02, 03, 04 (table of the 12 types), 05, 06, 07, 12, 13, 14, 16, the Locked Concept,
  `CLAUDE.md` and spec `001`. Where they differ, 17 wins for new rounds; booster, store, meta and reward unlocks are
  unchanged.

## Added

### `15_TECHNICAL_ARCHITECTURE.md`

High-level technical direction is now documented.

## Technical decisions

1. Unity selected as client engine.
2. C# selected as gameplay/runtime language.
3. Product remains strictly 2D.
4. Core gameplay simulation should be deterministic and independent from visual animation timing.
5. Levels are data definitions, not Unity scenes.
6. 5000+ launch levels are represented as versioned content data.
7. Generator and Solver are first-class offline/content-pipeline systems.
8. Main gameplay is offline-first.
9. Backend is limited primarily to meta systems such as cloud save, leaderboard, remote config and validated economy operations.
10. Future content can be delivered separately from the binary through versioned content packs/CDN-style delivery.
11. Mobile stack must support ads, IAP, analytics, crash reporting and remote config.
12. CI/CD must validate game logic and level content, not only compile the app.
13. Exact backend vendor, analytics vendor, DI framework, UI implementation, class design and project structure remain intentionally undecided.

## Documentation package

v0.5 contains only the latest active documentation plus:
- current Locked Concept;
- current Changelog;
- new Technical Architecture document.

## Revision 2026-09-29 — picture-first levels

Source: clarifications recorded in `specs/001-core-game-mvp/spec.md` (session 2026-09-29).

### Decisions

1. Every level is a **picture-first mosaic**:
   - built from a reviewed base picture made of color roles (max ~14×16 cells);
   - the role → variant mapping fixes the visible top layer of every cell, so the board reads as the subject;
   - hidden layers, keys and specials may deviate while the subject stays recognizable.
2. Cleared cells reveal the **finished version of the same picture**:
   - it is rendered automatically by default;
   - bespoke illustrations are optional;
   - on Win the picture goes to the Collection.
3. The generator is **picture-first + solution-first**:
   - the dependency graph is derived from the picture, which replaces the earlier "graph first, then spatialize" step;
   - pods and the Source Tray are designed around at least one planned solution;
   - the solver validates every level.
4. **Picture reuse** is allowed through remapping, mirroring, background and Source design, within these limits:
   - Levels 1–100 each use a different base picture;
   - the same base picture never appears within 50 consecutive levels;
   - estimated library size is roughly 1000–1500 base pictures for 5000 levels.
5. **No lives**: failed or abandoned attempts are free and restarts are unlimited. This confirms `10_ECONOMY_AND_MONETIZATION.md` §3.

### Updated documents

- `05_LEVEL_STRUCTURE.md` — §1, §4, §5 (Base picture), §6, §11, §18.
- `06_LEVEL_GENERATOR.md` — §1, §3–§10, §19–§22, new §23 (Picture library).
- `12_ART_AND_CONTENT_PIPELINE.md` — §7, §10 (Picture library), §12–§14.
- `14_MVP_SCOPE.md` — §4, §8, §9.
- `15_TECHNICAL_ARCHITECTURE.md` — §4, §6, §7, §8, §17.

Each updated document carries a `Revision 2026-09-29` note under its status line.

### Analysis follow-up (same day)

- `07_DIFFICULTY_AND_PROGRESSION.md` §4 now matches doc 13 and spec `001`: the Hard label unlocks at L5, the Key
  preview at L8, and Stones at L11.
