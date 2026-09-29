# 06 — Level Generator, Solver & 5000-Level Content Pipeline

**Status:** DRAFT FOR LOCK  
**Revision 2026-09-29:** picture-first generation (§1, §3–§10, §19–§23; spec `001` FR-079).

## 1. Principle

Levels are **picture-first** and **solution-first**:

1. A reviewed base picture and its role → variant mapping fix the visible top layer of every cell (`05_LEVEL_STRUCTURE.md` §5).
2. The generator derives the dependency structure from the picture: which regions shield others from the Garden Entry.
3. Everything the generator adds (hidden layers, keys, specials, pod partition, Source Tray) is designed around at least one planned winning solution.
4. The solver validates the result.

Randomly painting colors and hoping for solvability is forbidden. Picture colors are authored content, not random paint.

## 2. Launch requirement

Bloomlings must support **at least 5000 deterministic, solver-validated levels at launch**.

This does **not** mean 5000 hand-authored boards.

Content model:

- Levels 1–100: heavily curated / many handcrafted.
- Levels 101–500: generated from profiles, solver-validated, strong human review.
- Levels 501–5000+: profile-driven generation, automatic solver validation, sampling/manual QA.
- Milestone / Hard / Super Hard levels receive stronger curation.

A level number must map to a stable level definition.

## 3. Deterministic level contract

For every shipped level number store or derive a stable definition containing:

- level number;
- seed/version;
- base picture id + picture version;
- role → variant mapping;
- transform (mirroring, background treatment);
- board mask (derived from the picture);
- exact target variant set;
- cell/layer layout;
- Source Pod stacks;
- pod counts;
- keys/locks;
- connected pods;
- specials;
- intended difficulty class;
- difficulty score;
- solvability result;
- one or more valid solution traces;
- Hard/Super Hard flag;
- reward profile.

A definition may be a compact config (e.g. picture id + mapping + seed + parameters) that the game expands deterministically. The expanded level must be identical on every device.

Updating the app must not silently reshuffle already shipped levels unless intentionally versioned.

## 4. Generation profile inputs

- level band;
- board dimensions;
- picture pool (tags, size range);
- picture structure targets (region count, nesting depth, background share);
- total work volume;
- active target variant count;
- allowed global variants;
- role → variant mapping constraints;
- Entry layout;
- layer depth;
- allowed mechanics;
- Source stack count;
- pod-size ranges;
- Buffer pressure target;
- target difficulty;
- target duration;
- Hard/Super Hard mode;
- milestone constraints.

## 5. Choose base picture

Pick a reviewed base picture from the picture library (§23) that fits the profile:
- size within the band's board range;
- structure (region count, nesting, background) suitable for the target difficulty;
- tags suitable for the band/theme;
- not blocked by similarity control (§20).

Allowed transforms:
- mirroring;
- background treatment.

Every base picture is reviewed by a person for recognizability and gameplay usability before first use.

## 6. Map color roles to exact variants

The picture's color roles set how many active variants the level can have. The mapping picks which ones.

Typical active count:
- tutorial: 2–3;
- early: 3–4;
- normal: 4–5;
- advanced: 5–6;
- rare challenge: 6–7.

Rules:
- follow the color groups in `05_LEVEL_STRUCTURE.md` §5;
- only variants allowed for the level band;
- respect icon/color distance;
- same-family variants are allowed.

If a picture has more roles than the band allows, similar roles may share one variant (e.g. stem + pot → Wood) as long as the subject still reads. Otherwise choose another picture.

## 7. Derive dependency graph from the picture

Graph nodes are chunks of exact-variant work: connected regions of the mapped picture, plus hidden layers and specials added later.

Edges come from shielding. A region that must be cleared before another becomes reachable from the Garden Entry is its parent.

Example (flower in a pot, Entry at bottom center):

`Water background edge → Wood pot → Leaf stem/leaves → Flower petals → Acorn center`

Graph determines reachability order. Entry placement and mirroring change the graph and are part of the design choice.

## 8. Fit structure to the target

There is no free spatialization step: the picture fixes where every visible region is.

To hit the profile's structure target, the generator may:
- choose a different base picture;
- change the role → variant mapping (merge roles, swap siblings);
- mirror the picture;
- move or add Garden Entries;
- add hidden layers, keys and specials (§9–10) to deepen or branch dependencies.

A parent region physically protects its child region, either through the picture itself or through added layers.

Keep the subject recognizable at level start.

## 9. Add layers

Examples:

`Leaf → Violet`
`Moss → Dew`
`Acorn → Water → Flower`

All hidden layers count in exact variant demand.

The visible top layer must still follow the picture. Hidden layers may deviate from it.

## 10. Add mechanics

