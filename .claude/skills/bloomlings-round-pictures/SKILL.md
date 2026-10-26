---
name: "bloomlings-round-pictures"
description: "Generate the next batch of Bloomlings round pictures (pixel-art cell matrices), numbered sequentially after the last one, check them against the level generation rules, and get the owner's approval on a numbered contact sheet. Use when the owner asks for new pictures, a picture batch, pictures for levels N+, pictures by theme, or replacements for rejected pictures or for missing calendar positions (content/rounds/needs.json)."
argument-hint: "How many pictures, optional themes, optional target levels or needs (e.g. '30 pictures, themes: garden, sea' or 'replace 477 481' or 'pictures for needs')"
user-invocable: true
disable-model-invocation: false
---

# Round pictures (step 1 of 2)

This is the first half of process §13 of
[`product/17_LEVEL_GENERATION_RULES.md`](../../../product/17_LEVEL_GENERATION_RULES.md): accept the picture
(§13.1). The second half, turning approved pictures into levels, is the skill `bloomlings-round-levels`.

Read 17 §2 (input picture and sizes) and §4 (the first three levels) before drawing. Everything below must follow
those rules; where this skill and 17 differ, 17 wins.

## What a picture is

- A rectangular cell matrix in the pilot palette: 16 colours, 4 per hero (`tools/gameplay-pilot-v9/palette.cjs`).
  The codes are `G P B A C D V Y O L M R K N S E`: Лист, Лепесток, Вода, Янтарь, Жемчуг, Земля, Слива, Солнце, Тыква,
  Лайм, Мох, Ягода, Румянец, Глубина, Небо, Кора. Two shades of one hero are different materials.
- `.` is an empty cell (not counted, it can be a hole or an island gap), `#` an impassable strip (a stream) and `b` a
  Sprig bridge site on that strip. A bridge picture has exactly two `b`. The cell below each `b` is its anchor on the
  near bank (the entry is at the bottom).
- The palette is finite and discrete: no semi-transparent edge shades. The colours of the matrix are the materials of
  the level.
- **N = coloured cells.** The tool counts N, the number of colours and the volume of each colour.

## Ranges (17 §2)

| Level | Cells N | Colours |
|---|---:|---:|
| 1 — crown | exactly 138 | 3, volumes 63 / 37 / 38 |
| 2 — ladybug 2C | exactly 240 | 4, volumes 120 / 97 / 19 / 4 |
| 3 — bird 3B | exactly 281 | 6, volumes 76 / 70 / 69 / 51 / 13 / 2 |
| 4–9 | 400–650 | 4–7 |
| 10–24 | 550–1000 | 4–8 |
| 25–60 | 550–1200 | 5–9 |
| 61+ | 550–1400 | 5–9 |

- **Where in the range (owner, 2026-10-26).** The floor is not a target. Spread sizes toward the middle of the range
  and at times near its top. Quality comes first: use the room for a better, more detailed picture, not for more
  background.

- **Compact pictures.** After L25, compact pictures of 200–399 cells with 3–6 colours are allowed.
- **Size mix.** Size does not grow with the level number. For every 30 pictures aim at about 6 of 200–499 cells,
  15 of 500–999 and 9 of 1000–1400, mixed in any order and independent of difficulty.
- **Colours.** Up to 9 colours. 10–11 colours only when the extra colour clearly helps the picture, about one in
  15–20 and at most 2 in any 20 consecutive pictures. More than 11 colours or 1400 cells is a separate extended set:
  do not make it here.
- **Geometry.** Islands, holes and asymmetry are fine. Every coloured cell must be reachable through non-wall cells
  (the tool checks this).
- **Readability.** At game scale (a phone, about 6 px per cell on the sheet) the silhouette must be recognisable,
  neighbouring colours distinguishable and important details not tiny. Reduce colours before handing over, keeping
  the picture recognisable.

## Numbering: always sequential

