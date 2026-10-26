# 07 — Difficulty & Global Level Progression

**Launch revision 2026-10-15:** the first release contains 2500 sequential levels. Long-run guidance and milestones beyond 2500 describe future content updates, not required launch content.
**Status:** LOCKED (2026-09-29)  
**Owner amendment · 2026-10-26:** new rounds follow [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md); its difficulty calendar, class bands and mechanic introduction calendar replace §3–§15 here.
**Revision 2026-09-29:** §4 aligned with the unlock roadmap in doc 13 and spec `001` (Hard label L5, Key L8, Stones L11).

## 1. Progression model

Single infinite-style sequence:

`Level 1 → 2 → ... → 100 → ... → 1000 → ... → 5000+`

No map or level chooser.

## 2. Two progression layers

### System progression
Early levels unlock:
- boosters;
- leaderboard;
- mechanics;
- cosmetics/store features;
- challenges.

### Puzzle progression
Difficulty changes through:
- active variants;
- board depth;
- Source ordering;
- Buffer pressure;
- layers;
- connected/locked groups;
- special objects.

System unlocks become sparse later.

## 3. Levels 1–10 — dense onboarding

Goal:
teach core loop and unlock first recovery tools quickly.

Approximate cadence is defined in `13_UNLOCK_AND_MILESTONE_ROADMAP.md`.

By Level 10 player should understand:
- Source Pods;
- 5 Waiting Slots;
- exact matching;
- partial waiting;
- Jam;
- basic boosters.

Leaderboard unlocks at Level 10 in our current roadmap.

## 4. Levels 11–25

- all four families represented;
- 3–4 target variants;
- source depth;
- Stone blockers (from L11);
- keys are practised (the Key preview unlocks at L8, see doc 13);
- Hard levels become regular (the Hard label unlocks at L5, see doc 13).

## 5. Levels 26–50

- Layered Tiles;
- second same-family target variant;
- Locked Pods;
- Connected Pair;
- target count moves toward 4–5.

Level 40 is a major system unlock milestone.

## 6. Levels 51–100

- Gate/heavy blockers;
- Fountain;
- 5 active variants normal;
- occasional 6-variant challenge;
- stronger Source dependencies.

Level 100 = major milestone reward.

## 7. Levels 101–250

Focus:
combining established mechanics rather than tutorializing constantly.

Possible additions:
- locked Waiting Slot;
- Mystery Pod if fair;
- depth-3 layers;
- additional target variants.

## 8. Levels 251–500

Advanced mastery:
- 5–6 variants normal;
- advanced connected/locked combinations;
- environmental combinations;
- selected optional mechanics.

After roughly Level 500, major new mechanics should become rare.

## 9. Levels 501–1000

Mature game loop.

Variety:
- new board pictures;
- normal/hard/super-hard rhythm;
- variant combinations;
- milestone rewards;
- cosmetics;
- occasional new target variant.

Do not keep increasing average difficulty without relief.

## 10. Levels 1001–2000

Long-run retention band.

Use:
- broad profile rotation;
- harder Source layouts;
- deeper board dependencies;
- milestone rewards;
- leaderboard motivation.

No requirement for entirely new gameplay system.

## 11. Levels 2001–5000+

Sustainable endless-style content.

The player at Level 2500 should still be playing the same understandable game, not a pile of 30 mechanics.

Use curated profile families and difficulty waves.

## 12. Hard / Super Hard cadence

Provisional target after onboarding:

- Hard approximately every 4–6 levels.
- Super Hard approximately every 10–15 levels.

Exact cadence should be tuned after testing.

Do not make pattern perfectly predictable.

## 13. Milestone cadence after core onboarding

Possible long-run rhythm:

- every 5–10 levels: small reward/difficulty beat;
- every 25 levels: meaningful reward chest/booster bundle;
- every 50 levels: cosmetic/profile reward;
- every 100 levels: major milestone;
- every 250–500 levels: larger prestige/content reward.

## 14. Target variant progression

- start with 2;
- quickly reach 3–4;
- introduce same-family sibling after core matching is understood;
- 4–5 becomes standard;
- 5–6 becomes common in later game;
- 7 is rare/optional challenge only.

## 15. New mechanics rule

Early:
frequent unlocks are good.

Late:
new mechanics are rare.

After ~500:
prefer variations, target variants, cosmetics and profile combinations over new core rules.

## 16. Difficulty waves

Use:

`easy → normal → hard → relief → normal → normal → super hard → relief`

Avoid monotonic escalation.

## 17. Failure quality

Good:
> I chose that pod too early.

Bad:
> The game hid information I could not know.

Solver/fairness rules remain mandatory.

## Unique-art launch production (2026-10-15 owner revision)

The frozen 2500-slot schedule is in `content/launch-art/plan.json`. Adding or repairing art must not move sizes,
classes or required mechanic introductions. The gen-1.6.0 launch preset is
`content/profiles/difficulty-thresholds-launch.json`: decision pressure determines class, while work/pod limits still
bound duration. Every generated class must match its measured score, without overrides. This preset requires player
playtesting; the existing catalog and Daily retain their original difficulty thresholds.

Visual complexity does not increase with puzzle difficulty: the remaining pictures use large, sparse shapes, even on
30–36-cell boards. Readability in the finished cell grid takes precedence over detail in a source illustration.

## Owner amendment · 2026-10-26 — level generation rules

Rules: [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §5, §9, §10. Supersedes this document where they differ.

- **Calendar (§5).** Every 15th level is Super Hard; other multiples of 5 are Hard; the rest are Normal (5, 10, 20, 25
  Hard; 15, 30 Super Hard). Round milestones 50, 100, 150 … keep their Hard or Super Hard. The calendar says which
  verified class a position needs; it does not choose the picture size and does not turn a picture into a class.
- **Classes by the random test (§5).** Win share of the extended random bot (100 runs, widened to 400 when the 95 %
  Wilson interval crosses a border; ×1; waiting allowed only before a scheduled event): Normal 50–100 % (preferred
  60–90 %), Hard 20 % to under 50 % (25–40 %), Super Hard 5 % to under 20 % (7–15 %). Below 5 % is not released at start.
  The first Normal after a Hard or Super Hard aims at 75–95 % (90–100 % when it introduces a mechanic). The picture area
  is not part of the class.
- **Quality guides (§5).** Dangerous forks, forks with a single safe action, distinct winning plans and pods waiting
  without work per class are tuning guides, not hard filters.
- **Mechanic introduction (§9).** Two first meetings on Normal, alone, with a random-test target of 90–100 %: hidden
  pods 6/8, links 11/13, Sprig bridge 16/18, consolidation 19–50, Twig latch + acorn key 51/53, Drop canal 101/103,
  Twig shutters 151/153, Twig repair of the fifth slot 201/203, Bloom bud 251/253, Bloom seed 301/303, Drop well
  351/353, Drop hidden cells 401/403, stacks 451/453; after 453 the full set and its combinations. A new mechanic never
  first appears on Hard or Super Hard; three types need two separate meetings with each; four types only by the six
  recipes of §7.
- **Rhythm (§10).** Five segments of done work W (0–20 … 80–100 %); after 80 % W, 1–4 remaining launches (up to 6 on a
  long Super Hard); no new mandatory mechanic or unexpected dependency in the last fifth.
- **Levels 1–3 (§4).** Crown 138 cells / 3 colours, ladybug 2C 240 / 4, bird 3B 281 / 6; 3 columns, 5 slots, no extras.
