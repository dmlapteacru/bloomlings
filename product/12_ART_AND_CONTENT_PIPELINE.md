# 12 — Art & Content Pipeline

**Status:** LOCKED (2026-09-29)  
**Owner amendment · 2026-10-26:** pictures of new rounds are made by the skill `bloomlings-round-pictures` under [`17_LEVEL_GENERATION_RULES.md`](17_LEVEL_GENERATION_RULES.md) §2.
**Revision 2026-09-29:** picture library and finished-picture reveal (§7, §10, §12–§14; spec `001` FR-006/FR-007).
**Revision 2026-10-06 (product owner):** pictures of 224–288 cells from Level 11 and up to 22×28 for the rare big
levels (spec `001` FR-008).

**Revision 2026-10-15:** the launch scope is 2500 levels. The new picture direction is authored, unusual scene compositions; the plan and first 24 briefs are in `content/pictures/art-direction.md`. Existing procedural images remain baseline content until replaced and visually reviewed. Local generation of gameplay remains offline on the owner's computer.

## 1. Locked visual direction

- flat 2D;
- light;
- calm;
- minimal;
- low eye strain;
- board-dominant;
- no 3D/isometric drift.

## 2. Four families, reusable animation rigs

We create four main Bloomling character families:
- Sprig;
- Bloom;
- Drop;
- Twig.

Each family gets one reusable core animation set:
- idle;
- emerge;
- move;
- restore/process;
- finish/despawn.

Target variants should reuse these rigs whenever possible.

## 3. Target-variant production

A target variant needs:

- unique color;
- unique tile icon;
- tile art;
- Source Pod treatment;
- family character palette/accent;
- optional tiny accessory/effect;
- accessibility validation.

It does **not** normally require a completely new character rig.

## 4. MVP variant set

### Sprig family
- Leaf / green
- Moss / teal

### Bloom family
- Flower / pink
- Violet Bud / purple

### Drop family
- Water / blue
- Dew / cyan

### Twig family
- Wood / brown
- Acorn / orange

Total:
**8 target variants**
using **4 base character families**.

## 5. Future expansion

Potential:
- Vine / lime Sprig
- Berry / red Bloom
- Mist / indigo Drop
- Bark / gold Twig

This brings global pool to 12 without multiplying animation cost by 3.

## 6. Variant readability test

Every pair likely to appear together must pass:
- grayscale/icon test;
- small-size test;
- color-distance test;
- Source Pod test;
- Waiting Slot test;
- moving-character test.

If two variants are confusing, art must change before content ships.

## 7. Board art

Each target tile:
- full cell;
- simple fill;
- one unmistakable symbol;
- minimal shading.

No hero faces in target tiles.

Tiles are the pixels of the level's picture: tile color = variant color, so the board reads as the subject.

Symbols stay simple, centered and of equal visual weight, so they do not break the picture read.

## 8. Character scale

Characters should be small enough not to obscure target cells.

Animation is decorative feedback on top of clear logical tiles.

## 9. Source Pods

Pod asset system must support:
- 8+ variants;
- count;
- link;
- lock;
- mystery;
- active/waiting state.

Build data-driven skins, not bespoke UI per variant.

## 10. Picture library

Levels are picture-first mosaics (`05_LEVEL_STRUCTURE.md` §5, `06_LEVEL_GENERATOR.md` §23).

Need a growing library of base pictures:
- small garden-world subjects drawn at board resolution (max ~14×16 cells until 2026-10-06; now 224–288 cells, and up
  to 22×28 for the big levels);
- drawn in abstract color roles that map to the variant color groups: green, pink–purple, blue–cyan, brown–orange, and after expansion lime, red, indigo, gold;
- readable as the subject at board size with tile symbols on;
- hand-drawn, generated, or generated and then edited.

Scale (owner amendment, 2026-10-15): **2500 unique compositions for the 2500-level launch**, one use per composition.
Resizing, mirroring and recoloring do not count as new art. Build the library in reviewable batches with stable
composition IDs; the three-scene pilot is only a style/readability check. The earlier 800–1000 reuse estimate is withdrawn.

Every base picture is manually reviewed for recognizability and gameplay usability before first use.

The new launch art must not repeat compositions. The current legacy catalog has not yet been replaced; enforce
registry coverage and one use per composition across L1–2500 when migrating. Future catalogs add new compositions.

## 11. Backgrounds

Small reusable set of quiet garden backgrounds.

Theme rotation may occur by milestone but is not a level-map system.

## 12. Restoration reveal

Cleared cells reveal the **finished version of the same picture**: the same subject in its restored look, as clean, bright, calm art without tile symbols.

- The finished look can be rendered automatically from the base picture's grid. This is the default for the long tail.
- Bespoke finished illustrations are optional, e.g. for curated levels or milestones.
- Restored (open) cells must stay clearly distinct from active target tiles, e.g. softer/lighter treatment, no tile frame, no symbol.
- On Win, the finished picture is shown in full with a short celebration and added to the Collection.

## 13. Asset scalability principle

Adding a new target variant should be relatively cheap:
- icon;
- palette;
- pod skin;
- tile skin;
- small character variant treatment.

It should not require a new full content pipeline.

Adding a base picture should also be cheap: a small grid of color roles plus metadata. Its finished look is derived automatically by default.

## 14. Art acceptance checklist

Before shipping a new variant:
- exact target readable without color?
- distinguishable from same-family sibling?
- clear at 1× and 2×?
- clear in Waiting Slot?
- clear when 6 variants are on board?
- character does not hide tile state?

Before a base picture enters the library:
- subject recognizable at board size with tile symbols on?
- recognizable in each intended role → variant mapping?
- restored cells clearly different from active tiles?
- no three extremely similar shades in one board?

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

- A picture is a cell matrix in a finite discrete palette (the pilot's 16 colours, 4 per hero) without semi-transparent
  edge shades; the matrix goes to the generator.
- Sizes by stage (17 §2): L1 crown 138 / 3, L2 ladybug 2C 240 / 4, L3 bird 3B 281 / 6; then 250–1400 cells with the
  stage colour ranges; compact 200–399 / 3–6 after L25; up to 9 colours, 10–11 rarely.
- Pictures are numbered in sequence, checked by `tools/rounds/pictures.mjs check`, shown on a numbered contact sheet
  and approved or sent back by the owner. Approved pictures are not tied to a level or class.
