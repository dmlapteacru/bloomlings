# 06 — Level Generator, Solver & Launch Content Pipeline

**Status:** LOCKED (2026-09-29)  
**Revision 2026-09-29:** picture-first generation (§1, §3–§10, §19–§23; spec `001` FR-079).
**Revision 2026-10-06 (product owner):** pictures up to 22×28 for the rare big levels (spec `001` FR-008); the
generator writes each level's board look and checks a big level's hidden layers for blind guesses (FR-036).

**Revision 2026-10-15 (product owner):** launch scope is **2500 levels**, with later expansion after launch. Local generation stays on the owner's computer. New picture production prioritizes authored, unusual scene compositions; see `content/pictures/art-direction.md`. Older 5000-level production estimates below describe the expandable system.

## 1. Principle

Levels are **picture-first** and **solution-first**:

1. A reviewed base picture and its role → variant mapping fix the visible top layer of every cell (`05_LEVEL_STRUCTURE.md` §5).
2. The generator derives the dependency structure from the picture: which regions shield others from the Garden Entry.
3. Everything the generator adds (hidden layers, keys, specials, pod partition, Source Tray) is designed around at least one planned winning solution.
4. The solver validates the result.

Randomly painting colors and hoping for solvability is forbidden. Picture colors are authored content, not random paint.

## 2. Launch requirement

Bloomlings ships **2500 deterministic, solver-validated sequential levels at launch**, as configured in `content/launch.props`.

The local generator builds gameplay around a reviewed picture library. The launch does not require 2500 separately authored illustrations.

Content model:

- Levels 1–100: heavily curated / many handcrafted.
- Levels 101–500: generated from profiles, solver-validated, strong human review.
- Levels 501–2500: profile-driven generation, automatic solver validation, sampling/manual QA.
- Levels 2501+: future content updates using the same extensible pipeline.
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

New launch-art rule (owner amendment, 2026-10-15):
- L1–2500 use 2500 different compositions, one use each;
- resize/mirror/recolor and different Source difficulty do not create a new composition.

The current picker and existing source catalog still implement the legacy rules (first 100 unique, no reuse within
50, changed mapping/mirror and Source on reuse). They are retained until the art migration; passing them does not
satisfy the new launch-art requirement. The composition registry audit must pass before publishing the replacement art.

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
- grid of color roles (max ~14×16 until 2026-10-06; now 224–288 cells for regular levels, up to 22×28 for big ones);
- optional background region;
- finished (restored) look: rendered automatically from the grid by default, or a bespoke illustration for curated/milestone levels;
- tags: theme, season, suitable level bands, structure metrics (region count, nesting depth);
- review status.

Sources:
- hand-drawn;
- generated;
- generated and then edited.

Owner amendment, 2026-10-15: the launch needs **2500 unique compositions for 2500 levels**, each used once.
Resizing, mirroring, recoloring or changing the difficulty does not create another composition. This supersedes the
800–1000 estimate and the reuse plan for new launch art. The existing catalog/picker still follows the legacy windows;
it must be migrated and checked against the composition registry before claiming this target is met.
Pilot batches validate art quality; they are never recycled to fill the launch. See `content/pictures/art-direction.md`.

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

## Approved size distribution (owner amendment, 2026-10-15)

For the new L1–2500 composition library, the approved weighted distribution and pacing rules are recorded in
[`content/pictures/art-direction.md`](../content/pictures/art-direction.md#согласованное-распределение-размеров-по-уровням).
This supersedes the old equal-size production target from L11. At L51–100, 65% of boards are large/very large;
at L301–2500, compact boards are removed: 20% medium (around 21×24–22×26), 60% large, 20% very large.
The percentage quotas, pacing and art migration remain to be implemented; the current picker/validator and existing
catalog have not changed. Large-board mystery restrictions still apply and need separate review before reintroducing
Mystery on the new post-L300 size mix. Visible tile symbols are independent of the stored `boardLook` layer policy.
