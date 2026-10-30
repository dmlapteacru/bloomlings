---
name: "bloomlings-board-picture"
description: "Draw a Bloomlings board picture: a cell-grid pixel picture of one subject, as a concept or as a verified matrix with exact counts, palette HEX and per-material volumes, rendered to PNG straight from the matrix. Use when the owner asks to prepare, draw or check a board picture (клеточная картинка борда) for a level, or when bloomlings-round-pictures needs pictures drawn."
argument-hint: "Level, subject, mode (concept / verified), cells min–max, target, colours, palette, reference"
user-invocable: true
disable-model-invocation: false
---

# Board picture

The owner's drawing brief of 2026-10-26, turned into a skill. It says **how a board picture looks and how it is
checked**. Numbering, contact sheets and approval are the skill `bloomlings-round-pictures`, which uses this one to draw.
Size and colour ranges come from [`product/17_LEVEL_GENERATION_RULES.md`](../../../product/17_LEVEL_GENERATION_RULES.md)
§2 and §4; where they differ from a parameter given here, ask the owner.

## Parameters

Fill each from the request. If one is missing, take the default.

| Parameter | Meaning | Default |
|---|---|---|
| Level | the level number | — |
| Subject | one object, animal or plant, or a task card of the theme catalog | the owner's, else from the theme catalog `product/design/Bloomlings-picture-theme-catalog.md` (§2 families, §3 directions, §8 card) |
| Mode | `concept` or `verified` | `verified` |
| Cells | occupied cells, min–max | the stage range of 17 §2 (L1–L3: their exact counts and volumes, §4) |
| Target | the wished number of cells | the middle of the range; at times near the top. Never the floor by default |
| Colours | number of materials | inside the stage range of 17 §2 |
| Palette | the materials | the pilot palette (`tools/gameplay-pilot-v9/palette.cjs`): `G P B A C D V Y O L M R K N S E` |
| Reference | an attached picture | none |

## Look

Make an attractive silhouette that reads at once, for a calm casual game.

- The whole picture is equal square cells on one orthogonal grid. Every occupied cell is one flat colour of the chosen
  palette. The outer contour is stepped.
- **Use:** one main object; large, clear details; solid colour areas; well separated colours; a clean silhouette that
  reads at game scale.
- **Never draw a banned subject** (`tools/rounds/themes.json` `banned`): a geode (the owner, 2026-10-30).
- **Do not add:** gradients, textures or random shades inside areas; smooth outlines over the grid; unrelated background
  decor or sparkle clutter; text or frames inside the board; game mechanics, pods, the queue or slots.
- **Empty by default, environment where it belongs** (the owner, 2026-10-26). By default the space round the silhouette
  and in its holes is empty (`.`). Where it makes sense and makes the picture fuller (and above all the board fuller in
  play), the picture carries the subject's natural environment instead: the sea round a fish or a whale, the deep sea round
  a bathyscaphe, sky and sea round a lighthouse, a night sky round an owl, space round an astronaut, grass and sky round a
  grazing animal, an ice rink round a skater.
  - The environment is part of the work and of the cell count. Its cells count toward the stage range, so shrink the
    subject until subject plus environment fit (a 30×30 full rectangle is already 900 cells).
  - Draw it as one to three flat materials in clear areas: a horizon band, a sea and a sky, a ground strip, a round
    vignette or the full rectangle. A simple regular pattern (wave rows, a few big stars) is fine; noise, gradients and
    sparkle clutter are not.
  - Its colours must stand apart from the subject's edge colours, so the silhouette stays the clear centre.
  - Leave it out where it adds nothing (a cookie, a medallion, a mug), where the subject is its own container (a snow
    globe, a fishbowl), or where it would push the picture over its range.
- **Meme exception** (theme catalog §10): digits or a short sign that make the meme itself may be part of the matrix; their
  cells count like any other. Interface text, captions, watermarks and slogans stay out.
- Keep the 2–3 features the task card names as must-survive; if they do not survive the simplification, change the
  composition or the budget, not the caption.
- Main thin details are preferably at least two cells thick. A meaningful single cell, such as an eye, is fine.
- Shade with a second material of the same hue family on the far side, such as a darker band under a cap or the shadow
  side of a stem. Use it as a solid area, never as noise.
- With a reference, keep its cell style and the character of its colour areas, but draw the given new subject.
- Subjects that need a support (a branch under a bird, waves under a boat, grass under a mushroom) may keep it as part
  of the object, kept small.

## Mode `concept`

Make a visual variant. Give the cell and colour counts in its caption as **target**, until they are checked.

## Mode `verified`

1. **Matrix.** Write the picture as a rectangular matrix in a drawing module
   (`content/rounds/pictures/src/*.mjs`, format of `bloomlings-round-pictures`). Each position is empty `.` or one
   chosen material. Draw with the shape painter `tools/gameplay-pilot-v9/draw.mjs` (parts with `outline: false` for the
   flat look; background `() => '.'`) and the helpers in
   `content/rounds/pictures/src/helpers.mjs` (`fit` brings a material to an exact volume by trading edge cells, `put`
   places single cells, `ring4` closes a layer round another), or write rows by hand.
2. **Palette.** The palette is fixed with exact HEX by `palette.cjs` (normal mode).
3. **Count and check by program.** Run
   `node tools/rounds/pictures.mjs verify <module> [--cells a-b] [--colours a-b] [--target n] [--level n] --out <png>`.
   It reports, per picture:
   - the occupied cells, the materials really used, and the cells of each material with its HEX;
   - equal row lengths, no value outside the palette, and the volumes summing to the occupied cells;
   - the range checks (the given ones, else the level's stage of 17 §2 and the exact volumes of §4);
   - the distance from the target.
4. **Fix and recount.** If the result does not fit, correct the matrix and count again. Do not add meaningless cells
   just to reach a number.
5. **PNG from the matrix.** `verify --out` renders the final PNG straight from the accepted matrices. Every picture on
   one sheet has the same cell size, and the captions carry the counted numbers.
6. **Game scale.** Look at the PNG yourself (about 6 px per cell is a phone). Redraw what does not read as its subject.

## Result

- **Concept:** the image and the target parameters.
- **Verified matrix:** the PNG, the matrix with its palette, the counted cells and colours, and the volume of each
  material.
- If the matrix cannot be checked, say plainly that the result is a concept and do not state exact counts.
- Passability, the order colours open in, and the level's difficulty are **not** confirmed at this stage. The level
  skill `bloomlings-round-levels` checks them.
- For several pictures, use the same cell size on the comparison sheet.
- Once the owner approves a picture, `bloomlings-round-pictures` records it, and it lands in the one approved folder,
  `content/rounds/approved/` (`locked/` for levels 1–3, `pool/` for all others).
