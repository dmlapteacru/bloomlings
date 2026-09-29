# 05 — Level Structure

**Status:** LOCKED (2026-09-29)  
**Revision 2026-09-29:** levels are picture-first mosaics (§1, §4, §5, §6, §11, §18; spec `001` FR-006).

## 1. Core structural goal

A level is a **picture**: a dense mosaic of full gameplay cells whose tile colors draw a recognizable subject, as in the reference game.

Every level is built from a **base picture** (§5). The board should read as its subject from the first second, before any tile is cleared.

The grid may be rectangular internally. The visible playable area follows the picture: either the subject's outline or the full picture frame including a background region.

## 2. Board size

Suggested logical ranges:

### Tutorial
- 7×8
- 8×8

### Early standard
- 9×10
- 10×10

### Standard
- 10×12
- 11×12
- 12×12

### Large/advanced
- 12×14
- 14×14
- up to ~14×16 if phone readability remains good.

## 3. Work volume

Processable tile-layers:

- tutorial: 30–60
- early: 50–100
- standard: 90–180
- hard/late: 150–300+
- exceptional late levels may exceed this if animation/readability remains good

## 4. Density

Inside the picture's playable area:
- target initial occupancy roughly 75–95%;
- empty holes should be meaningful;
- board should not look sparse.

## 5. Base picture (picture-first mosaic)

Each level starts from a base picture:
- a small image of a garden-world subject, drawn at board resolution (max ~14×16 cells);
- made of abstract **color roles** (e.g. petal, leaf, stem, pot, background), not fixed variants;
- an optional background region around the subject;
- optionally stones and meaningful empty holes as neutral elements, as long as all mandatory content stays reachable.

Subject examples:
- flower;
- butterfly;
- watering can;
- leaf;
- tree;
- mushroom;
- fountain;
- bird;
- fruit/berry;
- snail/ladybug;
- cozy garden object;
- seasonal motif;
- abstract ornamental motif.

### Role → variant mapping

The level definition maps every color role to one exact target variant from the matching color group:
- green areas → Leaf (green) or Moss (teal);
- pink/purple areas → Flower (pink) or Violet Bud (purple);
- blue/cyan areas → Water (blue) or Dew (cyan);
- brown/orange areas → Wood (brown) or Acorn (orange);
- after expansion: lime → Vine, red → Berry, indigo → Mist, gold → Bark.

Rules:
- the **visible top layer** of every cell follows the mapping, so the board reads as the subject;
- hidden layers, keys and specials may deviate from the picture while the subject stays recognizable at level start;
- if a role has no allowed variant in the level band, map it to the closest allowed variant or do not use the picture in that band;
- §9 still applies: no three extremely similar shades in one board.

### Reuse

One base picture can produce several levels by changing:
- the role → variant mapping (e.g. pink flower → purple flower);
- mirroring;
- the background treatment;
- layers, specials and Source design.

Limits (see `06_LEVEL_GENERATOR.md` §20):
- Levels 1–100: every level uses a different base picture;
- the same base picture never appears twice within 50 consecutive levels;
- a reused picture must differ in mapping or mirroring **and** in Source design.

### Finished picture

The finished version of the same picture lies beneath the tiles (see `12_ART_AND_CONTENT_PIPELINE.md` §12).

Clearing reveals it cell by cell. On Win it is shown in full and added to the Collection.

## 6. Cluster shapes

Clusters are the picture's color regions: outline, subject parts, background.

Prefer subjects whose regions form:
- blobs;
- stepped edges;
- L-shapes;
- snakes;
- rings;
- pockets;
- nested shapes;
- branches;
- asymmetric masses.

Avoid pictures that break down into rectangular quadrants or large flat stripes.

## 7. Active target variants

The game has a larger global variant pool, but each level uses a subset.

Recommended:

### Tutorial
2–3 variants.

### Early
3–4.

### Standard
4–5.

### Advanced
5–6.

### Exceptional
6–7 only if board and Source Tray remain readable.

This replaces the old `max four colors` assumption.

## 8. Family distribution

A level may include:
- one variant from four families;
- two variants from one family plus other families;
- several same-family variants.

Example 5-type level:
- Leaf Sprig
- Moss Sprig
- Pink Bloom
- Water Drop
- Wood Twig

This is valid.

## 9. Same-family variant use

Same-family variants are valuable because they create visual character continuity while preserving gameplay-type complexity.

However:
- they must use clearly distinct color/icon combinations;
- avoid introducing three extremely similar shades in one board.

## 10. Garden Entry

Default:
- one bottom-center Entry.

Variants:
- two entries;
- secondary entry unlock;
- side entry in special level.

## 11. Structural archetypes

- Peel
- Fork
- Merge
- Ring
- Corridor
- Twin Fronts
- Key Branch
- Door Split
- Layer Reveal
- Fountain Dependency

With picture-first levels, an archetype emerges from:
- how the picture's regions nest relative to the Garden Entry;
- plus the layers, specials and Source design the generator adds.

The generator picks pictures, mappings, mirroring and Entry placement whose structure fits the target archetype (`06_LEVEL_GENERATOR.md` §7–8).

## 12. Pod count ranges

Typical group counts:
- small: 5–15
- medium: 16–40
- large: 41–100
- exceptional: 100+

## 13. Number of Source Pods

Typical:
- tutorial: 3–7
- early: 6–12
- standard: 10–20
- advanced: 15–30+

## 14. Variant distribution across pods

A target variant does not need one single pod.

Example Water total = 73:
- Water ×21
- Water ×32
- Water ×20

This creates ordering and partial-buffer behavior.

## 15. Level duration

- tutorial: 20–45 sec
- normal: 45–120 sec
- hard: 2–4 min

## 16. Buffer pressure

- Relaxed: 1–2
- Normal: 2–3
- Tense: 3–4
- Critical: 4–5

## 17. Difficulty impact of target-count variety

Holding other mechanics constant:

- 3 variants → low color-order complexity
- 4 variants → normal
- 5 variants → materially more Source/Buffer complexity
- 6 variants → advanced
- 7 variants → special/high-readability requirement

Variant count is a difficulty lever and must be part of level scoring.

## 18. Invalid layouts

Reject if:
- too few cells;
- sparse checkerboard;
- ambiguous cell art;
- impossible route;
- per-variant capacity mismatch;
- too many visually similar active variants;
- forced blind guess;
- booster required;
- near-duplicate recent layout;
- subject not recognizable at level start;
- visible top layer not following the picture's role → variant mapping;
- base picture reuse that breaks the limits in §5;
- restored (open) cells hard to tell from active tiles.
