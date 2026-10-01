# Contract: the kawaii Bloomlings (FR-032)

> **Replaced by spec 004 (2026-10-01).** The owner rejected the kawaii look. Spec 004 (`specs/004-character-art/`)
> replaces FR-032 with generated 2D characters whose shape is the variant symbol, and SC-006 with generated art files
> recorded as the project's own. The kawaii code (`BloomlingArt`) is removed.


The owner chose style B ("Kawaii") from a character sheet of six styles (Garden, Kawaii, Flat, Sticker, Storybook,
Plush) on 2026-10-01. This contract fixes how both builds draw it. The code is the engine-free
`client/Assets/Bloomlings/UI/Design/BloomlingArt.cs`, linked into the playtest.

## The look

`BloomlingLook(Family, Color, IconId?, Mood = Happy, Badge = true, Halo = false)` names one picture. Its `Key` holds
every field, so the same key always renders the same pixels.

| Field | Meaning |
|---|---|
| `Family` | the body: Sprig an egg, Bloom a round bulb, Drop a droplet, Twig a stump |
| `Color` | the variant color (or its muted or stuck shade) |
| `IconId` | the variant's `VariantInfo.IconId`; it picks the crest and turns the belly badge on. Null: the family's own crest and no badge |
| `Mood` | `Happy` (open sparkly eyes, a small smile), `Sleepy` (closed eyes), `Worried` (raised brows, a small frown), `None` (no eyes or mouth: a cosmetic expression draws the face) |
| `Badge` | draw the white belly badge when there is an icon |
| `Halo` | a white sticker edge around the whole figure (walkers on the board) |

## Parts, back to front

1. **Halo** (walkers only): the union of every solid part, grown by the outline plus 0.06 design units (at least
   2 px), in `char.halo`.
2. **Crest**, each part outlined then filled:

   | Icon | Crest |
   |---|---|
   | `leaf`, `moss`, `vine` (Sprig) | a stem and two round leaves, the body color lightened 0.3 |
   | `flower` (Bloom) | five petals around the top, the body color lightened 0.35 |
   | `bud` (Bloom) | a closed bud, the body color lightened 0.25 |
   | `berry` (Bloom) | a green calyx and stalk (`garden.leaf_2`, `garden.leaf_line`) |
   | `drop`, `dew`, `mist` (Drop) | none (accessories below) |
   | `log`, `bark` (Twig) | a stem and one leaf (`garden.leaf_2`, `garden.leaf_line`) |
   | `acorn` (Twig) | an acorn cap with a stalk (`char.cap`, `char.cap_line`) |

3. **Feet**: two small ovals, outlined, in the body color darkened 0.15.
4. **Body**: outlined, then filled with a radial gradient: the body color lightened 0.45 at the upper left, the body
   color at 55%, darkened 0.12 at the rim.
5. **Accessories**, outlined and filled in `char.sparkle`: Dew a four-point sparkle, Mist a small cloud.
6. **Blush**: two ovals in `char.blush` at 55%.
7. **Face** (by mood) in `char.ink`, with `char.sparkle` highlights in the eyes.
8. **Badge** (with an icon): a ring in the outline color at 45%, then a disc in `char.badge`.

The host draws the variant symbol (the `ShapeLibrary` symbol, or final icon art) over `SymbolBox` in
`SymbolColor(color)`. The symbol is not part of the picture, so it stays crisp at any size.

## Colors

| Value | Rule |
|---|---|
| body | the variant color; when its luminance is below 0.5, lightened 0.18 (pastel) |
| outline | the body darkened 0.45 (0.25 for near-black bodies), mixed 15% toward white; never black (FR-008) |
| symbol | the variant color, darkened in steps of 0.06 until it reaches 3:1 on `char.badge` (FR-024) |

New color tokens (`DesignTokens.Colors`, 7 tokens, 96 in all):

| Token | Value | Use |
|---|---|---|
| `char.ink` | `#2B2420` | eyes, mouth, brows |
| `char.sparkle` | `#FFFFFF` | eye highlights, the Dew sparkle, the Mist cloud |
| `char.blush` | `#FF8FA3` | cheeks (drawn at 55%) |
| `char.badge` | `#FFFDF7` | the belly badge |
| `char.cap` | `#8A5A30` | the acorn cap |
| `char.cap_line` | `#4A2E16` | the acorn cap's outline and stalk |
| `char.halo` | `#FFFFFF` | the walkers' sticker edge |

