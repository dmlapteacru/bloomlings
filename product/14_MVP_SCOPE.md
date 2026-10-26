# 14 — Launch / MVP Scope

**Status:** LOCKED (2026-09-29)  
**Owner amendment · 2026-10-26:** the mechanic set and calendar for new rounds are those of [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) (12 types, introduction calendar of §9).
**Revision 2026-09-29:** picture-first levels and picture library (§4, §8, §9; spec `001` FR-006).

> Revision 2026-10-15 supersedes v0.4: the launch contains **2500 sequential levels**, generated and solver-validated locally on the owner's computer. Additional levels follow after launch. Picture quality and distinctive composed scenes take priority over catalog size; see `content/pictures/art-direction.md`.

## 1. Product goal

Launch a Colony Flow-like long-run puzzle product that can support:
- new player onboarding;
- Level 100 milestone;
- Level 1000 players;
- Level 2000 players;
- Level 2500 players at launch, with later expansion;

without changing the fundamental UX.

## 2. Launch level requirement

**2500 deterministic, solver-validated sequential levels at launch.**

The boundary is `content/launch.props`; the source catalog can retain later levels as reserve. A reviewed picture library supplies the compositions.

## 3. Curation tiers

### Levels 1–100
- heavy manual design/review;
- all unlock tutorials;
- progression tightly controlled.

### 101–500
- generator-assisted;
- solver validated;
- strong manual review;
- advanced mechanics introduced.

### 501–2500 at launch; 2501+ in future updates
- generator/profile driven;
- solver validates every level;
- milestone/Hard/Super Hard receive stronger review;
- human sampling and automated QA.

## 4. Core gameplay required

- dense large 2D board;
- every level is a picture: a picture-first mosaic built from a base picture (`05_LEVEL_STRUCTURE.md` §5);
- many full target cells;
- 4 Bloomling families;
- 8 launch target variants;
- 3–6 typical active variants;
- Garden Entry;
- stacked Source Tray;
- numbered Spirit Pods;
- 5 Waiting Slots;
- exact matching;
- partial completion;
- concurrent workers;
- route/reachability;
- Jam;
- no timer;
- 2× speed;
- restoration reveal of the finished picture;
- Next Level.

## 5. Launch boosters

Primary four:
- Extra Slot;
- Shuffle;
- Return;
- Bloom Burst.

Reveal only if Mystery ships.

Wild Spirit is optional later consumable, not baseline toolbar booster.

## 6. Launch systems

Required:
- linear Level N progression;
- milestone unlock system;
- Leaderboard unlocked at Level 10;
- Store;
- Daily Reward;
- Hard / Super Hard profiles;
- currency;
- rewarded recovery;
- interstitial infrastructure;
- Remove Ads;
- cosmetics/wardrobe if Level 40 roadmap remains locked.

Daily Challenge is desired but can be cut if scope pressure requires; roadmap must then substitute Level 50 unlock.

## 7. Launch mechanics

Reference-core:
- stacked Source;
- connected pods;
- keys;
- locked pods;
- heavy route blocker.

Bloomlings-specific:
- Layered Tiles;
- Fountain;
- additional exact target variants.

Late launch progression candidates:
- locked Waiting Slot;
- Mystery Pod;
- Mystery Tile;
- Chest;
- Statue/Bridge;
- Connected Triple.

Only mechanics validated before launch should appear in the launch profiles.

## 8. Art/content

Required:
- strict 2D;
- 4 family animation rigs;
- 8 launch target-variant sets;
- Source Pod asset system;
- five-slot UI;
- special-object assets;
- quiet background themes;
- picture library: 2500 unique compositions for 2500 launch levels, each used once; resize/mirror/recolor variants do not count (owner amendment, 2026-10-15) (`06_LEVEL_GENERATOR.md` §23);
- automatic finished-picture rendering, with bespoke illustrations only where chosen (e.g. milestones).

The new 2500-level art library must use each composition once. The existing catalog still uses legacy reuse rules
and is not evidence that 2500 unique compositions are already ready.

## 9. Generator/solver launch gate

Before release:
- all 2500 launch level definitions generated/materialized and solver-validated;
- solver passes every level;
- exact target accounting passes;
- mechanic unlock constraints pass;
- palette/readability constraints pass;
- every base picture reviewed; subject recognizable at level start;
- 2500 visually reviewed, unique compositions cover L1–2500 exactly once; resize/mirror/recolor copies do not count;
- no booster-required solutions;
- no invalid hidden-information dependencies.

## 10. UX launch screens

- Home
- Gameplay
- Pause
- Win/Next
- Jam/Recovery
- Store
- Settings
- Leaderboard
- Wardrobe if shipped
- Daily Reward
- Daily Challenge if shipped

No level map.

## 11. Explicitly out of launch baseline

- world map;
- room builder;
- PvP;
- clans;
- narrative campaign;
- permanent hero power upgrades;
- battle pass unless added after product validation;
- 5000 handcrafted scenes.

## 12. Pre-code gate

Do not start full implementation until:
- all design docs locked;
- unlock roadmap locked at least through Level 500;
- long-run milestone policy supports expansion beyond the 2500-level launch;
- generator profiles cover the launch and remain extensible;
- leaderboard semantics locked;
- four booster semantics locked;
- launch mechanic set frozen;
- Technical Architecture then designed separately.

## Extended board support (owner amendment, 2026-10-15)

The supported ceiling is **36×36 (1296 cells)**, including 24×24, 30×32 and 36×36. This supersedes the older
22×28 format ceiling in the earlier sections. Tutorial boards remain 11×12–12×12. From L11 the standard profiles
accept large art without changing the existing catalog. Small grids use simple, recognizable subjects; unusual
story compositions use 22×28 or large grids. The launch still requires 2500 unique compositions used once each.

Runtime coordinates, JSON readers/schemas, generation, solver/validation, packs and the clients share this ceiling.
Counts/work/duration scale only above the old 616-cell profile anchor; thresholds through 616 cells remain pinned
to the previous calculation, then scale for larger areas. Five slots, variant limits, mechanic unlocks and hidden-layer
fairness still apply. Existing definitions and production pictures are not regenerated.

The three restored engineering fixtures and their exact local generation recipes are documented in
`content/pictures/dev/README.md`. Their pictures are separate from the production library and explicitly included
only in the tester/desktop preview. Dense-board completed images retain their gameplay tile style in win/Collection.

## Owner amendment · 2026-10-26 — level generation rules

The approved set has 12 types: hidden pods, links, latch + acorn key, wooden shutters, closed bud, well, hidden cells,
stacks of one material, and the four hero mechanics (Sprig bridge, Drop canal, Twig repair of the fifth slot, Bloom
seed). The round engine plays hidden pods, links and the bridge today; the others need engine and pilot work before
the levels that introduce them (51 and later).