- Pictures are numbered in the order they are made: P0001, P0002, … The next batch continues after the highest number
  (`node tools/rounds/pictures.mjs next`). If 474 pictures exist, the next batch is 475+.
- **A rejected picture keeps its number** and is redrawn in place (`replace`, version + 1). Nothing is renumbered.
- **`forLevel`** defaults to the picture's number. It is only the stage the picture is checked against. The level
  skill may later place the picture on another level of a fitting class and stage.

## Steps

1. **Find out what to draw.**
   - How many pictures: the owner's number, or the count of positions in `content/rounds/needs.json` when the owner
     asks for pictures for the needs.
   - The themes: the owner's themes, else your choice. Vary the subjects (animals, food, plants, objects, scenes)
     and avoid repeats of recent titles (`pictures.mjs list`).
   - The stages of the target numbers (table above).
   - For needs: match each need's stage and class. A Hard or Super Hard needs a picture that allows dependencies:
     enclosed areas, colours shielded by other colours. A bridge introduction needs a picture with a stream (`#`) and
     two `b` sites.
2. **Draw the batch as a module**, e.g. `content/rounds/pictures/src/batch-0475.mjs`:
   - `export default {theme, pictures:[{title, theme?, forLevel?, rows:[...top first...]}]}`.
   - Draw by the skill `bloomlings-board-picture` (the owner's brief of 2026-10-26), in its verified mode: one object,
     flat colour areas, the space round the silhouette left empty (no coloured background), main details at least two
     cells thick. Check each module with `node tools/rounds/pictures.mjs verify <module>` before rendering it.
3. **Write and check.**
   - `node tools/rounds/pictures.mjs render content/rounds/pictures/src/batch-0475.mjs`, then
     `node tools/rounds/pictures.mjs check --status draft`.
   - Fix every `НЕТ` line (out of stage range, more than 11 colours, cut-off cells, wrong number of bridge sites) and
     redraw.
   - Treat the warnings with judgement: 10–11 colours (respect the window), tiny colour details.
4. **Look at them yourself first.**
   - `node tools/rounds/pictures.mjs sheet --status draft --out content/rounds/pictures/sheets/batch-0475.png`
     writes the PNG and a `.md` table next to it.
   - Open the PNG. Redraw anything that does not read as its title at this scale before showing it.
5. **Show the owner.**
   - Send the sheet PNG with `SendUserFile`, plus its `.md` table.
   - Ask for the decision by number: which to approve, which to redo, and what to change.
   - Never approve on the owner's behalf. Pictures stay `draft` until the owner says so.
6. **Record the decision.**
   - `node tools/rounds/pictures.mjs approve 475 476 …` for approved ones.
   - `node tools/rounds/pictures.mjs reject 477 --note "..."` for ones to redo, then redraw each with
     `node tools/rounds/pictures.mjs replace P0477 <module> [--index k]`, check, and show a new sheet of only the
     redrawn ones.
   - Repeat until the batch is settled. `approve` refuses a picture that fails the rules check.
7. **Commit** the batch module, the picture JSONs, the manifest and the sheet (content/rounds/pictures/…), then
   suggest the next step: `bloomlings-round-levels` for the approved pictures.

## The first three pictures

L1–L3 are fixed by 17 §4: crown 138/3, ladybug 2C 240/4, bird 3B 281/6, with the exact colour volumes above.
- If the owner gives their matrices, enter them as rows.
- Otherwise draw them to these exact counts.
- The level skill rejects them if the volumes differ.

## Do not

- Do not number out of sequence or reuse a number for a different picture.
- Do not tie a picture to a difficulty: Hard on 500 cells and Normal on 1100 are both fine (17 intro, §5).
- Do not add mechanics' objects other than the stream and bridge sites: the level skill places objects.
- Do not commit generated sheets of rejected drafts as approved, and do not edit approved pictures in place. A change
  is a `replace` with a new version and a new approval.
