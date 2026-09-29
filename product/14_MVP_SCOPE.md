# 14 — Launch / MVP Scope

**Status:** LOCKED (2026-09-29)  
**Revision 2026-09-29:** picture-first levels and picture library (§4, §8, §9; spec `001` FR-006).

> v0.4 changes the content requirement substantially: the launch build must support **5000+ levels**, even though only the early portion is heavily handcrafted.

## 1. Product goal

Launch a Colony Flow-like long-run puzzle product that can support:
- new player onboarding;
- Level 100 milestone;
- Level 1000 players;
- Level 2000 players;
- Level 5000+ players;

without changing the fundamental UX.

## 2. Launch level requirement

**At least 5000 deterministic, solver-validated sequential levels.**

This is a content-system requirement, not a requirement for 5000 handmade designs.

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

### 501–5000+
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

Only mechanics validated before launch should appear in the 5000-level profiles.

## 8. Art/content

Required:
- strict 2D;
- 4 family animation rigs;
- 8 launch target-variant sets;
- Source Pod asset system;
- five-slot UI;
- special-object assets;
- quiet background themes;
- picture library: 100 different base pictures for Levels 1–100, roughly 1000–1500 for the 5000-level catalog (`06_LEVEL_GENERATOR.md` §23);
- automatic finished-picture rendering, with bespoke illustrations only where chosen (e.g. milestones).

5000 levels do not require 5000 bespoke illustrations.

## 9. Generator/solver launch gate

Before release:
- all 5000 level definitions generated/materialized or reproducibly derivable;
- solver passes every level;
- exact target accounting passes;
- mechanic unlock constraints pass;
- palette/readability constraints pass;
- every base picture reviewed; subject recognizable at level start;
- picture reuse limits pass (Levels 1–100 unique, no repeat within 50 levels);
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
- long-run milestone policy locked through Level 5000;
- generator profiles defined through Level 5000;
- leaderboard semantics locked;
- four booster semantics locked;
- launch mechanic set frozen;
- Technical Architecture then designed separately.
