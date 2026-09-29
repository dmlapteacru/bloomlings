# 05 — Level Structure

**Status:** DRAFT FOR LOCK

## 1. Core structural goal

A level is a dense picture/mosaic built from many full gameplay cells.

The grid may be rectangular internally, but the visible playable silhouette should often be irregular.

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

Inside active board mask:
- target initial occupancy roughly 75–95%;
- empty holes should be meaningful;
- board should not look sparse.

## 5. Board silhouette

Examples:
- flower;
- butterfly;
- watering can;
- leaf;
- tree;
- mushroom;
- fountain;
- bird;
- abstract ornamental motif.

## 6. Cluster shapes

Use:
- blobs;
- stepped edges;
- L-shapes;
- snakes;
- rings;
- pockets;
- nested shapes;
- branches;
- asymmetric masses.

Avoid default rectangular quadrants.

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
- near-duplicate recent layout.