Only mechanics unlocked for that level band may appear.

Keys and specials must not hide a tile's icon or color and must not break subject recognizability.

Examples:
- locked pod;
- connected pair;
- key;
- heavy garden door;
- layered tile;
- Fountain;
- locked Waiting Slot;
- Mystery.

Unlock roadmap is authoritative.

## 11. Calculate exact demand

Demand is per exact target variant.

Example:
- Leaf = 83
- Moss = 41
- Flower = 67
- Violet = 36
- Water = 58

Never aggregate only by Bloomling family.

## 12. Partition into Source Pods

Each exact target total is partitioned intentionally.

Example:
Moss 41 → `17 + 24`

Partition influences:
- immediate completion;
- partial waiting;
- buffer pressure;
- source-stack dependency.

## 13. Build Source Tray

Arrange pods into stacks to provide:
- useful choices;
- tempting premature choices;
- buried future pods;
- locked/connected structures.

## 14. Deterministic solver

Solver tracks:
- current visible exact tile variants;
- hidden layers;
- open routes;
- active Waiting Pods and remaining counts;
- exposed Source Pods;
- keys;
- locks;
- special states;
- Buffer capacity.

It explores legal tap sequences.

Reject if no winning path exists.

## 15. Player-information solver

Required for Mystery mechanics.

Reject levels where only hidden omniscient knowledge avoids unavoidable failure.

## 16. Difficulty score

Factors:

### Structure
- dependency depth;
- branching;
- unsafe-choice density;
- dead-end depth.

### Buffer
- peak occupancy;
- partial pod persistence;
- connected commitments.

### Variants
- active variant count;
- same-family siblings;
- color/icon similarity;
- cross-variant layers.

### Specials
- keys/locks;
- Gate chains;
- Mystery load;
- locked slot;
- environmental objects.

### Scale
- total work;
- estimated duration.

## 17. Difficulty classes

Every level is classified:

- Normal
- Hard
- Super Hard

Hardness is computed and may be manually overridden during curation.

Hard/Super Hard are not new rules; they are profile presets with higher complexity targets.

## 18. Long-run progression profiles

Do not hand-tune all 5000 individually.

Use progression bands.

Example:

- 1–25: onboarding
- 26–50: early mechanics
- 51–100: core system completion
- 101–250: combination expansion
- 251–500: advanced mastery
- 501–1000: mature normal loop
- 1001–2000: mature + harder profiles
- 2001–5000: long-run rotation with controlled escalation

Difficulty should wave, not climb forever.

## 19. Anti-power-creep rule

Level 4000 does not need 20 mechanics.

Long-run variety comes from:
- base pictures and their mappings/mirroring;
- target sets;
- deeper Source ordering;
- different pod partitioning;
- layers;
- mechanic combinations;
- Hard/Super Hard profiles.

## 20. Similarity control

Track recent:
- base picture (and its mapping/mirroring);
- active variants;
- family mix;
- variant proportions;
- region topology;
- mechanics;
- Source arrangement;
- solution depth;
- difficulty class.

Reject repetitive sequences.

Hard rules:
- Levels 1–100: every level uses a different base picture;
- the same base picture never appears twice within 50 consecutive levels;
- a reused base picture must differ in role → variant mapping or mirroring **and** in Source design.

## 21. QA strategy

### 1–100
Manual playtest every level.

### 101–500
Solver + manual playtest every level during pre-production/soft launch if practical.

### 501–5000
Solver every level + automated invariants + representative human sampling + special review for milestone/Hard/Super Hard.

### Picture library
Every base picture is reviewed before first use:
- recognizable at board resolution with tile symbols on;
- readable in its intended mappings;
- usable for gameplay.

## 22. Required invariants

Every shipped level:
- solvable without boosters;
- exact variant capacity reconciles;
- no inaccessible mandatory content;
- no illegal mechanic before unlock;
- no unavoidable hidden-information fail;
- no unreadable palette combination;
- visible top layer follows the picture's role → variant mapping;
- picture reuse limits respected (§20);
- stable deterministic definition.

## 23. Picture library

The long-run catalog is driven by a library of base pictures, not by bespoke boards.

Each base picture stores:
- subject/name;
- grid of color roles (max ~14×16);
- optional background region;
- finished (restored) look: rendered automatically from the grid by default, or a bespoke illustration for curated/milestone levels;
- tags: theme, season, suitable level bands, structure metrics (region count, nesting depth);
- review status.

Sources:
- hand-drawn;
- generated;
- generated and then edited.

Size estimate:
- Levels 1–100 need 100 different base pictures;
- with each base picture used in at most ~5 levels, 5000 levels need roughly 1000–1500 base pictures.

Exact library size and production plan are decided in implementation planning.