## Geometry (design units: x and y about −1..1, y up)

The design is drawn at `Fit` = 0.94 of the unit square, so the outline and the halo stay inside `ShapeRaster.Margin`.

| Part | Place |
|---|---|
| body | center y −0.237; Sprig 0.65 × 0.68 (egg), Bloom 0.70 × 0.67, Drop circle 0.67 with its tip at 0.80, Twig a rounded box 0.60 × 0.66 |
| eyes | x ±0.24, y −0.05, 0.13 × 0.16 |
| mouth | y −0.20, radius 0.055 |
| blush | x ±0.44, y −0.22 |
| badge | y −0.58 (`BadgeY`), radius 0.30 (`BadgeRadius`); the symbol fills 0.86 of it (`SymbolShare`) |
| feet | x ±0.20, y −0.88; the ground line is `FeetY` = −0.95 |
| outline | 0.035 design units (`OutlineWidth`), never under 1.3 px |

## Host API

| Call | Gives |
|---|---|
| `BloomlingArt.Render(look, size, topDown, premultiplied)` | `size` × `size` RGBA bytes; bitmaps take rows from the top and premultiplied alpha, Unity textures rows from the bottom and straight alpha |
| `BloomlingArt.SymbolBox(box)` | where the symbol goes over a picture drawn into `box` (y down) |
| `BloomlingArt.SymbolAnchors` | the same as 0..1 anchors with y up (Unity) |
| `BloomlingArt.OnCard(face)` | the picture's box on a pod or slot card face: as large as fits between the card top and the count pill (`CardPillShare` = 0.29 of the face from its bottom), so the pill never covers the badge |
| `BloomlingArt.OnCardAnchors`, `SymbolOnCardAnchors` | the same as anchors (Unity) |
| `BloomlingArt.Silhouette(family, icon?)` | the outer edge with its outline; the `char.*` shapes are this edge, so skins and hit areas follow the figure |
| `BloomlingArt.SymbolColor(color)` | the symbol's color on the badge |

- **Playtest:** `IPainter.Picture(key, render, box)` draws the picture, cached by key and quantized size
  (`AndroidPainter`: an `ARGB_8888` bitmap; `SkiaPainter`: an `Rgba8888` premultiplied image). `Visuals.Bloomling`
  draws it, marks `char.<family>`, `char.face` and `char.accent`, and draws the symbol. `Visuals.GroundShadow` puts a
  flat shadow under the feet.
- **Unity:** `ProceduralSprites.Bloomling(look)` bakes a sprite (128 px). `PodView`, `SlotRowView`, `BloomlingFigure`
  (workers, the win cheer, Home, the Wardrobe, the profile), Home's sitters and the splash use it, with the symbol
  image at the anchors above.

## Where each mood and option is used

| Place | Mood | Badge | Halo |
|---|---|---|---|
| exposed pod, flight to a slot | Happy | yes | no |
| pod still in its stack (muted color, FR-022a) | Sleepy | yes | no |
| working slot | Happy | yes | no |
| stuck slot (grey shade) | Worried | yes | no |
| walker on the board | Happy | yes | yes |
| splash | Happy | yes | no |
| Home, leaderboard, Wardrobe, profile | Happy (None with a worn expression) | no | no |
| win cheer (Unity) | Happy | no (variant crest only) | no |

## Tests (`BloomlingArtTests`)

- Every variant renders a visible figure, and nothing reaches the picture's edge even with the halo.
- The badge is `char.badge`, and every variant's symbol, also muted and stuck, reaches 3:1 on it.
- No icon, no badge.
- The eyes are open when happy or worried, closed when sleepy, absent with `None`.
- The four families' silhouettes differ, and the crest tells Flower from Violet Bud and Wood from Acorn.
- The `char.*` shapes follow the drawn figure (under 2% of pixels differ).
- Rendering is deterministic, and the key changes with every field.
- Premultiplied bytes never exceed their alpha.
- On a card, the badge clears the count pill.
