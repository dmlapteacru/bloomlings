# 12 — Art & Content Pipeline

**Status:** LOCKED (2026-09-29)  
**Revision 2026-09-29:** picture library and finished-picture reveal (§7, §10, §12–§14; spec `001` FR-006/FR-007).

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
- small garden-world subjects drawn at board resolution (max ~14×16 cells);
- drawn in abstract color roles that map to the variant color groups: green, pink–purple, blue–cyan, brown–orange, and after expansion lime, red, indigo, gold;
- readable as the subject at board size with tile symbols on;
- hand-drawn, generated, or generated and then edited.

Scale:
- Levels 1–100: 100 different base pictures;
- 5000-level catalog: roughly 1000–1500 base pictures if each is used in at most ~5 levels (reuse through remapping, mirroring and different Source design).

Every base picture is manually reviewed for recognizability and gameplay usability before first use.

5000 levels do not require 5000 bespoke illustrations.

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
