# 08 — Linear Meta Progression

**Status:** LOCKED (2026-09-29)
**Owner amendment · 2026-11-01:** Wardrobe/cosmetics open at **L44** and the Daily Challenge at **L48**; planned
features are listed in §12 ([`plans/content-unlocks-plan-2026-11-01.md`](plans/content-unlocks-plan-2026-11-01.md),
[`13_UNLOCK_AND_MILESTONE_ROADMAP.md`](13_UNLOCK_AND_MILESTONE_ROADMAP.md)).

> There are no user-facing Garden Areas or level-map progression.

## 1. Main progression

The player's primary status is:

`Level N`

The goal is always:
> continue to the next level.

## 2. Home

Home shows:
- current level;
- Play/Continue;
- Petals/coins;
- settings;
- Store access;
- next meaningful milestone teaser.

Example:

`LEVEL 88`
`Leaderboard Rank: #...`
`Level 100 reward in 12`
`PLAY`

## 3. Milestones

Milestones can unlock:
- booster;
- leaderboard;
- Store feature;
- cosmetics;
- Daily Challenge;
- new target variant;
- new gameplay mechanic;
- profile/leaderboard badge.

The exact unlock roadmap lives in `13_UNLOCK_AND_MILESTONE_ROADMAP.md`.

## 4. Leaderboard

Planned unlock:
**Level 10**

Primary ranking metric:
- highest completed global level.

Possible secondary tie-breakers later:
- stars/clean clears;
- completion time;
- weekly progression.

Baseline should keep ranking understandable.

## 5. Visual theme progression

Background/theme may rotate automatically across level bands.

This is visual freshness, not a navigable Area system.

Examples:
- daylight garden;
- pond garden;
- orchard;
- ancient garden;
- moonlit garden.

## 6. Target-variant unlocks

New target variants can enter the global pool at milestone levels.

Once introduced:
- generator may use them in future levels according to progression profile.

Unlock does not require player selection.

## 7. Cosmetics

Cosmetic Bloomling progression unlocks with the Wardrobe at **Level 44** (Level 40 before the owner's amendment of
2026-11-01); its catalogue opens in waves at L44, L60, L80 and L100.

Possible:
- skins;
- hats;
- trails;
- expressions.

No permanent gameplay power.

## 8. Collection

Optional:
completed/restored pictures can populate a Gallery/Collection.

Collection is not a level selector.

## 9. Daily Challenge

Unlock: **Level 48** (Level 50 before the owner's amendment of 2026-11-01), behind its remote flag. The Daily streak
(planned, §12) opens with it.

Separate optional puzzle:
- one daily board;
- reward;
- does not change main Level N.

## 10. Long-run prestige

At major milestones:
- 100;
- 250;
- 500;
- 1000;
- 2000;
- 3000;
- 5000;

grant visible prestige:
- profile frame;
- skin;
- badge;
- leaderboard marker;
- larger reward bundle.

This gives high-level players something to display without changing core rules.

## 11. No builder

Do not introduce:
- room renovation choices;
- construction timers;
- branching world maps.

Those would add navigation and art overhead without improving the desired core.

## 12. Planned features (owner, 2026-11-01; not implemented)

From the owner's content-unlocks plan (revision 4). None of these exists in the game yet; there are no runtime roadmap
entries for them. Thresholds are the reached current level.

| Level | Feature | Status |
|---|---|---|
| L27 | **Album scenes**: one whole illustration of 3–5 elements, each revealed by winning its matching campaign picture (earlier wins count at once), a one-time chest when finished; not a level selector | Owner-confirmed; not implemented |
| L48 | **Daily streak** with the Daily Challenge: a small bonus for consecutive daily wins, proposed every 7 wins and then a new cycle (length not final); a missed day ends only the current streak; no paid repair, no ads needed. Also planned: a calendar of 365 prepared daily puzzles (date → stable puzzle id) | Planned; not implemented |
| L60 | **Photo studio**: a postcard of a hero in owned gear on a background from finished album scenes, in an owned frame; saved or shared by the player's own action; no second lock | Proposed in the plan, not explicitly confirmed; not implemented |
| L81 | **Master challenges**: a permanent archive of author-checked hard puzzles (5 at first), free retries, one reward and a master mark for the first win; Level N never changes | Owner-confirmed; not implemented |
| Per event | **Calendar events' dynamic access**: at the first entry while an event is active, `eventUnlockLevel = max(30, L₀ + 7)`, stored once per player and event release and never recomputed; access only during the event's calendar time | Owner-confirmed; not implemented |

Deferred or rejected: hero errands (deferred; L150+ only a guide), outfit collections (last, optional), secret search
(rejected), an Endless Stash with paid barriers (no), a Pass (not now), expeditions as a second shell (no).
