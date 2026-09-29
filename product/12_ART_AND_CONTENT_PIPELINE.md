# 12 — Art & Content Pipeline

**Status:** DRAFT FOR LOCK

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

## 10. Board masks

Need a growing library of simple picture/mosaic masks.

For 150-level MVP:
- 150 distinct level masks/pictures;
- generated/authored hybrid acceptable;
- every mask manually reviewed for gameplay usability.

## 11. Backgrounds

Small reusable set of quiet garden backgrounds.

Theme rotation may occur by milestone but is not a level-map system.

## 12. Restoration reveal

Cleared cells reveal simple finished garden picture/pattern.

## 13. Asset scalability principle

Adding a new target variant should be relatively cheap:
- icon;
- palette;
- pod skin;
- tile skin;
- small character variant treatment.

It should not require a new full content pipeline.

## 14. Art acceptance checklist

Before shipping a new variant:
- exact target readable without color?
- distinguishable from same-family sibling?
- clear at 1× and 2×?
- clear in Waiting Slot?
- clear when 6 variants are on board?
- character does not hide tile state?
