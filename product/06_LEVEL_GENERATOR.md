# 06 — Level Generator, Solver & 5000-Level Content Pipeline

**Status:** DRAFT FOR LOCK

## 1. Principle

The generator creates a valid dependency puzzle first, then spatializes it.

Randomly painting colors and hoping for solvability is forbidden.

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
- board mask;
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

Updating the app must not silently reshuffle already shipped levels unless intentionally versioned.

## 4. Generation profile inputs

- level band;
- board dimensions;
- mask family;
- total work volume;
- active target variant count;
- allowed global variants;
- cluster ranges;
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

## 5. Choose mask

Use approved picture/mosaic silhouette.

Sources:
- authored templates;
- procedural masks;
- generated silhouette candidates;
- transformed existing templates.

All shipped masks are reviewed for readability.

## 6. Choose active variants

Typical:
- tutorial: 2–3;
- early: 3–4;
- normal: 4–5;
- advanced: 5–6;
- rare challenge: 6–7.

Respect icon/color distance.

Same-family variants are allowed.

## 7. Build dependency graph

Graph nodes are chunks of exact-variant work.

Example:

`Leaf edge → Moss route → Key → Gate → Violet core`

Graph determines reachability order.

## 8. Spatialize graph

Map nodes into irregular clusters.

Parent region physically protects child region.

Keep shape organic and picture-readable.

## 9. Add layers

Examples:

`Leaf → Violet`
`Moss → Dew`
`Acorn → Water → Flower`

All hidden layers count in exact variant demand.

## 10. Add mechanics

Only mechanics unlocked for that level band may appear.

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
- masks;
- target sets;
- deeper Source ordering;
- different pod partitioning;
- layers;
- mechanic combinations;
- Hard/Super Hard profiles.

## 20. Similarity control

Track recent:
- mask;
- active variants;
- family mix;
- variant proportions;
- cluster topology;
- mechanics;
- Source arrangement;
- solution depth;
- difficulty class.

Reject repetitive sequences.

## 21. QA strategy

### 1–100
Manual playtest every level.

### 101–500
Solver + manual playtest every level during pre-production/soft launch if practical.

### 501–5000
Solver every level + automated invariants + representative human sampling + special review for milestone/Hard/Super Hard.

## 22. Required invariants

Every shipped level:
- solvable without boosters;
- exact variant capacity reconciles;
- no inaccessible mandatory content;
- no illegal mechanic before unlock;
- no unavoidable hidden-information fail;
- no unreadable palette combination;
- stable deterministic definition.
