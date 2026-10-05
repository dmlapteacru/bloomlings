# Contract: the reference look (kit, components, recipes)

Every recipe here is drawn identically by the playtest (`playtest/design/Kit.cs`, painters) and the Unity client
(`UiKit`, views). Sizes are in reference units (`p.U(…)` / `UiKit.Units(…)`) or in fractions of the element's box.
Colors are tokens of `DesignTokens.Colors` (added to its `All` map) or derived with `Lighten`/`Darken`/`Mix`. The
reference image is `specs/005-reference-look/reference.jpg`; crops named below are regions of it.

## 1. Palette

### 1.1 Variant colors (core `VariantCatalog`, research D1)

| Variant | Color | | Variant | Color |
|---|---|---|---|---|
| Leaf | `#99D323` lime | | Water | `#3485E7` blue |
| Moss | `#0FB198` teal | | Dew | `#61DAE1` cyan |
| Flower | `#FF3B89` hot pink | | Wood | `#9B4904` brown |
| Violet Bud | `#7F3CC4` purple | | Acorn | `#CF7F20` amber |
| Vine (exp.) | `#FFFF3C` | | Mist (exp.) | `#94A8EC` |
| Berry (exp.) | `#810F00` | | Bark (exp.) | `#84794B` |

### 1.2 Material and UI tokens (new, `DesignTokens.Colors`, keys in brackets)

| Token | Key | Value | Use |
|---|---|---|---|
| `WoodLight` | `wood.light` | `#FBE2BC` | sign / rim face top (pale honey wood) |
| `WoodMid` | `wood.mid` | `#F1CD98` | sign / rim face bottom |
| `WoodGrain` | `wood.grain` | `#C99863` | grain streaks and lines (faint) |
| `WoodEdge` | `wood.edge` | `#DDB27C` | the slightly deeper bottom band of a plank |
| `WoodLine` | `wood.line` | `#8B5A2B` | sign / rim outline |
| `WoodDark` | `wood.dark` | `#8A5634` | pod frame face |
| `WoodDarkTop` | `wood.dark_top` | `#A86F45` | pod frame top light |
| `WoodDarkLine` | `wood.dark_line` | `#4A2A14` | pod frame outline |
| `StoneTop` | `stone.top` | `#FCE8C6` | stone block top light (light, warm cream stone) |
| `StoneFace` | `stone.face` | `#F1D5A8` | stone block face |
| `StoneLip` | `stone.lip` | `#D9B585` | stone block lower edge |
| `StoneLine` | `stone.line` | `#7E6844` | stone outline and (deep, dark) joints |
| `StoneMoss` | `stone.moss` | `#7DB24A` | moss patches |
| `ParchmentTop` | `parchment.top` | `#FFF8E8` | card / tray top |
| `ParchmentBottom` | `parchment.bottom` | `#F5E4C3` | card / tray bottom |
| `ParchmentEdge` | `parchment.edge` | `#EBCB9A` | aged edge band, inner border line, plate depth |
| `ParchmentLine` | `parchment.line` | `#B48552` | card outline |
| `ParchmentWell` | `parchment.well` | `#F3D7AB` | inset wells (jam row, sunk tabs) |
| `CreamFace` | `cream.face` | `#FCE7C8` | cream button / slot / booster face (peachy) |
| `CreamTop` | `cream.top` | `#FFF6E6` | cream face top, the domed middle, glyph halos |
| `CreamLip` | `cream.lip` | `#E6C69B` | cream lower lip |
| `CreamLine` | `cream.line` | `#C79F6F` | cream outline (soft tan) |
| `InkBrown` | `ink.brown` | `#3A2416` | sign letters, counts, glyphs on cream, row labels (the reference's near-black brown) |
| `InkTitle` | `ink.title` | `#6E3416` | card and sheet titles, the win and milestone signs (a warmer red-brown) |
| `InkBrownSoft` | `ink.brown_soft` | `#7B5A3A` | body text on parchment |
| `LotusFill` | `lotus.fill` | `#F7739F` | lotus petals (deep pink at their edges) |
| `LotusTip` | `lotus.tip` | `#FFE4EE` | lotus petal middles (near white) |
| `LotusLine` | `lotus.line` | `#D14F7A` | lotus outline |
| `BadgeGreen` | `badge.green` | `#245C34` | count badge disc |
| `LawnLight` | `lawn.light` | `#9CC842` | gameplay lawn (sunny yellow-green; owner's review: a little more saturated) |
| `LawnDark` | `lawn.dark` | `#64982D` | lawn shade, grass strokes |
| `FoliageDeep` | `foliage.deep` | `#1F4D17` | the gameplay garden's hedge and the bushes' shade (owner's review, FR-020) |
| `Foliage` | `foliage.mid` | `#3A8526` | the garden's bushes and leaves |
| `FoliageLight` | `foliage.light` | `#7DC443` | sunlit leaves and bush highlights |
| `IvyLeaf` | `ivy.leaf` | `#96D03C` | ivy / clover leaves on signs (yellow-green) |
| `IvyLine` | `ivy.line` | `#2F6A18` | ivy outline, the dark shadows between leaves, veins |
| `ButtonBlue` | `button.blue` | `#45A3EE` | jam Return / Bloom Burst buttons |
| `ButtonOrange` | `button.orange` | `#F6B021` | orange buttons ("Next" in the strip) |
| `RayLight` | `ray.light` | `#FFF4C8` | win light rays (alpha) |

`ButtonPrimary` becomes the reference's yellow-green `#62B83A` (top `#ADE162`, lip `#378F24`, line `#24661A`);
`GardenLook.Green` follows it. The spec 002 scrim `surface.scrim` becomes a warm brown, `#2A1708` at 50% (it was a cool
`#1E2430` at 55%), so the garden and every card's backdrop keep their warm hue; the jam sheet keeps 0.3 of it.

### 1.4 The backgrounds' saturation (the owner's note of 2026-10-04, FR-031)

The owner's backgrounds keep at most 70% of the animated heroes' mean HSL saturation (their frames' mean, 0.656, on the
pixels at least half opaque): `tools/heroanim/saturation.mjs` scales `win.jpg` (×0.66), `wardrobe.jpg` (×0.68) and the
gameplay themes (daylight ×0.72, orchard ×0.71, pond ×0.82; moonlit stays, at 59%), and `layers.mjs` the layered Home
as one scene by its garden's factor (×0.73, `layers.json` `saturation`), once, offline. The transform keeps a pixel's
lightness and hue (each channel moves toward `(max + min) / 2`); a second run changes nothing; a background added later
is brought down by running `node saturation.mjs` (or `layers.mjs` for the Home layers). The UI tokens and pictures, the
heroes, the 2D characters and the board keep their saturation (the owner, after the full ladder was tried: "only the
background").

### 1.3 Color sets (`GardenLook`)

| Set | Face | Top | Lip | Line | Label |
|---|---|---|---|---|---|
| `Green` | `#62B83A` | `#ADE162` | `#378F24` | `#24661A` | white, outlined `Line` |
| `Blue` (jam) | `ButtonBlue` | lighten 0.3 | darken 0.25 | darken 0.42 | white, outlined `Line` |
| `Orange` | `ButtonOrange` | lighten 0.3 | darken 0.22 | darken 0.42 | white, outlined `Line` |
| `Cream` | `CreamFace` | `CreamTop` | `CreamLip` | `CreamLine` | `InkBrown`, plain |
| `White` (round buttons, Petals pill) | = `Cream` | | | | `InkBrown` |

The level pill sets (`Blue`/`Lilac` for the gameplay level) are replaced by the wooden sign; Super Hard tints the sign's
letters `BadgeSuperHard` darkened 0.35 (so they keep their contrast on the pale wood) and keeps the SUPER HARD badge under
it.

## 2. Material pictures (`UiRaster`, engine-free)

`client/Assets/Bloomlings/UI/Design/UiRaster.cs`. Every function returns **straight-alpha RGBA bytes, rows from the
top**, `w * h * 4` long, anti-aliased edges (1 px), deterministic for the same arguments (hash noise, no `Random`).
Sizes are pixels. Common helpers: rounded-rect SDF, value noise / fbm, smoothstep, `Rgba.Mix`.

| Function | Picture |
|---|---|
| `Plank(w, h, radius, outline, WoodTone tone, int seed, float lipShare = PlankLip)` | A wooden plank: a pale vertical gradient top→bottom, horizontal grain (many fine, faint streaks: fbm stretched along x, plus 6–9 thin, faint, barely wavy lines and an occasional knot ellipse), a 1–2 px lighter bevel inside the top edge, only a slightly deeper band along the bottom (`lipShare` of h, `PlankLip` = 6% by default, 3% on the button rim; `WoodEdge` at 60%) and no dark lip, the outline. `WoodTone.Light` uses `WoodLight`/`WoodMid`/`WoodGrain`/`WoodEdge`/`WoodLine` (signs, rims); `WoodTone.Dark` uses `WoodDarkTop`/`WoodDark`/`WoodDarkLine` (pod frames). Two tiny nail dots, near the top at the left end and near the bottom at the right end, when `h > 48`. |
| `Frame(w, h, radius, border, WoodTone tone, int seed)` | The plank material as a ring of width `border` (hole transparent) with an inner shadow along the hole's top and a light inner edge along its bottom: the pod frame. |
| `Stone(w, h, radius, outline, int seed)` | A stone block of warm sandy stone: `StoneTop`→`StoneFace` gradient, a smooth surface (fbm mottling ±4%, rare faint speckles), a light bevel at the top, `StoneLip` along the bottom 14%, the dark `StoneLine` outline; `seed` varies the mottling and, on about one block in six, a soft tuft of moss (`StoneMoss`, feathered edge) over a corner. |
| `Tile(size, Rgba color, string iconId, TileStyle style)` | The candy tile (§3.1), square; `TileLipShare(style)` gives its lip for the kit's press. |
| `Grass(size, int seed)` / `Grass(w, h, int seed)` | A grass cell (`tile.grass`, §4.1): the board cell of the picture's background, a muted lawn square (`lawn.light`/`lawn.dark` mixed, a soft mottle, short dark and light blades on a 0.125 grid, a faint top shadow, a deeper rim) inset 2.5% (`GrassInset`) with a 10% radius over a deeper green; `GrassVariants` = 4 pictures, `GrassSeed(x, y)` picks one per cell so neighbors differ. Opaque. |

Each build caches pictures by `key + "@" + w + "x" + h` (sizes quantized up to multiples of 8 px).

### 2.1 The picture primitive

- Playtest: `IPainter.Picture(string key, Box box, Func<int, int, byte[]> render)` renders at the box's pixel size
  (quantized), caches, and draws stretched into `box` with linear filtering, honoring the alpha, clip and transform
  stacks. `SkiaPainter` uses `SKAlphaType.Unpremul`; `AndroidPainter` premultiplies before `CopyPixelsFromBuffer`.
  `PainterBase` declares it abstract. Pictures count as asset slots: callers `p.Mark(slotId)` as for shapes.
- Unity: `ProceduralSprites.Picture(string key, int width, int height, Func<int, int, byte[]> render, Vector4 border =
  default)` flips the rows (Unity is bottom-up), builds a `Texture2D` (RGBA32, bilinear, clamp, `DontSave`) and a
  `Sprite` (100 ppu, center pivot, FullRect, `border` for 9-slicing), cached by key and size. Planks and frames are
  rendered once at a reference size (for example 512 × 128 for a sign) and 9-sliced with a border of their radius; tiles
  at 128 px per side. Add to `client/DotnetCheck/UnityStubs.cs` only what the stubs miss.

## 3. Components

Playtest names are `Kit.*`; the Unity twin in `UiKit` takes the same name. A component draws only through the kit.

### 3.1 Candy tile — `Kit.CandyTile(p, box, VariantId variant, TileStyle style, TileState state = Normal)`

Reference crops: the board (gameplay screen) and the "Target Variants" strip.

- Shape: a square, corner radius 7% of its side (board: nearly square, so the board reads as one continuous mosaic) or
  20% (sticker), at least 3 px.
- Face (pillowy satin): vertical gradient from `color.Lighten(0.36)` (top) to `color` (60%) to `color.Darken(0.14)`
  (bottom).
- Lip: `color.Darken(0.28)` along the bottom 7% (`UiRaster.TileLipShare`; none on `Flat`), under the face.
- Outline: crisp and solid, 2% of the side (board) or 2.6% (sticker), min 1 px, in `color.Darken(0.45)` (board) or
  `color.Darken(0.5)` (sticker) at full alpha. Neighboring board tiles are parted only by their two outlines (about 3–4%
  of a cell), as in the reference.
- Gloss: no white band or specular dot (the jelly look is gone): a faint white band from 6% to 24% of the face height,
  inset 10% left/right, alpha 0.18 (board) or 0.16 (sticker) → 0 downward, and a lighter bevel (`color.Lighten(0.45)` at
  0.8, 2.5% of the side) just inside the top and the left edges, fading out by 60% of the side, so the tile reads as a
  raised cushion.
- Symbol, **board style**: the variant symbol (`ShapeLibrary.SymbolId(iconId)`) in a box of 60% of the side (the shape
  itself spans about 42% of the tile, as the reference's beads), centered 2% above the middle, as an embossed bead: a dark
  rim (the symbol grown by 4.5% of its box) in `color.Darken(0.5)`, a fill from `color.Lighten(0.1)` (top) to
  `color.Darken(0.3)` (bottom) and a bright white specular on its upper left (alpha 0.7, the sticker's highlight
  ellipse). Below 28 px per tile the symbol is a flat `color.Darken(0.32)` fill.
- Symbol, **sticker style** (pods, slots, jam row, Collection, the strip): the symbol at 66% of the side with no light
  halo: a crisp outline (the symbol grown by 3.5% of its box) in the icon's own dark tone (its bottom color darkened
  0.32), a fill from `icon top` to `icon` (per variant, §3.1.1), a white highlight ellipse clipped to the symbol's upper
  left (alpha 0.4), and the variant's detail.
- States: `Dimmed` (waiting pods, the ones under the exposed pod in its column, §3.7) mixes face and symbol 45% toward
  `ParchmentBottom`; `Grey` (stuck) uses `color.Grey()`; `Pressed` sinks the face into the lip (`Kit.Block` press);
  `Mystery` uses `TileMystery` with a white "?" (`tile.mystery`) and no symbol.

#### 3.1.1 Sticker icon colors and details

| Variant | Icon fill (top → bottom) | Detail |
|---|---|---|
| Leaf | `#5FB84A` → `#2F8C32` | a lighter midrib and two side veins `#8ED86A` |
| Moss | `#22B79C` → `#0D7C68` | three faint dimples (alpha 0.2): a nearly uniform cushion |
| Flower | `#FFD2E2` → `#F79AC0` | a yellow center `#FFD35C` with an orange dot |
| Violet Bud | `#C58BF5` → `#8E4BD8` | a lighter middle line and two green sepals `#5BAA3A` at the base |
| Water | `#2F8EF5` → `#0B4FB0` | a white curved highlight on the left |
| Dew | `#E6FFFF` → `#9EEFF3` | a white sparkle star at the top right, outlined thinly in its line color |
| Wood | `#C47A3C` → `#7A3A12` | two concentric darker rings on the stump top |
| Acorn | `#E39A4A` → `#A35A18` (nut), cap `#7A4A22` | a cap line and a tiny stem |
| Expansion | `color.Lighten(0.2)` → `color.Darken(0.15)` | none |

Every sticker icon is outlined (4% of its symbol box, `UiRaster.StickerLine`) in its tile's own color darkened 0.45
(`UiRaster.StickerLineDarken`), Dew's nearly white icon in `#1F8D95`, so each icon contrasts with its face (owner's
review: the Water drop no longer repeats the face's gradient and Dew no longer reads as a faint ghost).

The symbols themselves are redrawn in `ShapeLibrary` (research D13) so that each reads as in the reference strip while
the two variants of a family keep different silhouettes: leaf (a broad almond, about 1.5:1, with a barely visible
stem), moss (a round 1:1 cushion with about eleven soft scallops), flower (five clearly separated round petals with
deep notches), bud (a chunky rounded bud with three short tips and two sepals, no stem), water (pointed teardrop), dew
(round droplet, its sparkle as a separate part), wood (stump: a short cylinder with a ringed top), acorn (cap and nut).
The mask difference between any two symbols at 48 px stays above 0.08 (`ShapeLibraryTests`).

#### 3.1.2 Board gem icons (owner's review, spec 005 FR-026)

The reference's board icons are bold "gems" (crops `zz-tiles1.png`, `zz-tiles2.png`): on every board tile the symbol
sits at about 56% of the tile (outline included), centered on the face (above the lip), with
- the gem silhouette `ShapeLibrary.GemSymbol(iconId)` in a shape box of `UiRaster.GemBox` = 0.64 of the tile (the
  silhouette spans about ±0.8 shape units, so its fill is about 44% of the tile);
- a thick dark outline: the silhouette grown by `UiRaster.GemLine` = 6% of the tile (at least 1 px) in a deep,
  saturated shade of the tile: `Vivid(color.Darken(0.42), 1.5)`, over a faint drop shadow (the silhouette moved down
  0.05 shape units, the outline color at 0.22); `UiRaster.Vivid(c, k)` pushes each channel away from the color's grey
  `g = 0.3R + 0.59G + 0.11B` by `k` (`g + k·(ch − g)`, clamped), as `GardenLook.SpecialFace`;
- a vivid fill in a shade of the tile color (the reference's gems are vivid shades of their tile, never mixed toward
  black): `Vivid(color.Darken(0.12), 1.6)` (`UiRaster.GemShade`, `GemSaturate`) when the color's HSL lightness is above
  0.55, else `Vivid(color.Lighten(0.25), 1.4)`, from that shade lightened 0.18 at the top to darkened 0.06 at the
  bottom;
- the gem's inner line in the outline color at 0.55 (`ShapeLibrary.GemDetail`: the leaf's midrib, the flower's center
  ring, the stump's front rim) and a lighter or darker part (`GemLight`: the flower's middle, the stump's top, dew's
  sparkle, the fill's top lightened 0.22; `GemDark`: the acorn's cap, the fill's bottom darkened 0.2);
- a strong white highlight on its upper left (alpha 0.6, the sticker's highlight ellipse) and a specular dot
  (radius 0.10 shape units at (−0.32, 0.42), alpha 0.95);
- no light copy or bead. Below 28 px per tile the gem is its silhouette in the outline color mixed 40% toward
  `color.Darken(0.32)`.
The gem silhouettes are simple and chunky so they read at 40 px, and keep the distinct silhouettes of research D10:
leaf a chunky almond tilted to the upper right; moss a round cushion a little wider than tall with nine soft scallops and
a flatter foot; flower five round petals around a smaller middle (deep notches); bud a tulip with three short tips on a
round body; water a tall upright pointed drop; dew a round droplet leaning right (its soft tip up-left) with a sparkle
inside; wood a stump with its top ellipse and two roots; acorn a wide cap with a stem over a nut pointed below. Every
pair of gem masks differs by more than 0.08 at 48 px (`ShapeLibraryTests`). The finished picture (`TileStyle.Flat`)
uses the same gem icons.

### 3.2 Wooden sign — `Kit.WoodSign(p, box, text, TypeStyle style, SignDecor decor)`

Reference crops: the gameplay top bar, "Level Complete!", the Wardrobe banner, the Home "LEVEL 88" plaque.

- `UiRaster.Plank` (Light: pale honey wood) filling `box`, radius 28% of the height, outline 2.5% of the height (min
  2 px), and a soft shadow below (`Kit.SoftShadow`, `GardenShadow` alpha 0.22, offset 7% of the height).
- Letters: `InkBrown` with a light emboss (`TextLook.Plain`-like, emboss `WoodLight.Lighten(0.4)`), centered, max width
  82% of the plank, and never under the leaves on its ends: with the owner's ivy picture (D5) at most the plank's width
  less 1.25 × its height, with `SignDecor.Flowers` at most the width less 1.28 × its height (Unity: label margins of
  0.625 h and 0.68 h); the win and milestone sign (`SignDecor.Flowers`) uses `InkTitle`.
- `SignDecor.Ivy`: clover/ivy clusters overlapping both ends (gameplay level sign, Wardrobe/Store banner): six clovers
  whose three leaflets are pointed lenses with a faint midrib (`IvyLeaf.Darken(0.35)` at 0.45), in yellow-green
  `IvyLeaf` shades (±0.2) with a light top-left side (`IvyLeaf.Lighten(0.3)` at 0.5) and a thin outline of
  `IvyLeaf.Darken(0.35)` at 0.7 over a soft `IvyLine` shadow at 0.5 between them (no heavy dark outline), gathered in two
  groups at the top and bottom corners of the plank's end with one leaf bridging them; each end's cluster (or the owner's
  picture, D5) fills a square 1.25 × the sign's height on its middle line, centered 0.04 × the height outside the
  plank's end, so it reaches 0.665 × the height beyond it (`GardenLook.IvyBox`, `IvyReach`; Unity drew it 0.06 × the
  height inside the end until 2026-10-04; `GardenLook.SignExtent` is the plank with its leaves). `SignDecor.Flowers`:
  lush clusters at the top-left and the bottom-right ends (win sign; `GardenLook.FlowerBox`), 1.35 × the
  sign's height: five big almond leaves (0.36–0.5 of the cluster, fanned from up-left to down-left, `GardenLeaf1/2/3`
  with `IvyLine` veins) and two white five-petal flowers (0.34 and 0.26 of the cluster) with yellow centers.
  `SignDecor.None`: Home plaque, card headers.
- Never a touch target.

### 3.3 Buttons

- **Primary** (`Kit.PrimaryButton`): the green set's face (spec 003 `Kit.Face`) with the smooth gloss: a band of the
  set's top lightened 0.35, from 4% to 46% of the face, inset 3%, alpha 0.35 → 0, feathered in three steps (inset +0,
  +2%, +4% at a third of the alpha each) so no edge shows, and a thin white shine along the straight part of the top edge
  only. A uniform pale wood rim instead of the cream plate: `UiRaster.Plank` (Light, `lipShare` 0.03, so no dark bottom
  band) behind the face, the face inset by 8.5% of the height on every side, outline `WoodLine`, a soft shadow. White
  label outlined in the set's line with a lighter extrusion (`TextLook.OnGloss`: 0.6 × `LabelExtrudeEm`). Pressed: the
  face sinks (existing press depth) and darkens by 8%. Decorations (leaves and flower at the corners) stay.
- **Secondary** (`Kit.SecondaryButton`): the cream set on a cream plate (`ParchmentEdge` depth), brown label, optional
  brown glyph on the left (Restart's ⟳).
- **Round / squircle icon buttons** (`Kit.RoundButton`, `Kit.IconFace`): a single domed cream cushion: the face
  `CreamFace` → `CreamFace.Darken(0.04)` (peach toward the edges) with a lighter `CreamTop` middle feathered in from 8%
  of the size (three steps, no inner ring, no dish), a `CreamLip` lower edge (7% of the size), a soft tan `CreamLine`
  outline (2%), a soft shadow (`Kit.SoftShadow`); brown glyph (`InkBrown`) at about 46% of the size with a thin `CreamTop`
  halo all around it (the shape grown by 0.06). Pause and speed in the top bar are squircles (radius 34% of the height)
  of the same height; Settings, back and close are circles. Close is cream with a brown ✕ (no longer red).
- **Speed pill** (`Kit.SpeedPill`, replaces `DarkPill`): the cream squircle style, as tall as Pause and wider, with the
  speed text ("1×" or "2×", `InkBrown`) and the `ui.fast` glyph (▶▶: a solid triangle and a notched chevron, about 42%
  of the pill tall, as tall as the digits) after it, in the cream halo.
- **Choice button** (`Kit.ChoiceButton(p, box, ColorSet set, iconDraw, label, cost)`, jam): a rounded rectangle
  (radius 22% of its height) in the green or blue set with the glossy face, the icon (its box 50% of the height, so the
  icon itself is about 44% of the face as on the reference; its center at 36% of the face) in the upper half, the white
  outlined label below it (font scaled to 21% of the button's height, centered at 78% of the face, `TextLook.OnGloss`),
  and a cost pill (§3.4) centered on its bottom edge, overlapping by 40% of the pill's height.
- **Secondary button glyph**: the brown glyph's box is 70% of the face's height, so the glyph stands a little taller than
  the letters, as the reference's ⟳ on "Restart Level".
- Orange (`GardenLook.Orange`) is available for a highlighted secondary call to action.
- Disabled buttons use `ColorSet.Disabled()` and alpha 0.55 as before.

### 3.4 Pills and badges

- **Count badge** (`Kit.CountBadge`): a `BadgeGreen` disc with a 10% white ring and a 3% `GardenShadow` outline; white
  digits (`type.badge` size scaled to the disc). Booster tiles place it at the bottom-right corner, overlapping by a
  third.
- **Cost pill** (`Kit.CostPill(p, box, Cost cost)`): cream (`CreamFace` → `ParchmentBottom`), `CreamLine` outline,
  soft shadow; contents: the lotus and the price (`InkBrown`), or a green ▶ square and "Free", always icon first, or
  "×N" charges in bigger digits (66% of the pill's height), after the booster's icon (80% of the height) on a jam choice.
- **Petals pill** (`Kit.PetalsPill` / `UiKit.PetalsPill`, both on the kit's `PetalsPillParts`; the owner, 2026-10-03:
  "at 0 it shows crooked, somewhere in the middle, the lotus itself too far left"): the cream style; the pill fits its
  amount inside its layout box (at the box's right end, as in the page header; `align` 0.5 centers it, as in Home's
  header row since the owner's request of 2026-10-04, where `decorate` adds the main buttons' leaves and flower on its
  corners, §6.4): the lotus
  (0.92 of the height) fully inside its left end (0.07 of the height in), the amount in `InkBrown` at half the height
  right after it (0.07 gap), left-aligned for every length, then 0.38 of the height of cream, or the green round "+"
  (the pill's height) on the right end, reaching 0.2 of the height beyond it. The width is measured with every digit
  a "0" (`PetalsPillParts.WidthText`), so a counting amount keeps its pill; a very long amount takes the whole box
  before its digits shrink. Before, the pill took its whole box, the lotus overlapped its left edge by 10% and the
  amount sat centered in what was left, so a short amount floated in the middle.
- **Lotus** (`Kit.Petal`, shape `currency.petal` redrawn as a compact lotus bud): a tall almond center petal (about 90%
  of the height), two side petals curving out and two small back petals over a small base; each petal deep pink
  (`LotusFill`) at its edges and near-white (`LotusTip`, `currency.petal.tips`: the petal shrunk by 0.12 from 20% to 85%
  of its length, over a softer pink band) in its middle, the middles parted by deep pink edges; `LotusLine` outline.

### 3.5 Surfaces

- **Parchment** (`Kit.Paper`, all cards, the jam sheet, the tray panel, the slot band): `ParchmentTop` →
  `ParchmentBottom` gradient, a warm aged band over the outer 6% of the shorter side (`ParchmentEdge` at about 0.4 at
  the outline fading inward, in fine steps so no ring shows), a thinner `ParchmentLine` outline (0.8 × the frame width
  token), a thin `ParchmentEdge` inner line 1.2% of the shorter side inside it, a soft shadow; no wood frame.
- **Well** (`Kit.Well`): `ParchmentWell` with an inner shadow along the top and a `ParchmentEdge` outline (jam slot
  row, sunk tabs, empty plates).
- **Card** (`Kit.Card`): parchment; a title in `type.title` `InkTitle` (no green band) or a `WoodSign` header when the
  screen says so (the milestone); the cream round close button over the top-right corner, on the top card only
  (a card covered by another, as Pause under Settings, shows none). The Wardrobe and the Store are full-screen pages
  under the page header (§6.5, §6.6), not cards.

### 3.6 Board furniture

- **Stone border** (`Kit.StoneBorder(p, gridBox, cell)`): blocks of `UiRaster.Stone` around the grid (outline 3.5% of
  the block's shorter side), thickness 0.42 cell, lengths alternating 1.0 and 0.8 cell, nearly rectangular (rounded 14%;
  the corners are square blocks 0.42 × 0.42 rounded 30%), a thin dark joint of 0.04 cell between blocks (`StoneLine` at
  alpha 0.45), seeds by position so the border never flickers. A 0.04 cell dark gap
  (`GardenLook.BoardGap` = `LawnDark.Darken(0.55)`) between the stones and the tiles, which also shows as the thin dark
  lines between tiles.
- **Garden Entry** (the owner's choice "B" of 2026-10-05, FR-034; slot `board.entry.arch`): a small stone arch set in
  the border beside each entry cell (`Kit.EntryArch`, Unity `BoardView`'s arch images; `UiRaster.EntryArch`). Its
  picture is `BoardLayout.ArchWidth` × `ArchHeight` (1.12 × 0.86) cells drawn upright for a bottom entry, its middle
  `ArchInset` (0.18) cell from the door toward the board, turned clockwise by `BoardLayout.ArchOf` (0° bottom, 180° top,
  90° left, -90° right); in cells from its top-left: the grid's edge at 0.36, the border's outer edge at 0.82. Two
  `stone.*` pillars 0.15 wide carry an arch band (inner radius 0.29, outer 0.44, spring line at 0.53) with four keystone
  joints and a `stone.line` outline, round a dark opening (`garden.shadow` darkened 0.6 at its top, 0.15 at its foot,
  `ray.light` glowing at its foot) where two white eyes with `ink.brown` pupils peep; seven `ivy.leaf` leaves with
  `ivy.line` veins climb it and a five-petal `petal.fill` flower with a `petal.center` heart sits on the keystone; a
  soft `garden.glow` light (0.32) is round it and a `garden.shadow` ellipse (0.3) under it. It lies over the border and
  the entry cell's foot (at most 0.36 cell, the symbol stays readable) and under the walkers, which set off from the
  border there (`BoardLayout.Door` / `DoorOf`, both builds). The big arch under the board (9 sandy blocks on two piers
  with its own room in the board's fit, slots `board.arch` and `tile.entry`) was retired on 2026-10-03 at the owner's
  request ("What is the arch under the board in gameplay? Remove it.") and stays retired.
- **Board layout** (`BoardLayout.Fit(area, width, height, maxOuterWidth)`, engine-free, both builds): the cells take the
  largest size that fits the grid, the border (`Rim` = 0.46 cell) and 0.2 cell of lawn on the left and right in the
  area, with the border's outer box at most `maxOuterWidth` wide; the group is centered in the area. Entries take no
  room (they took 1.8–2.2 cells of an arch and its lawn per entry side before 2026-10-03), so the board grew by about
  15% (a 9 × 10 board's cells from about 79 to 92 px at 1080 × 2340); the small arches of 2026-10-05 stand in the border
  and take no room either. Entries may be on any side, several per level.
- **Lawn**: the gameplay backdrop scene becomes a lawn (§4.2).
- **Pedestal** (`Kit.StonePedestal(p, box)`, `UiRaster.Pedestal`): a warm grey-beige ellipse-topped stone drum that
  holds its own over the owner's painted gardens: the top ellipse `StoneTop.Mix(StoneFace, 0.4)` (lighter toward the
  front) with a ring joint at 0.72 of its radius, radial joints outside it and one hairline crack; the side shaded from
  `StoneFace.Darken(0.06)` to `StoneLip.Darken(0.12)` in two courses of staggered blocks (`StoneLine` joints at alpha
  0.55, 1.5% of the height wide) with a crack in the upper course, a darker lip band and `StoneMoss` tufts on about a
  third of the base rim; a `StoneLine` outline 2.5% of the height around it and along the top's front edge; a soft
  `GardenShadow` (0.22) ellipse under it (`Kit.StonePedestal`). It returns the top ellipse's box (where the heroes
  stand). Win (unless the owner's win picture paints the stage, §6.3), Home (without the owner's Home picture) and
  Wardrobe.

### 3.7 Tray pieces

- **Pod** (`Kit.Pod(p, chip, variant, count, look, waiting)` over `Kit.PodFrame`; Unity `UiKit.GridPod`): one pod of
  the tray's grid (§6.1, FR-021 as amended on 2026-10-03; the owner's choice "E" of the same day: the icon first, the
  count small in a corner). A stack's pods stand one after another in its column and are never drawn on each other.
  Each pod's place is at least `PodChip.MinAspect` = 1.45 times as wide as it is tall, and `PodChip.In(place)` places
  its parts:
  - **Frame** (`PodChip.Frame`): `PodChip.Aspect` = 1.3 times as wide as the place is tall (the place's width when it
    is narrower), centered in the place. `UiRaster.Frame` (Dark, `mat.wood.dark`) fills it, with a radius of 20% of its
    height and a border of `PodChip.Border` = 9% of its height, over a soft `GardenShadow` shadow. There is no handle.
  - **Panel** (`PodChip.Inner`, the frame inset by the border): the variant's color lightened 0.5 at the top and 0.8
    at the bottom (the reference's lime, pink, sky blue and orange panels). A hidden mystery pod keeps plain cream; a
    locked pod uses `StateLockBg`.
  - **Icon** (`PodChip.Icon`): the owner's detailed icon of the variant (§3.11, `tile.icon.<id>`) alone, with no candy
    tile under it, over the panel's middle. Its box is `PodChip.Tile` grown by `PodChip.IconGrow` = 4% of the pod's
    height at each side, so it reaches a little over the panel into the border.
  - **Tile** (`PodChip.Tile`): a square `PodChip.TileShare` = 1.04 times the panel's height, centered on the panel.
    The sticker tile (§3.1) is drawn there for a hidden mystery pod (its "?"), and for a variant whose owner picture
    is missing. A locked pod's padlock stands in its middle. Flights start from it.
  - **Count** (`PodChip.Count`): small digits over the panel's bottom right corner, their middle
    `PodChip.CountInset` = 0.42 of their type size in from the corner, their type size `PodChip.CountShare` = 36% of
    the pod's height, at most half the panel wide. `type.count` in `InkBrown`, with no "x", in a white outline of
    `PodChip.CountOutlineEm` = 0.14 em, so they read over the icon (`PodChip.CountLook`).
  - **"+N" disc** (`PodChip.Badge`): on the last shown pod of a column deeper than the tray shows (§6.1), a count badge
    (§3.4) `PodChip.BadgeShare` = 44% of the pod's height. Its center is 12% of the height inside the frame's top
    left corner, away from the count.

  The pod's states:
  - **Exposed** (depth 0): bright, and the only pod that takes a tap. Its touch box is the box grown about its center
    to `size.touch_min`, and may reach over the waiting pod under it.
  - **Pressed**: the frame sinks by 4% of its height, carrying its icon (or tile) and count. Its shadow shortens, a
    `GardenShadow` shade darkens it, and a sticker tile sinks into its lip.
  - **Waiting** (depth 1 and deeper, the pods under the exposed one in its column): the same parts, muted but
    readable (spec 001 FR-013). The wash is lighter (the color lightened 0.74 at the top to 0.9 at the bottom), and a
    `ParchmentBottom` veil at 0.4 covers the frame and the panel. The shadow is lighter, the icon is at
    `PodChip.WaitingIconAlpha` = 0.7 (a stand-in sticker tile is `Dimmed`, §3.1), and the count is softer
    (`InkBrownSoft` mixed 30% toward `ParchmentBottom`). A waiting pod is shorter than the exposed one, so its frame,
    icon and count are smaller too. A waiting hidden mystery pod shows only its veiled "?" tile and its count.
  - **Locked**: the padlock (`ui.lock`, 62% of the tile's side) in the tile's middle on the grey panel, with the
    softer count.
  - **Mystery**: the lilac mystery tile and the count.
  - **Connected**: the link bar (`pod.link`) joins frames that stand side by side in one row. Each connected group of
    the tray has its own color (`state.link`, then `state.link_2` and `state.link_3`). A group whose shown members lie
    on different rows marks each member with a ring of its color instead (§4.1).
- **Waiting Slot** (`Kit.SlotPlate`): a cream plate (raised: `CreamTop`→`CreamFace`, `CreamLip`, `CreamLine`, radius
  20%); filled: the sticker tile at 72% of the face's width (at most 58% of its height), 10% below the face's top, and
  the count below it; empty: a dashed rounded inner
  outline (`CreamLine` alpha 0.8, dash 9%/6% of the side) on a slightly sunk face; stuck: grey tile + the hourglass
  badge; danger: the dashed outline in `StateDanger` with "!"; extra slot: the green "+" badge; locked: grey face with
  the lock.
- **Booster tile** (`Kit.BoosterTile`): a cream squircle (radius 26%) in a cream-white bezel with a faint silver tint
  (`GardenLook.BoosterRim` = `CreamTop.Mix(StateStuck, 0.25)`), a cream lip (`GardenLook.BoosterLip` =
  `CreamLip.Mix(StateStuck, 0.3)`) and a soft tan outline (`GardenLook.BoosterLine` = `CreamLine.Mix(StateStuck,
  0.35).Darken(0.1)`), never a grey keycap; the booster icon (§3.8) in a box of 74% of the tile (the icon itself
  about two thirds of it), the count badge or the cost pill; selected:
  the existing glow ring and lift; disabled: greyed. The Store's row tiles use the same colors.

### 3.8 Icons (`ShapeLibrary`, multi-part icons drawn by `Kit.BoosterIcon`)

| Icon | Look |
|---|---|
| Extra Slot | a blue disc (`#3E9BEA`, outline darker) with a white bold "+" |
| Shuffle | two curved arrows chasing each other, orange `#F2A33A` and green `#57B847`, white outline |
| Return | a fat arrow pointing left whose tail bends gently down to the right, yellow `#FFC23D` with an orange `#E08A1E` outline |
| Bloom Burst | a five-petal flower, pink `#F58CC8` petals, yellow center |
| `ui.fast` | ▶▶ in brown: a solid triangle and a notched chevron, each spanning 80% of the glyph's height |
| `ui.back` | a brown left arrow |
| `ui.restart` | a circular arrow (existing, brown on cream) |

### 3.9 Decorations

- **Ivy cluster**, **flower cluster** (§3.2): `ShapeLibrary.IvyLeafSdf`/`IvyVeinSdf` clovers in `IvyLeaf` shades with
  `IvyLine` outlines and midribs; `ShapeLibrary.ClusterLeafSdf`/`ClusterVeinSdf`/`ClusterFlowerSdf` big leaves and white
  flowers (`GardenFlower`) with yellow centers; never touch targets.
- **Soft shadow** (`Kit.SoftShadow(p, box, radius, alpha, offsetShare)`): every raised element (round buttons, signs, the
  button rim, slots, pills, badges, jam choices) casts four `GardenShadow` layers, each grown by 1.5% of the shorter side
  at a quarter of the alpha, so the shadow is soft and never reads as another lip.
- **Light rays** (win): 10 soft wedges from behind the heroes' bodies (`RayLight`, five strokes each at alpha 0.09 out
  to the full radius and 0.11 to 62% of it), slowly turning (0.05 turn per second), over a soft radial glow of eight
  faint `RayLight` discs (alpha 0.035, radii 6% to 48%), so no disc edge shows.
- **Falling petals** (win): 10 pink petal shapes (`LotusFill`, `LotusTip`) drifting down and swaying.

### 3.10 The owner's icon and leaf pictures (owner's review, FR-027, pictures.md D)

Both builds draw the owner's picture instead of the drawn icon or leaves when its file exists, in the drawn version's
box (aspect kept), and the drawn stand-in while it is missing. Names: `OwnerPictures.BoosterIcon(id)` =
`booster-{id}` in `OwnerPictures.IconFolder` (`Art/Icons/Resources/Icons/`); `OwnerPictures.Ivy` (`ivy`), `Flowers`
(`flowers`), `ButtonLeaves` (`button-leaves`), `LogoLeaves` (`logo-leaves`) in `OwnerPictures.DecorFolder`
(`Art/Decor/Resources/Decor/`); `OwnerPictures.SlotOf` gives their slots (`booster.{id}`, `ui.sign.ivy`,
`ui.sign.flowers`, `ui.deco.garden`, `ui.logo.wood`).

- Playtest: the folders are embedded as `icons/` and `decor/` (`playtest/android`, `playtest/preview`); the painter's
  names are `PainterBase.IconPrefix + name` (`icon/booster-shuffle`) and `DecorPrefix + name` (`decor/ivy`).
  `Kit.OwnerPicture(p, name, box, mirror, turn)` draws one; mirroring is `IPainter.PushSquash(-1, 1, cx, cy)` (turned
  half way: `(-1, -1)`), which both painters' canvases apply and `PainterBase` keeps hit boxes right under.
- Unity: `OwnerArt.Icon(name)`, `OwnerArt.Decor(name)` (sprites, null while missing) and `OwnerArt.Show(image, sprite,
  mirror, turn)` (a negative `localScale`).
- Booster icons: wherever a booster's drawn icon shows (booster tiles, jam choices, cost pills with charges, the Store,
  drops and rewards), because `Kit.IconParts` / `UiKit.IconParts` / `UiKit.SetIconParts` recognize a booster's parts
  list (`GardenLook.BoosterOf`); a disabled one fades to `GardenLook.PictureDisabledAlpha` (0.45) instead of turning grey.
- `ivy`: over the left end of a sign, mirrored for the right end; it replaces both the back and the front leaves (it is
  drawn in front only). `flowers`: the win sign's clusters, mirrored for the second one. `button-leaves`: a main button's
  top-left corner, turned half way for the bottom-right one. `logo-leaves`: the drawn wordmark's leaves, mirrored for the
  right end.

### 3.11 The owner's variant icons and lotus (owner's request, pictures.md G9–G24)

Names in `OwnerPictures.IconFolder`, for the eight launch variants (`OwnerPictures.Variants`): the detailed
`OwnerPictures.VariantIcon(iconId)` = `variant-{id}` (512 × 512, slot `tile.icon.{id}`), the simplified
`OwnerPictures.FieldIcon(iconId)` = `field-{id}` (256 × 256, slot `tile.gem.{id}`) and `OwnerPictures.CurrencyLotus` =
`currency-lotus` (256 × 256, slot `currency.petal`). The expansion variants keep their drawn symbols.

- A candy tile (§3.1) with its picture (`OwnerPictures.TileIcon(iconId, style, state)`: the field icon on `Board` and
  `Flat`, the detailed one on `Sticker`, none on `Mystery`) is its face alone (`UiRaster.TileFace`: the same square,
  gradient, gloss, bevel, lip and outline, no symbol; picture keys `tile.face/{style}/{state}/{hex}`) and one sprite of
  the picture over it, in `OwnerPictures.TileIconBox(tile, style)`: a square of `FieldIconBox` (0.66: the icon itself
  about 62% of the tile) or `StickerIconBox` (0.76: about 70%) of the tile's width, centered across and on the face's
  middle above the lip.
- States: `Dimmed` draws the picture at `OwnerPictures.TileIconAlpha` 0.55 over the dimmed face; `Grey` draws a grey
  copy of the picture (`OwnerPictures.GreyPixels`, the luma of `Rgba.Grey`, made once per picture by the host) at 0.8
  over the grey face; `Pressed` sinks the picture with the face; `Mystery` keeps the "?" tile; `Flat` (the finished
  picture) is the field icon on the flat face.
- Playtest: `Kit.CandyTile` (its recipes cached per style, state, icon and color); the grey copy is the painter name
  `icon/variant-{id}#grey` (`PainterBase.GreySuffix`), which both painters make once from the picture. The Android
  painter decodes the icons with mipmaps.
- Unity: `CandyTileView` (an `Icon` image over the face, placed by its layout), `OwnerArt.TileIcon` (the sprite or its
  grey copy, cached), the flying pod (`UiFx.FlyTile(…, icon)`) and the finished picture's texture
  (`BoardPictures.Finished(…, icons)`, which bakes the field icons into the flat tiles with `UiRaster.DrawOver` from
  `OwnerArt.IconPixels`). The imported icons keep no readable copy (`OwnerIconImporter`: mipmaps, high-quality
  compression): the grey copies and the baked pixels are read back once through a render texture.
- The lotus: `Kit.Petal` and `UiKit.PetalIcon` (`UiKit.SetIconParts` with `GardenLook.Lotus`) draw the picture in the
  drawn lotus's box wherever the Petals show, faded like a booster picture when grey.

### 3.12 The owner's animated heroes (owner's delivery, FR-028, research D18)

`tools/heroanim` pre-renders the owner's four FBX heroes offline into flat frames (constitution VII: no model, scene or
camera in the game). The kit's `HeroMotion` (`HeroMotion.cs`, with the generated `HeroMotionData.cs`) describes them
for both builds; Home places them with `HomeLayers` (§6.4), the win and the milestone in their hero box (§6.3).

- **Frames**: `Art/Heroes/Resources/HeroMotion/{family}-{idle|react|win}-{NN}.png` (`HeroMotion.Folder`, `FrameName`),
  slots `char.hero3d.motion.{family}` (`HeroMotion.Slot`). 24 frames a second (`Fps`; 12 until the owner's 60 fps
  models, research D22): per Meshy family 96 idle frames (a 4 s loop) and 48 reaction frames (2 s); Twig (its Blender
  model, research D23) 72 idle frames (3 s), 36 reaction frames (1.5 s) and 72 frames of the win's cheer (3 s,
  `MotionClip.Win`, `HasWin`) (`FrameCount`, `Seconds`, `Has`). Every clip starts on the idle's first frame (the seam)
  and the reaction and the cheer end on it, so they join the idle without a jump. Each file is an 8-bit
  palette PNG with transparency, one palette per family.
- **The cell**: every frame is cut from one 448 × 504 cell (`CellWidth`, `CellHeight`; the still heroes' 8:9 shape)
  whose feet line lies 90% down (`FootLine`). The seam pose fills about 84% of the cell's height down to the feet
  (`Fill`: 0.840 to 0.842) and 58% (Twig) to 82% (Bloom) of its width (`SeamWidth`). A frame is stored cropped to its
  visible bounds: `HeroFrame.X`, `Y`, `Width` and `Height` are its crop in cell pixels,
  `HeroMotion.PictureBox(cell, frame)` places it in a cell box on screen, and `HeroMotion.Cell(box)` fits the largest
  centered 8:9 cell into a box.
- **Head points**: `HeroFrame.Head` (the head bone, at the chin) and `HeroFrame.Top` (the head's top, at the brow), as
  shares of the cell, projected from the rig in the bake; `HeroFrame.Roll` is the head's tilt in degrees, clockwise from
  straight up.
- **A worn hat** (`HeroMotion.Hat(cell, family, frame, shape)`): a square half the cell wide (as
  `CharacterArt.HatOnHero` on the stills), its middle on the chin-to-brow line, `HatLift` × that line's length above the
  brow plus 6% of its size (so its brim, about 71% down its box, overlaps the head by 15% of its size), turned by `Roll`
  about its box's middle; a `sprout` rises a quarter of its size more, so its stem grows from the head's top. `HatLift`:
  Sprig and Bloom 0 (the brow is the top of their face discs; their leaves and petals poke out beside the hat), Drop
  0.35 (halfway up its pointed head, whose tip pokes into the hat), Twig 0.55 (the hat sits on its acorn cap).
- **Timing** (`HeroMotionPlayer(family, idleOrigin)`, deterministic in its inputs): the idle shows its first frame at
  `idleOrigin` and every 4 s after. `React(now, waitForSeam)` starts the reaction at the next seam when asked to wait or
  when that seam is at most `MaxSeamWait` (0.35 s) away, else at once, cross-fading from the idle frame it interrupts
  over `DissolveSeconds` (0.12 s); a request while a reaction plays or waits is ignored; after it the idle starts over
  from the seam. `Pose(now)` gives the `HeroPose`: its clip and frame index, and while it cross-fades the idle frame it
  comes from (`FromIdle`, else −1) and that frame's alpha (`FromAlpha`).
- **Drawing a pose**: the frame `HeroMotion.Frame(family, pose.Clip, pose.Index)` in its picture box; while
  `pose.FromIdle ≥ 0`, the idle frame `FromIdle` over it at `FromAlpha`. In an outfit (on Home once the Wardrobe is
  open): the trail behind (`CharacterArt.TrailBox(cell)`), the skin pattern through the frame's own alpha (as on the
  still hero), the worn expression on its cream badge (`CharacterArt.ExpressionBadge(cell)`: the frames have no faceless
  twin, so always the badge) and the hat at `HeroMotion.Hat`. A hero is never a button: no press look, no click sound.
- **Loading**: a frame is loaded when it is first drawn and kept in a bounded cache, never all 576 (about 280 MB as
  RGBA). The playtest embeds the folder under `heromotion/` (`PainterBase.HeroMotionPrefix`), keeps the frames it drew
  as palette pictures (one byte a pixel) in a cache bounded by bytes, the least recently drawn dropped first, and
  expands only the frames on screen to RGBA (`playtest/design/HeroFrames.cs`; `Visuals.HasMotion`,
  `Visuals.MotionHero`). Unity loads a family's frames one at a time from `Resources` and unloads them when no view
  holds the family (`HeroFrames`, `HeroFrameSet.Hold`); `HeroMotionView` shows one hero in its cell and changes its
  images only when the frame changes; `Editor/HeroMotionImporter` imports the frames and the `home-*.png` layers as
  full-rect sprites without mipmaps, alpha as transparency, clamped and compressed, without a readable copy.
- **Fallbacks**: while a family's frames are missing, Home shows its still hero (`CharacterArt.Hero`) in its cell, and
  the win and the milestone its celebrating picture (`CharacterArt.Cheer`), else the group.

## 4. Screens

Positions and order stay as in spec 002; only the looks change.

### 4.1 Gameplay (frames 7–9, 12–14, 21–23)

- Top bar: Pause squircle, level `WoodSign` (Ivy) instead of the level pill, speed pill.
- Board: lawn backdrop, `StoneBorder`, candy tiles (board style, gem icons §3.1.2), restored ground of a picture role as
  pale flat cells (`PictureColor` lightened 0.55, radius 10%, no bevel, a faint inner shadow), the cells of the picture's
  background (no role, no stone) as grass (owner's review: `Kit.GrassCell` / `UiKit.GrassCell` / Unity
  `BoardPictures.Ground`, the `UiRaster.Grass` picture of §2, `tile.grass`), stones/keys/locks/layers/specials as now
  (stone obstacles in `StoneFace` tones), no picture at the entries (2026-10-03; the stone arch is retired), walkers unchanged. In detail (`BoardPainter`):
  - target tiles nearly fill their cells (inset 0.8%), so only the dark gap and their outlines part them;
  - a stone obstacle is a raised block of the border's stone (`Kit.StoneBlock`, radius 24%) over a soft shadow on the
    restored ground, with a jagged crack (a `StoneLine` groove over a light lip) whose direction follows the cell;
  - a special is a candy-like raised block in its color made vivid (`GardenLook.SpecialFace`: twice as far from its
    grey, lightened 0.05; outline `Darken(0.45)`, lip `Darken(0.28)`, face `Lighten(0.28)` → color, a faint gloss) with
    its white glyph (56% of the face with a counter, 66% without) outlined in `Darken(0.45)`, and its counter on a
    `CountBadge` 0.36 cell tall over its bottom edge until it opens;
  - the next layer peeks from a chip in the tile's top-right corner (40% of the tile): a small board-style candy tile in a
    cream ring with a dark rim; a key waiting under a tile is the gold key on a cream disc in its top-left corner;
  - Bloom Burst targeting rings every candidate tile in `BoosterBloomBurst`, pulsing gently.
- Finished picture (win, Collection; research D14): `TileStyle.Flat` candy tiles (no lip) inside a `StoneBorder` 0.3 cell
  thick when the cells are at least 16 units; the background as grass cells (`tile.grass`), stones as stone blocks.
- Slot row: on a parchment band, `SlotPlate`s.
- Tray: parchment panel behind the columns; pods per §3.7.
- Booster bar: on the parchment, `BoosterTile`s.
- Since the owner's review the rows sit in the reference layout of §6.1: the slots, the four booster boxes and the
  Source stacks' columns of pods (since 2026-10-03) on one parchment tray.
- Unity twin, layout: `GameplayHud.Layout(hasBadge, hasBoosters, entrySides, stackCount)` places every region from
  `ScreenLayout.ReferenceGameplay`. It gives the views their boxes in their own canvas units, all wired by
  `GameplayController`:
  - `SlotCells` and `BoosterCells`;
  - `PodGrid(stackCount)`: the regions with the columns and the pod sizes moved into the pod row's canvas units by
    `GameplayHud.InPodRow`, so `Pod`, `Chip` and `Shows` work there;
  - `FitBoard` for `BoardView.Fit`.
- Unity twin, drawing: `UiKit.TrayPanel` (the frame and its bands) and `UiKit.GridPod` (one pod of the grid, exposed
  or waiting, `PodView`) in `UiKitTray.cs` draw the playtest's recipe of §3.7 and §6.1 "Drawn". `TrayView` lays out
  the columns, their "+N" discs, the wells and the links.
- Connected pods: a group whose shown members sit side by side in one row is joined by the link bar. A group whose
  shown members lie on different rows, or that shows a single member, marks each shown member with a ring of its link
  color (in a white rim) at its pod frame's top-right corner (the "+N" disc takes the top left, the count the bottom
  right). Bars and rings go by the frames (`PodChip.Frame`), not by the wider places.
- Flights: a committed pod's flight starts at its pod's tile (`TrayView.TilePosition`, `TileSize`) and lands on the
  slot's tile (`SlotRowView.TilePosition`, `TileSize`). A returned pod flies back to the tile of its place on top of
  its column.

### 4.2 Backdrop

`BackdropScene.Gameplay` becomes a lush sunny garden seen from above (owner's review, FR-020: "as colorful as the
reference"): `LawnLight` → `LawnDark` fbm grass with fine darker strokes and lighter sunny paths, a dense hedge of deep
`foliage.*` greens along the sides, bushes and big leaves along every edge, many flowers (pink, white, orange
`ButtonOrange`, yellow), big ones near the edges, a light vignette (0.12); no sky.
Home and Splash keep the sky, arches and hills but warmer (until the owner's pictures); the distant arches are signed
distances blended over 1.5 backdrop pixels, so their round doorways stay smooth at a fifth of the resolution, the
hedges carry a leafy texture, and their blossoms (fewer, of varied sizes, pink and white with yellow middles) gather
toward the hedges.

The lawn (`BackdropRaster`) draws, back to front: soft patches of sun and shade with a finer mottle; lighter paths
winding through the grass (the ridges of a slow fbm, `lawn.light` mixed 16% toward the flower yellow, at 0.5); one short
tapered blade per 0.011-width cell, dark or light; the hedge along the sides (deep `foliage.deep` → `foliage.mid` with a
leafy speckle of `foliage.light`, its inner edge wavy, 0.045–0.095 of the width, a soft shadow on the grass); soft bushes
at the edges (0.1-width cells, within 0.11 of an edge, radius 0.6–1.0 cell, `foliage.deep` → `foliage.mid` lit from the
upper left, a `foliage.light` speckle, a soft shadow); almond leaves fanned inward from the nearest edge (0.058-width
cells, within 0.19 of an edge, in `foliage.*` greens, a few in spring green); big five-petal flowers within 0.16 of an
edge (0.1-width cells) and small ones everywhere (0.062-width cells, many more near the edges), pink most often, then
white, orange, the theme's own and yellow; the vignette. The theme still
shows (`DesignTokens.Backdrop`, frame 18): its accent's hue tilts the grass (Pond teal, Orchard warm, Moonlit blue-green)
and colors a fourth flower, and the Moonlit Garden's dimmer background darkens the lawn toward dusk; the Daylight Garden
keeps `lawn.light` and `lawn.dark`. Hosts render it at a third of the screen's resolution
(`BackdropRaster.Downscale(scene)`; Home and Splash stay at a fifth). The slots stay `bg.theme.*` (the owner's pictures
B2–B5 replace the lawn).

`BackdropScene.Win` (the full-screen win, §6.3; slot `bg.win`, the owner's `win.png` replaces it,
`OwnerPictures.Win`): the level's lawn rendered at an eighth of the screen (`Downscale(Win)` = 8, so the hosts' smooth
upscale blurs it), lightened 24% toward `parchment.top`, with a warm `ray.light` glow around the middle of the screen
(its strength `(1 − d)² × 0.62`, d the distance from (0.5 W, 0.5 H) over 0.75 W with the height squeezed 0.8). It takes
the gameplay colors, not Home's warm ones (`BackdropRaster.IsLawn`).

### 4.3 Popups and cards (frames 10, 11, 17–20)

- Jam sheet: parchment sheet; title "No more space!" (`InkTitle`, `type.title`; "No pod can move!" when stuck); subtitle
  "All Waiting Slots are full. Choose a way to continue." (`InkBrownSoft`, broken after the first sentence into two
  lines; "Choose a way to continue." when stuck, since the slots are not full then); an inset `Well` with the slot
  contents (`ui.jam.slots`: sticker tiles with counts, free slots as small dashed plates, locked ones with the padlock);
  one `ChoiceButton` per recovery booster the player can use now (Extra Slot green, Return blue, Bloom Burst blue;
  Shuffle green if offered) with its cost pill (×N charges, or lotus + price); the free rescue (once per attempt) as one
  more green `ChoiceButton` with the rescue booster's icon and name and the "▶ Free" pill; the choices in one row of up
  to three, a 2 × 2 grid of four (as on the reference) or rows of three; Restart as a cream secondary button with ⟳ at
  the size of a card's main button. A short phone shrinks the well, the choices and the gaps together.
- Pause, Settings, Daily reward, Themes, Milestone: `Card` per §3.5; the milestone uses a
  `WoodSign` header. The Store is a page since the owner's note of 2026-10-04 (§4.6, §6.6), the Leaderboard and the
  Collection since the owner's request of the same day ("All the menu's places must be a separate page. Not popups.";
  §4.6, §6.8, §6.9). Pause: brown title, the
  cream close, Resume (primary, decorated), Restart (⟳), Settings (gear) and Home (`ui.back`) as cream secondaries with
  their glyphs. Settings: cream rows with brown labels and
  the garden toggle (on: the green set's glossy track with a white ✓ and the knob right; off: a parchment well; the knob
  a domed cream cushion like the round buttons); since the owner's request of 2026-10-04 its last switch is "Falling
  petals" (`settings.petals`, the save's `settings.homePetalsOn`, off by default since the owner's tuning of 2026-10-05;
  the earlier `homePetals`, on in every older save, is ignored): off, Home draws neither the layered
  Home's petals (`bg.home.petals`) nor the stand-in's falling petals; the splash, the win and the milestone keep theirs.
- Demo and unlock cards: parchment, no title; a booster's card shows its colored icon on a cream tile; the first line in
  `type.button_secondary` `InkBrown`, the others in `type.body` `InkBrownSoft`, each wrapped to the card; variant cards
  show sticker tiles; "Tap to continue" in `InkBrownSoft`.

### 4.4 Win (frame 15)

- `WoodSign` (Flowers) "Level complete!" (`type.level_home`, `size.win_sign_height` = 146 units tall, at most 80% of
  the card wide) across the card's top edge, its center 30 units below it.
- The finished picture in full color: each picture cell as a flat candy tile (no lip, small gloss, board-style symbol
  of its role's variant) inside a `StoneBorder` (thin, 0.3 cell); `size.win_picture_height` = 520 units tall (24% of the
  safe height on a shorter phone, at least 400), with the light sweep once. A milestone level adds a cream pill with the gold medal and
  "Milestone reached!" over the picture's top edge.
- The heroes group on a `StonePedestal` above the sign, in the room up to the safe area's top (left out under 150
  units), laid out by `HomeStage.Celebration` (the pedestal 80% of the group's width at the stage's bottom, the group
  picture standing on its top with the heroes' feet, `CharacterArt.GroupFeetShare`, a tenth of the top's half height
  below its middle, as large as the room from the heads to the pedestal's foot allows), with light rays behind (clipped
  above the card, fading in, alpha 0.85) and falling petals around. The group picture has no base of its own. When the
  owner's celebrating hero of the celebrating family exists (pictures.md A7, `char.hero3d.cheer.*`; Twig and Sprig by
  turns level by level, `CharacterArt.CelebrantOf`, since the owner's choices of 2026-10-03 (Twig alone at first), before
  them the family of the variant with the most work), it stands alone on the pedestal instead of the group. Since the owner's delivery
  (FR-028, §3.12, §6.3) that family's animated hero stands there first, when its frames exist: its reaction from the
  moment it appears, then its idle for as long as the card shows; the still celebrating picture, then the group, stand
  in while the frames are missing. A light sprinkle of confetti falls
  for 2.2 s only above the card, so the picture, the reward and Next stay clean. Pause stays visible and usable over the
  win card in both builds (FR-002: the card changes no tap outcome, so Home, Restart and Settings stay reachable from
  it, as before spec 005). The celebration (rays, petals, Next breathing) animates for about
  8 s after the card shows, then rests on its last frame until the next input, so an idle win card costs no frames;
  an animated hero keeps the card drawing for as long as it shows (its 24 fps frames; the playtest at its slower
  celebration rate).
- The reward as a cream pill (`CostPill` style, `size.reward_pill_height` = 104 units tall, `ui.pill.reward`) "+N"
  with the lotus, counting up, a sparkle at the lotus and petals bursting out; a dropped booster charge below it as its
  icon and "+1 Name".
- Next: `PrimaryButton` (wood rim, decorated, breathing). ×2: cream secondary with the ad glyph.
- Milestone (frame 16): the same sign ("Level N"), heroes (the level's animated hero as on the win), rays and petals;
  "Milestone reached!" in `InkBrownSoft`;
  each reward's icon on a cream tile with its amount in a cream pill over the tile's bottom edge; Continue (primary,
  decorated, breathing).

### 4.5 Home, Splash (frames 1–3)

- Logo: "Bloomlings" in `type.wordmark` with a wooden look: pale cream-yellow fill `#FFF0C8` → `#E9B874`, outline
  `WoodLine`, a darker extrusion, inside the reference's olive moss band (`IvyLine` mixed 35% toward `WoodLine`,
  lightened 12%; 0.14 em with its own extrusion), broad leaves (the win sign's cluster leaves, 1.7 em) behind both ends
  and two small pink flowers over them.
- Level: a `WoodSign` (None) plaque with "Level N", as tall as its row and as wide as the letters plus 1.5 × its height.
- Play: the big primary button in its wood rim.
- Settings, Petals pill per §3.3–3.4, with the profile avatar on one header row (§6.4; the owner's request of
  2026-10-04).
- Heroes (`HomeStage.ShowsHeroes`; first deferred by the owner on 2026-10-02 over the single Home picture, then
  delivered animated the same day, FR-028): over the owner's layered Home (pictures.md B1: the garden with its
  fountain layers) Home and the splash stand the four animated heroes on the painted fountain (§6.4 "The layered
  Home", §3.12), no pedestal and no drawn fountain, with the logo, Settings, the Petals pill, the avatar, the Daily
  Challenge's side button, the plaque, Play, the pills and the bottom menu (§6.7) over them. Without the owner's picture, the drawn stage
  `HomeStage.ReferenceDiorama` (kit `HomeLook.cs`, §6.4): a `StonePedestal` ring, the lotus fountain on it
  (`Kit.LotusFountain`, `ui.fountain`: a small pedestal as its basin, water, two lily pads, the lotus) and the four
  still heroes around it as in the reference (Bloom raised behind the fountain, Drop at the right back, Sprig at the
  left, Twig in front at the right). Over an owner picture without the fountain layers (a splash picture of its own,
  B6) no heroes show. Early and progressed alike, each hero wears its outfit once the Wardrobe is open. The splash
  shows the Home picture until its own (B6) exists (`OwnerPictures.Resolve`) and the same stage as Home, so it turns
  into Home without a jump. The Leafling guest (spec 004 R17) was removed by the owner on 2026-10-02.
- The milestone teaser is a parchment pill (`Kit.ParchmentPill`) with the outlined pink gift and `InkBrown` text; the
  Daily Challenge card is parchment with the sun on a cream disc. (The rank pill and the avatar side button left Home
  for the bottom menu's Leaderboard and Wardrobe places on 2026-10-04, §6.7.)
- Backdrop: `HomeStage.Garden` warms the Home and splash colors (a clearer blue sky, sunlit horizon and hills, lush
  bushes with pink blossoms, sandy arches; 15% of the band's theme tint stays).

### 4.6 Wardrobe and the Store page

- The page header (`Kit.PageHeader` / `UiKit.PageHeader`, §6.5): the back (`ui.back`) round button, the `WoodSign`
  (Ivy, its clusters at `PageHeader.IvyScale`) banner in the middle of the screen and the Petals pill on one line, the
  same on every page.
- Hero on a `StonePedestal` with ‹ › cream round arrows (Unity Wardrobe).
- Name card: parchment with a small sign-like tab carrying the name (`type.title`), the role line (`type.body`
  `InkBrownSoft`) and a description.
- Family tabs: cream tabs, top corners rounded; the selected one lighter and joined to the panel below; each shows the
  family's hero picture (or silhouette) and its name.
- Outfit cards (`Kit.OutfitCard`, its Unity twin `UiKit.OutfitCard` shared by the Wardrobe and the Store): cream cards
  (radius 11%, a 3.5% lip) with a beige picture well 64% of the face tall, the name below; the worn one has a green-tinted
  well, a 4 px green (`GardenLook.Green.Face`) border and a green check badge. A hat sits on the hero's head in full color
  (`CharacterArt.HatOnHero`: 55% of the picture wide over `HeadTopHero`, its brim overlapping the head by 15%), its own
  tint with a `tint.Darken(0.45)` outline and a light top-left side.
- Footer: "Earn special outfits as you play!" (`InkBrownSoft`).
- Unity Wardrobe (a full screen over Home, `ScreenLayout.ReferenceWardrobe` with the kind chips, §6.5): the tabs end
  with a Profile tab (the avatar), whose stage shows the avatar on the pedestal and whose name card says what the
  profile items do; the panel starts with the kind chips (`UiKit.Tabs`: skins, hats, trails, faces; or frames, badges,
  markers), then the outfit cards of the kind, three to a page, "Default" (none of the kind) first, each card showing
  the hero in its outfit with that item; the worn card (or the shown profile item) is green with the check; the footer
  sits between the cream ‹ › page arrows. The hero's feet stand on the pedestal's top ellipse; the ‹ › cushions are
  `UiKit.PageArrow`s in touch-sized squares; the description is broken into two balanced lines.
- The Store page (both builds; preview frames 17 and 26; §6.6; the playtest's and Unity's `StoreScreen`): over
  the Wardrobe's garden (`bg.wardrobe`, B7), the page header with the "Store" banner, then a parchment panel
  (`mat.parchment`, a card's radius, its bottom corners past the screen's edge) with the Shop / Cosmetics tabs
  (`Kit.Tabs`, once the cosmetics open), Unity's offline line, and the list. The Shop's rows are cream rows (`Kit.Row`)
  with the booster tile (0.8 of the row) and its count badge, the name (`type.button_secondary`, grown with the row) and
  a cost pill (a tap on the row buys), the real-money rows faded with "Unavailable" while purchases are off. The
  Cosmetics tab: `Kit.FamilyTabs` (`ui.tab.family`) over the lighter panel and `Kit.OutfitCard`s (`ui.card.outfit`)
  three to a row, as many rows as the page holds (two on 16:9, three on 19.5:9, four on 21:9 in the preview): "Default",
  worn while the family wears nothing, then each item for sale shown on the chosen family's hero (a frame, badge or
  marker as its shape) with its cost pill on the card's bottom edge (a tap buys); the footer between cream ‹ › page
  arrows (`Kit.ArrowButton`, `UiKit.PageArrow`); the bottom menu (§6.7) over the panel's foot, the Shop in its
  medallion. The Daily Reward card carries a `WoodSign` (None) header (the Leaderboard and Collection cards did too
  until they became pages).
- The Leaderboard and Collection pages (both builds; preview frames 5, 6, 20 and 31; §6.8, §6.9; the playtest's and
  Unity's `LeaderboardScreen` and `CollectionScreen`): the Store page's frame (the Wardrobe's garden `bg.wardrobe`, the
  page header with the "Leaderboard" or "Collection" banner, the parchment panel `mat.parchment` with a card's radius
  and its bottom corners past the screen's edge, the bottom menu with their place in the medallion). The Leaderboard's
  rows are the card's recipe on the page's row (`ReferenceLeaderboardRegions.Parts`): cream rows (`Kit.Row`, `ui.row`;
  the player's raised and green), the outlined medals (`ui.medal`, gold, silver, bronze) with their `type.badge`
  number or the plain brown rank, the portraits on cream discs (`ui.person`, or the player's hero), the brown name
  (`type.body`; "You" in `type.button_secondary`), the player's marker and badge, the score in `type.count`, every
  letter grown with the row (its height over a `0.11W` row's); "…" (`type.title`, `ink.brown_soft`) between the top
  ranks and the player's neighbours; the offline line in `type.caption` `ink.brown_soft`; Refresh the cream secondary
  button with ⟳ (`ui.button.secondary`, `ui.restart`). The playtest shows its offline form, as its card did: five
  placeholder ranks (sunk parchment bars, no invented players), the gap and "You" with the highest completed level;
  its Refresh says the leaderboard is offline. The Collection's grid: the count in `type.caption` `ink.brown_soft`,
  the finished pictures newest first in their raised cream frames (`Kit.PictureFrame`, `collection.frame`; pressed,
  they squash like a tile), three to a row, "n / m" in `type.caption` between the cream ‹ › page arrows
  (`Kit.ArrowButton`, `UiKit.PageArrow`) when they take more than one page; a picture's detail
  (`collection.detail_frame`): the picture in the same frame up to `0.8W`, its name in `type.title` `ink.brown`, and
  "Completed at Level N" in `type.body` `ink.brown_soft`, centered.
- The playtest's Wardrobe (owner's review, FR-025; preview frame 27, `playtest/design/WardrobeScreen.cs`, opened from
  the bottom menu's Wardrobe, §6.7; before 2026-10-04 from Home's avatar): the §6.5 layout without the kind chips and
  the profile tab, the bottom menu over the panel's foot with the Wardrobe in its medallion; the name card is
  `Kit.NameCard` (parchment whose middle rises into the name tab); the cards, three a page, are "Default" (nothing worn;
  a tap takes everything off), the owned worn items (a tap wears one, or takes it off when worn), the worn items for sale
  (cost pill; a tap buys and wears) and the ones earned later (`Kit.OutfitCard(…, locked: true)`: the picture faded to
  `GardenLook.PictureDisabledAlpha`, the `Kit.LockBadge` padlock where the check would be); each shows the family's hero
  in its outfit with the item in its kind's place. Equipping and buying go through `WardrobeService`.

## 5. Asset slots

The Leaderboard and Collection pages (§6.8, §6.9) add no slot: they draw the page header's, the Store page's and the
cards' slots (`bg.wardrobe`, `mat.parchment`, `ui.row`, `ui.medal`, `ui.person`, `ui.button.secondary`, `ui.restart`,
`collection.frame`, `collection.detail_frame`, the menu's); `ui.card` no longer lists them.
New slots (kind `Procedural` unless noted) registered in `AssetSlots` and marked where drawn:
`mat.wood.light`, `mat.wood.dark`, `mat.stone`, `mat.parchment`, `tile.candy`, `tile.candy.sticker`,
`ui.sign.wood`, `ui.sign.flowers`, `ui.button.rim`, `ui.button.choice`, `ui.pill.cost`,
`ui.pill.speed`, `ui.badge.count` (restyled), `board.border.stone`, `board.arch` (retired on 2026-10-03 with the arch), the lawn (the `bg.theme.*` slots
restyled, §4.2; `tile.base`, `tile.ground`, `tile.layer_peek` and `tile.picture` restyled; `tile.entry` retired on 2026-10-03), `fx.rays`, `fx.petals` (kind `Shape`: one petal), `ui.pedestal`, `ui.tab.family`, `ui.card.outfit`,
`ui.logo.wood`, `ui.back`, `ui.fast`, `ui.nav.bar`, `ui.nav.medallion`, `ui.nav.lock` and `ui.locked.notice` (the bottom
menu, its locked places' padlock and their notice, §6.7), `icon.nav.shop`,
`icon.nav.wardrobe`, `icon.nav.home`, `icon.nav.leaderboard` and `icon.nav.collection` (its places' icons, the owner's
pictures D9–D13; the `icon.` prefix is `UiKit`), `booster.extra_slot`/`shuffle`/`return`/`bloom_burst` (redrawn; the owner's
icon pictures replace them, §3.10), `tile.grass` (the picture's background cells, §4.1), `bg.win` (the win's garden,
§4.2), `ui.sign.ivy`
(kind `Shape`: the clover cluster), `ui.jam.slots` (the jam's slot row), `ui.pill.reward` (the win's and the milestone's reward pills),
`tile.icon.{id}` and `tile.gem.{id}` (the owner's detailed and simplified variant icons, §3.11; `OwnerPictures.IconSlot`,
`GemSlot`). The `mat.` prefix is the `Material` category and `board.` belongs to `BoardTile`. Owner pictures (research D16 and
`pictures.md`) keep or add their `bg.*`, `char.hero3d.*` and `brand.wordmark` slots, whose kind is their stand-in's
(`Procedural` backdrops, the `Text` wordmark, the `Generated` heroes), with the drawn or generated stand-in as fallback:
`bg.home`, `bg.splash`, `bg.wardrobe` (the Wardrobe and the Store page) and `bg.theme.*` (`OwnerPictures.SlotOf`); the
optional tagline is `brand.tagline` (kind `External`, not drawn yet); the optional celebrating heroes (A7) are
`char.hero3d.cheer.sprig|bloom|drop|twig` (`CharacterArt.CheerSlot`, picture `CharacterArt.Cheer(family)` =
`3d/{family}-cheer`), with the group picture standing in until they exist. The owner's 3D pictures share the
`tools/artgen` folder: `adopt` marks them `"source": "owner"` in its `manifest.json` (`tools/artgen/README.md`).
The owner's layered Home and animated heroes (FR-028, §3.12, §6.4) add `bg.home.fountain_back`, `bg.home.lotus`,
`bg.home.fountain_front`, `bg.home.shadow` and `bg.home.petals` (`HomeLayers.SlotOf`; `bg.home` stays the garden
layer) and `char.hero3d.motion.sprig|bloom|drop|twig` (`HeroMotion.Slot`), with the still heroes as their stand-in.

## 6. Reference layouts (owner's review, spec 005 FR-020 to FR-025, FR-029)

Measured on the reference's phone screens (crops `g-game.png`, `g-jam.png`, `g-win.png`, `g-home.png`,
`g-ward.png` with a 5% grid). `W` and `H` are the safe area's width and height; positions are fractions of them
unless given in `W` units. The engine-free `ScreenLayout` computes every region for both builds; screens place
elements only from those regions. On screens shorter than 19.5:9 the tray rows scale down by
`k = clamp((H / W) / 2.0, 0.8, 1)` (`ScreenLayout.ReferenceAspect` = 2.0, the safe shape of a 19.5:9 phone with its
insets: `k` is 1 at 19.5:9 and 21:9, about 0.93 at 18:9 and 0.86 at 16:9) and the board takes what is left.

The functions (engine-free, `client/Assets/Bloomlings/UI/Design/ReferenceLayout.cs`, partial `ScreenLayout`; tests in
`ReferenceLayoutTests`) are `ScreenLayout.ReferenceGameplay` → `ReferenceGameplayRegions` (with `PodChip` for one
pod of the tray's grid), `ScreenLayout.JamCard` → `JamCardRegions`, `ScreenLayout.WinScreen` → `WinRegions`,
`ScreenLayout.ReferenceHome` → `ReferenceHomeRegions`, `ScreenLayout.PageHeader` → `PageHeader` (the four pages'
header row), `ScreenLayout.ReferenceWardrobe` → `ReferenceWardrobeRegions` and
`ScreenLayout.ReferenceStore` → `ReferenceStoreRegions`; the Leaderboard and Collection pages are
`ScreenLayout.ReferenceLeaderboard` → `ReferenceLeaderboardRegions` (with `LeaderboardRowParts`) and
`ScreenLayout.ReferenceCollection` → `ReferenceCollectionRegions` (in `MenuPages.cs`, §6.8, §6.9); `ScreenLayout.ReferenceScale` is `k`; the bottom menu is
`ScreenLayout.BottomNav` → `BottomNavRegions` and `ScreenLayout.BottomNavTop` (in `BottomNav.cs`, §6.7). Where the measurements
left a choice, the implementation fixes it as noted under each table ("Fixed:").

### 6.1 Gameplay

| Region | Box |
|---|---|
| Top bar | top `0.012W`, height `0.13W`: Pause squircle `0.13W` at the left edge + `0.04W`; the level sign `0.42W × 0.115W` centered, with ivy over its ends; the speed pill `0.2W × 0.115W` at the right edge − `0.04W` |
| Board | between the top bar (+ `0.02W`) and the entry strip: the stone border's outer box at most `0.86W` wide, centered; the grid inside it (border 0.42 cell + gap 0.04 cell); cells as large as fit |
| Entry strip | under the board, `0.04W` tall, plain lawn, whatever the entries (since 2026-10-03; before, `0.17W` with the arch for bottom entries) |
| Tray | from the entry strip to the bottom of the screen (under the bottom inset too), full width, parchment with rounded top corners (radius `0.06W`) and a soft top shadow; inner padding `0.035W` at the sides, `0.025W` at the top, the bottom inset + `0.02W` at the bottom |
| Slots row | `0.16W·k` tall: five plates `0.14W·k` wide each (portrait, height = row), spread evenly across `0.92W`; the extra slot (sixth) narrows them to fit |
| Separator | a thin `ParchmentEdge` line with a light line under it, in the middle of a `0.03W·k` gap |
| Booster row | `0.18W·k` tall: four cream squircle boxes `0.16W·k` square, spread evenly across `0.9W` (centers at 13%, 37.7%, 62.3% and 87% of W at `k` = 1), the green badge on each box's bottom-right |
| Separator | as above |
| Pods row | one column per Source stack across `0.96W`, each holding `PodRows` pods one after another: the exposed pod `0.13W·k` tall on top, then the waiting pods `0.1W·k` tall, `0.01W·k` apart; `0.46W·k` tall with four rows, `0.35W·k` with three |

Before the owner's rule of 2026-10-03 the slots row was `0.19W·k` (plates `0.165W`), the booster row `0.23W·k` (boxes
`0.195W`), the separators `0.04W·k` and the pods row `0.31W·k` of decks. These rows shrank so that four rows of pods
fit while the board keeps a third of the screen (FR-021, research D20).

Fixed:
- **Top bar and badge**: the Pause box is `0.13W` square at `0.04W`; the sign and the speed pill are centered on the
  bar. A Hard or Super Hard badge (`hasBadge`) is a `0.36W × 0.052W` box under the bar and pushes the board down by its
  height.
- **Collapsing**: the entry strip shrinks by `k` too. Without boosters (`hasBoosters: false`) the booster row and its
  line collapse.
- **Tray**: the tray box spans the whole screen width and runs to the screen's bottom (`TrayRadius` = `0.06W`). Its
  content box (`TrayContent`) stops `0.02W` above the bottom inset.
- **Separators**: lines `0.92W` wide and `max(2 px, 0.005W)` thick, in the middle of their `0.03W·k` gaps.
- **Slots and boosters**: the slots spread their `0.92W` with at least `0.0238W` between them (five are `0.055W` apart
  at `k` = 1). The boosters spread their `0.9W` with at least `0.03W` between them, the first box starting at `0.05W`
  and the last ending at `0.95W`.
- **Slot tile**: a Waiting Slot's sticker tile is `min(0.74 face width, 0.66 face height)`, 8% of the face under its
  top (`ReferenceGameplayRegions.SlotTile`, about `0.1W`), with the count under it.
- **Tray height**: on the reference shape (`k` = 1, four rows, boosters) the tray holds `0.905W` of rows and padding:
  `0.025W` + slots `0.16W` + `0.03W` + boosters `0.18W` + `0.03W` + pods `0.46W` + `0.02W`. It held `0.855W` with the
  decks.
- **Board**: `ReferenceGameplayRegions.FitBoard(width, height)` runs `BoardLayout.Fit` over `BoardArea` (the
  board's top to the entry strip's bottom, the safe width less `0.02W` a side) with the stone border's outer box at
  most `0.86W` (`MaxBoardShare`); the board fills that width or the whole height down to the tray.
- **Booster badge**: `BoosterBadge(i)` is the badge disc as `Kit.BoosterTile` draws it (0.34 of the box, its center
  0.55 of it inside the bottom-right corner).

**Pod grid** (§3.7; FR-021 as amended on 2026-10-03; `ReferenceGameplayRegions.Columns`, `PodRows`, `FrontHeight`,
`QueueHeight`, `PodGap`, `Shows`, `Pod`, `Chip`). This is the owner's gameplay rule: a stack's pods go one after
another, never on each other, whatever the look.
- **Rows**: `PodRows` is 4 (`MaxPodRows`) when the safe height is at least `FourRowsAspect` = 1.95 times the safe width
  (19.5:9 phones and taller), else 3 (`MinPodRows`). 16:9 phones get three rows, and so do 18:9 phones under a status
  bar, whose safe shape falls just below 1.95.
- **Columns**: one per stack, each `min(PodMaxShare·W·k, (0.96W − ColumnGapShare·W·(n − 1)) / n)` wide, where
  `PodMaxShare` = 0.24 and `ColumnGapShare` = 0.016. They spread across the pod row's `0.96W` with gaps from `0.016W`
  to at most `0.04W`, centered, and every column spans the pod row's height. At `k` = 1:
  - four stacks are `0.228W` wide and fill the row;
  - two or three stacks are `0.24W` wide and sit `0.04W` apart in the middle;
  - five stacks are `0.179W` wide, and six (`SourceTray.MaxStacks`) `0.147W`.
- **Pods**:
  - The exposed pod, `Pod(stack, 0)`, sits at the column's top and is `FrontHeight` = `min(FrontShare·W·k, column
    width / PodChip.MinAspect)` tall (`FrontShare` = 0.13).
  - Each next pod, `Pod(stack, d)` for `d` ≥ 1, is `QueueHeight` = `min(QueueShare·W·k, column width / 1.45)` tall
    (`QueueShare` = 0.1), under the one before it and `PodGap` = `PodGapShare·W·k` apart from it (`PodGapShare` =
    0.01).
  - So a pod's place is never less than 1.45 times as wide as it is tall, and its 1.3:1 frame always has its whole
    width. With five and six stacks the pods get shorter: the
    exposed one is `0.124W` and `0.101W` tall at `k` = 1.
  - The pod row is `FrontHeight + (PodRows − 1)·(QueueHeight + PodGap)` tall, and its last row ends at its bottom.
  - `Shows(d)` is true for `0 ≤ d < PodRows`. Deeper pods are not drawn; the "+N" disc counts them.
- **One pod** (`Chip(stack, d)` = `PodChip.In(Pod(stack, d))`):
  - `Frame` is `min(width, Aspect·height)` wide (`Aspect` = 1.3) and the box's height, centered in the box.
  - `Inner` is the frame inset by `Border` = 9% of its height.
  - `Tile` is a square `TileShare` = 1.04 times `Inner`'s height, centered on `Inner`; `Icon` is `Tile` grown by
    `IconGrow` = 4% of the height at each side.
  - `Count` is centered `CountInset` · size in from `Inner`'s bottom right corner, `size` = `CountShare` = 36% of the
    height tall and half of `Inner` wide (the digits' type size and their widest).
  - `Badge` is a disc `BadgeShare` = 44% of the height, centered 12% of the height inside the frame's top left corner.
- **Taps**: only the exposed pods take taps, each through its touch box: the pod's place (wider than its frame) grown
  about its center to the touch minimum (`size.touch_min` × the screen scale). A touch box may reach over the waiting pod under it, which takes no
  taps. The touch boxes stay inside the safe area and clear of each other and of the booster boxes
  (`ReferenceLayoutTests`).

**Drawn** (the playtest's `LevelScreen`, `PodPainter`, `SlotPainter` and `BoosterBarPainter`; the Unity twin follows
this recipe):
- **Tray**: the old banded parchment on the new regions, as the reference's tray (`LevelScreen.TrayPanel`):
  - a frame of deep parchment (`parchment.edge` mixed 55% toward `parchment.line`, a dark outline, a soft shadow
    rising onto the lawn), showing `0.012W` round one band per row;
  - each band runs from `parchment.well` mixed 55% toward `wood.light` at the top to `parchment.edge` mixed 40% toward
    `parchment.well` at the bottom, with its edges aged with `parchment.line`, a light bevel along its top and a thin
    outline;
  - the first band's top corners are `TrayRadius` less the margin, the others `0.03W`;
  - the bands are parted by a `0.009W` groove at each separator's middle.
- **Columns** (`PodPainter.DrawColumns`; Unity `TrayView` with `PodView` and `UiKit.GridPod`): each shown pod is
  §3.7's `Kit.Pod` in its `Chip`. The exposed pod is bright and the waiting pods under it are muted. The "+N" disc sits
  on the last shown pod of a deeper column (`PodPainter.MoreBadge`, slot `pod.deck`). An emptied stack is a `Kit.Well`
  (`parchment.well` mixed halfway toward `parchment.edge`) in its exposed pod's box, inset by 6% of the box's height.
- **Taps and flights**: only the exposed pod takes a tap. A committed pod flies as its sticker tile from its
  `PodChip.Tile` (the icon's middle) to the slot's tile (`SlotPainter.TileBox`); a pod that Return puts back flies from its slot to the tile of its place on top
  of its column.
- **Motion** (presentation only, FR-002; the core's events drive it):
  - When the exposed pod leaves, the pods under it slide up one row in 0.18 s, easing out (`PodView.SlideSeconds`,
    the playtest's `TrayMotion.SlideSeconds`). A pod reaching the top row grows to the exposed pod's height,
    and the next hidden pod fades in at the last row.
  - When Return puts a pod back, its column slides down a row, and the last shown pod fades out under the pod row's
    bottom.
  - Shuffle re-lays the columns at once with each build's Shuffle effect as before: the playtest's swirl ring
    (`fx.shuffle_swirl`) and Unity's turn-over of every shown pod.
- **Connected pods**: pods side by side in one row are joined by the link bar (`pod.link`, §3.7) between their frames.
  A group whose shown members lie on different rows marks each member with a ring of its link color on its frame's
  top-right corner.

### 6.2 Jam (centered modal)

The gameplay stays visible under a warm scrim (alpha 0.5). The card: `0.92W` wide, centered horizontally, its center
at 51% of H, height from its content: top padding `0.06W`; the title (`type.title`, `ink.title`); `0.02W`; the subtitle
in up to two lines (`type.body`, `InkBrownSoft`); `0.035W`; the well with the slot contents `0.8W × 0.21W` (tiles
`0.11W` with counts below); `0.04W`; the choice grid — two columns `0.4W` wide with a `0.05W` gap, rows `0.205W` (the
button) + `0.075W` (its cost pill overlapping the button's bottom edge by half), `0.03W` between rows; `0.045W`; Restart
`0.7W × 0.14W` (cream, ⟳); bottom padding `0.06W`. The close button (cream round `0.13W`) over the top-right corner when
the sheet may be closed. The card pops in (motion.pop). Fixed: the title box is `0.1W` tall and inset `0.16W` from
the card's sides (clear of the close button), the subtitle box `0.11W` (two lines) inset `0.06W`; each cost pill is
`0.3W × 0.09W`, its bottom `0.075W` under its button's bottom edge (so it overlaps the button by `0.015W`); an odd last
choice sits in the middle; the close button's center is `0.06W` inside the card's right edge and `0.025W` below its top;
on a short screen every height and gap shrinks by `Scale` (the card stays `0.05W` inside the safe area);
`WellCell(i, n)` gives each slot's tile (`0.11W·Scale`, at most 86% of its share of the well) and count box.
Playtest (`EndCards.Jam`): the rules never let the jam be dismissed, so there is no close button; the title's letters
fill 80% of its box (`type.title` about 1.35×), each subtitle line 78% of half the subtitle box; one touch box covers a
choice and its cost pill; the scrim takes every tap below the gameplay's top bar, so the board and the tray stay
visible around the card (spec 001 FR-027) but out of reach, and Pause and the speed button above it stay usable.

### 6.3 Win (full screen)

The gameplay is replaced by the celebration over the win garden (`bg.win`, else the gameplay garden blurred and
lightened); no top bar.

| Element | Box |
|---|---|
| Sign | `0.66W × 0.13H`, centered, top at 7.5% of H (flower clusters over both ends, out to `0.04W` from the edges) |
| Picture | the finished picture in its stone frame, at most `0.8W` wide, top at 21.5% and bottom at most at 58% of H |
| Rays and petals | centered on the hero, radius `0.6W`, behind the hero; petals over the whole screen |
| Hero | the level's celebrant, Twig on odd levels and Sprig on even ones (`CharacterArt.CelebrantOf`: its animated hero, else its celebrating picture, else the group), centered, from 50% to 76% of H, overlapping the picture's foot |
| Pedestal | `0.8W` wide, from 70.5% to 81.5% of H, top ellipse at about 73.8% under the hero's feet (73.4%); with the owner's win picture its own stone disc is the stage instead |
| Reward pill | `0.47W × 0.09H`, centered, from 76% to 85% of H (on the pedestal's front) |
| Next | the primary button in its wood rim, `0.84W` wide, from 86% to 96% of H; ×2 reward as a small cream pill under it when offered, or beside the reward pill |

Fixed: the sign is `0.66W` from 7.5% to 20.5% of H; the hero box is an 8:9 solo picture's box (`CharacterArt.HeroWidth`
/ `HeroHeight`, at most `0.8W`) from 50% to 76%, the group fits inside it; the pedestal box spans 70.5% to 81.5% (its
top ellipse about 73.8%, so the hero's feet stand on its middle, not on its back rim); the owner's win picture (B8) is
drawn from the screen's top, centered across, at least cover-sized and as large as it takes for its stone disc
(`OwnerPictures.WinStageShare` = 57.5% of the picture's height) to lie under the pedestal's top
(`OwnerPictures.TopAnchored`; about 1.27 × cover at 19.5:9), and then no drawn pedestal stands on it; the rays' center is the hero box's center; `Double` (the ×2 offer) and `Drop` (a dropped booster)
are boxes at most `0.21W` wide and `0.13W` tall beside the reward pill, right and left, `0.02W` from it. `Pause` is a
cream squircle `0.11W` square, `0.03W` from the left and `0.015W` under the top inset: no top bar shows, but Pause stays
usable over the win (FR-016), so Home, Restart and Settings stay reachable.
The animated hero (owner's delivery, FR-028, §3.12): when the celebrating family (Twig) has its frames, it stands in
`HeroMotion.Cell(Hero)` (the 8:9 cell fitted into the hero box, its foot line where the still hero's feet stand) instead
of the celebrating picture, played by a `HeroMotionPlayer(family, idleOrigin: t0)` with `Celebrate(t0)` (Twig's cheer; a hero without one reacts),
t0 the moment the hero appears (after the entrance delay the win already has: in the playtest 0.1 s after the card
shows, as it starts rising in; in Unity when the celebration shows). The cheer (Twig's 3 s) therefore plays first, from the
idle's first pose, and then the idle loops for as long as the screen shows. The hero's entrance, the rays, the petals
and the confetti stay as they were; the hero wears no outfit (as the celebrating picture). The milestone screen shows
Twig the same way. The group and the still celebrating picture stay as the fallbacks while the frames are
missing.
Playtest (`EndCards.Win`, `EndCards.Milestone`): the win fades in over the gameplay for `EndCards.WinFadeSeconds`
(0.35 s), then replaces it (`LevelScreen`); the picture hangs from the top of its box (as large as fits) and pops in;
the sign slides down, its title in two lines when one would be small (each line 34% of the plank's height), the flower
clusters 1.2 × the sign's height (at most `0.34W`) on the plank's top corners (the left one 0.18 h, the right one,
mirrored, 0.10 h under its top), the title at most the sign's width less 0.9 × a cluster wide, so it stays in the free
middle; the
group (while the cheer pictures are missing) stands with its feet just behind the middle of the pedestal's top, its
heads at the hero box's top, at most 1.15 × the pedestal wide; confetti falls behind everything for 2.2 s; a dropped
booster is its icon on a cream tile with a green "+1" badge in `Drop`; the ×2 offer needs a rewarded ad, which the
playtest does not have, so it is not shown. The milestone (frame 16) is the same full screen: the sign "Level N", its
rewards on a parchment panel in the picture's place (each on a cream tile with its amount in a cream pill), the hero on
the pedestal, "Milestone reached!" with the gold medal (the leaderboard's rank medal without a number: two deeper gold
ribbon tails behind the `medal.gold` disc, both outlined in `medal.gold` darkened 0.3, a small white star on the disc)
in the pill on the pedestal, and Continue in `Next`.
Unity (`WinScreen`, `MilestoneCard`): over the owner's win picture (pictures.md B8) the backdrop is anchored at the top
and zoomed (`OwnerPictures.WinZoom`, at least cover) so the stone disc painted in it (`OwnerPictures.WinDiscShare`, the
middle of its top at 0.58 of the picture) lies under the hero's feet (`Hero.Top + HomeStage.FeetShare × Hero.Height`),
and the drawn pedestal is left out: one stage, as on the reference. The celebration sign's flower clusters sit on the
plank's top corners (the left one 0.18 of the plank's height down, the right one 0.10), the letters at most the plank
less 0.9 × a cluster wide; the medal of "Milestone reached!" is the gold rosette (`UiKit.GoldMedal`: ribbon tails
`#E0A21A`, a `#FFC83D` disc, `#B7790F` outlines, a white star), 70% of the pill's height.

### 6.4 Home

| Element | Box |
|---|---|
| Header row | one line, Settings' middle (the owner's request of 2026-10-04: "`[Settings]  [ Petals 5090 + ]  [Avatar]`"; `ReferenceHomeRegions.Header`): Settings, the Petals pill and the Avatar |
| Settings | cream round `0.13W`, left `0.04W`, top 2.5% of H |
| Petals pill | box `0.44W × 0.105W` (`PetalsWidthShare`, `PetalsHeightShare`; before: `0.38W × 0.095W` at the right edge − `0.02W`), centered on the safe area's middle and on Settings' middle line, `0.11W` clear of Settings and of the Avatar; the pill fits its amount and stands centered in the box with its "+" (`align` 0.5), the main buttons' leaves and flower on its top-left end and on the "+"'s bottom-right edge (`GardenLook.PillDecorationBoxes`), never touch targets; its "+" opens the Store page once the Store is open |
| Avatar | the profile avatar `0.13W`, Settings' mirror: right `0.04W` from the safe right edge, top 2.5% of H (`Avatar`): the player's hero (`CharacterArt.ProfileHero`, Bloom; in its outfit once the Wardrobe is open) on a domed cream disc with a soft green middle, the chosen profile frame (1.08 of it) and the profile badge (0.36 of it, at its bottom left); no shirt badge. A tap presses and clicks; the playtest then shows the toast "Profile coming soon" (`home.profile_soon`); Unity's Home has no toast (`HomeFeatureActions.OnProfile`, none yet). The profile page comes later |
| Logo | none on Home since the owner's tuning of 2026-10-05 (FR-036); the box stays for the splash's wordmark: `0.8W` wide centered, from 10% to 20.5% of H; the owner's logo picture (C1, with transparent margins) is sized by width, `0.82W` (`ReferenceHomeRegions.LogoPicture`), so its letters span about `0.8W` and fill 10%–20.5% |
| Diorama | from 22% to 70% of H: the owner's layered Home over the whole screen with the four animated heroes on its fountain (below, "The layered Home"); else the drawn garden with the still heroes on a pedestal with the lotus fountain, centered at 50% |
| Promo scenes | in the logo's place from 17.5% of H (`PromoTopShare`, or `0.02W` under Settings): No Ads at the left, the Daily Reward at the right, each `0.2835W` wide (`HomePromo.WidthShare` × `PromoScale` 1.05) and `0.67` of that tall, `0.04W` from the edge (`ReferenceHomeRegions.NoAds`, `.DailyReward`; §6.4.1), each on a cream plate with soft shadows (FR-036, below) |
| Side buttons | the Daily Challenge (right) as a cream round button `0.13W` under the Daily Reward's promo scene with `0.086W` between them (`DailyGapShare`; `0.03W` before 2026-10-05) (at 24% of H before the promo scenes), `0.04W` from the edge (`SideButton(right, i)` for more). Since the owner's bottom menu (2026-10-04, FR-030, §6.7) the Store, the Wardrobe (the profile avatar with its shirt badge), the Collection and the rank pill are gone from Home: they are the menu's places (the avatar came back the same day, without the shirt badge, at the right of the header row) |
| Level plaque | wooden sign `0.4W × 0.068H` (`PlaqueWidthShare`, `PlaqueShare`; `0.5W × 0.085H` before 2026-10-05), centered, `0.01H` over Play |
| Play | the primary button (wood rim, decorated, breathing), `0.68W` wide, `0.12H` tall (`PlayWidthShare`, `PlayShare`; `0.85W × 0.15H` before 2026-10-05), ending over the teaser row; the label "Play" alone (no arrow), half the button's height (`ReferenceHomeRegions.PlayLabelShare`) |
| Teaser | the milestone teaser as a small parchment pill (`0.5W × 0.04H`) centered under Play, its row's touch boxes ending on the bottom menu's top (`0.015W` over it before 2026-10-05); the free booster as a cream pill beside it when offered |
| Bottom menu | the owner's wooden bar across the screen's bottom with Home in its raised medallion (§6.7) |

Fixed: the fractions apply to the safe height (`bottomReserve`, 0 in both builds since the playtest's dev row moved into
the Settings card opened from Home, lifts the bottom stack by its height); the Petals pill (without the "+" while the
Store is locked) fits its amount (§3) and stands centered in its box with its "+", on the Settings button's height, as
does the Avatar; the logo starts at 10% of H or `0.01W` under Settings, whichever is lower, and the owner's logo picture's top no
higher than the header row's bottom less a tenth of its height (its transparent margin), so its letters start under the
row; the pill's flowers (the top-left cluster `1.2` of the pill's height with its flower on the pill's rounded left end
at its top, the bottom-right one `0.9` of that, turned, its flower on the "+"'s edge) stay between Settings and the
Avatar even when the amount takes the whole box. On a 1080 × 2340 phone (insets 110 / 63) the row's middle line is
y 234: Settings spans x 43–184 and y 164–305, the Avatar x 896–1037 at the same height, the Petals box x 302–778 and
y 178–291 (119 px clear of each), the pill's flowers 136 and 122 px square, and the logo picture (886 × 325) starts at
y 278, its letters at about 310, under the row's bottom (305). The side columns start at 24% of H or `0.02W`
under the logo and stack `0.13W` buttons `0.03W` apart. The bottom stack (owner's bottom menu, 2026-10-04, FR-030) is
laid out bottom up from the menu's top (`ReferenceHomeRegions.NavTop` = `ScreenLayout.BottomNavTop`, the medallion's
top, §6.7): its limit is `0.015W` (`BottomNav.GapShare`) over it, less `bottomReserve`; the teaser row is centered half
the touch minimum over the limit, so the free booster's touch box (`0.02W` right of the `0.5W` teaser to `0.02W` from
the edge, `0.04H` tall) ends on it; Play ends 1 px over that touch box, `0.15H` tall (`PlayShare`) unless the plaque
would then rise above 60% of H (`PlaqueFloorShare`), when it shrinks, at least to `0.11H` (`PlayMinShare`) and the
touch minimum; the plaque (`0.085H`, `PlaqueShare`) stands `0.01H` (`PlaqueGapShare`) over Play. On a 1080 × 2340
phone (insets 110 / 63) the plaque spans 1413–1598 (before: 1497–1681), Play 1619–1944 at its full `0.15H` (before:
1703–2028), the teaser 1968–2055 and the menu's top is 2093; on 1080 × 1920 (63 / 0) Play shrinks to `0.126H` (234 px)
so the plaque stays at 60% (1177), and on 1080 × 2520 (120 / 66) it keeps `0.15H`, the plaque at 61%. The diorama
keeps 22%–70% of H. The rank pill (`Rank`), the Wardrobe, Collection and Store side buttons (`Wardrobe`,
`Collection`, `Store`) were removed from `ReferenceHomeRegions` with them.

**The owner's tuning** (2026-10-05, FR-036, research D29, from the constructor of the game's own layers; the
numbers are the owner's on a 1080 px wide screen, kept as shares of W): no logo; the garden blurred by `4/1080` of its
width in its picture (`layers.mjs` `gardenBlur`); the stage (the fountain's layers, the heroes, their shadows and the
petals) at `HomeLayers.Stage(screen)`, the cover box at 0.9 toward the screen's middle across and 60% of its height
down; each hero ×1.05 about its feet (`HomeLayers.HeroScale`), Twig at 0.85 of the picture's width; the heroes' Home
frames sharpened, at 110% contrast and saturation (`heroes.json` `home`, `post.mjs`); the falling petals off unless
Settings switches them on. Each promo scene stands on the round buttons' cream cushion (`ui.button.round`,
`HomePromo.PlateBox`: the scene's box grown by `0.02` of its width, corners `0.22` of the plate's width) over its soft
shadow (`UiRaster.RoundShadow`: `garden.shadow` at 0.4, blur `12.8/1080 W`, `6.4/1080 W` down); every picture of the
scene casts a soft shadow under all of them (`HomePromo.ShadowOf`, `UiRaster.SilhouetteShadow` from the picture's alpha:
`garden.shadow` at 0.55, blur `16/1080 W`, `8/1080 W` down), posed as its picture; the Remove Ads card's scene has
neither.

**The layered Home** (owner's delivery, FR-028, research D19; kit `HomeLayers` and `HomeMotion` in `HomeLayers.cs`,
the boxes in the generated `HomeLayersData.cs`). Over the owner's garden with its fountain layers (pictures.md B1),
Home and the splash draw, back to front:
1. the garden `home` (`bg.home`), cover-fitted and centered, as every Home backdrop draws it;
2. the fountain's back `home-fountain-back` (`bg.home.fountain_back`);
3. Drop, then Bloom (`HomeLayers.DrawOrder`; `BehindLotus` says which go before the lotus), each over its shadow;
4. the lotus `home-lotus` (`bg.home.lotus`), cut out of the fountain's back and drawn again over Bloom;
5. Sprig, then Twig, each over its shadow;
6. the fountain's front `home-fountain-front` (`bg.home.fountain_front`), over the heroes' feet;
7. the petals `home-petals` (`bg.home.petals`), drifting;
8. the UI: Settings, the Petals pill, the avatar, the promo scenes on their plates, the side button, the plaque, Play,
   the pills and the bottom menu (§6.7); the splash its wordmark.

Every layer over the garden lies at `HomeLayers.Place(HomeLayers.Stage(screen), layer)`, `screen` the full-screen box
the backdrop cover-fits the garden into: `Cover` lays the 852 × 1846 picture (`PictureWidth`, `PictureHeight`) over it
at the larger scale, centered (the garden's own box), `Stage` takes that box at 0.9 toward the screen's middle at 60% of
its height (since 2026-10-05), and `Place` scales a layer's box in the picture's pixels into it, so the heroes stay on
the fountain on every screen shape.

| Layer (`HomeLayers`) | Box in the picture (x, y, width × height) |
|---|---|
| `Back`: `home.jpg` | 0, 0, 852 × 1846 |
| `FountainBack`: `home-fountain-back.png` | 0, 700, 852 × 540 |
| `Lotus`: `home-lotus.png` | 294, 835, 269 × 159 |
| `FountainFront`: `home-fountain-front.png` | 0, 987, 852 × 342 |
| `Petals`: `home-petals.png` | 13, 166, 827 × 1048 (its start; it drifts) |
| `Shadow`: `home-shadow.png` | 410 × 175 (cut at 0, 918; drawn at each hero's `ShadowBox`) |

The heroes (`HomeLayers.Placement(family)`, measured on the reference's Home and fitted to the layered fountain: the
feet's middle as shares of the picture's width and height, the seam pose's height as a share of the picture's width;
`Phase`: how far into its idle loop each starts, so the four do not breathe together):

| Hero | Feet x | Feet y | Height | Where | Phase |
|---|---|---|---|---|---|
| Drop | 0.705 | 0.532 | 0.34 | the right back | 2.6 s |
| Bloom | 0.505 | 0.49 | 0.45 | behind the lotus, its feet hidden | 1.3 s |
| Sprig | 0.235 | 0.56 | 0.40 | the left rim, its feet behind the front flowers | 0 |
| Twig | 0.83 | 0.568 | 0.33 | the right rim, its feet behind the front flowers | 0.7 s |

`HomeLayers.HeroCell(picture, family)` is the hero's frame cell (§3.12): `Height` × the picture's width ÷
`HeroMotion.Fill` tall, 8:9, its foot line on the feet (without frames, the still hero's box with the figure 0.8 of
it). The shadow (`HomeLayers.ShadowBox`) is the shadow picture as wide as the seam pose (`HeroMotion.SeamWidth` of the
cell), its middle 8% of its height below the feet, at `ShadowAlpha` 0.85. The places are fixed: the player's hero does
not swap with Sprig here (it does on the drawn stand-in).

The petals (`HomeLayers.PetalsAt(picture, t)`, t the seconds since Home or the splash opened): the petals' box moved
down `PetalsSpeed` = 22 picture pixels a second, wrapping round the picture's height (a lap in about 84 s), and
sideways `PetalsSway` = 14 picture pixels × sin(2π t ÷ `PetalsSwaySeconds`), `PetalsSwaySeconds` = 7 s; drawn there
and one picture height higher, at `PetalsAlpha` 0.9, smoothly at the display rate.

The motion (`HomeMotion(start)`, made when Home or the splash appears; the splash's carries on into Home): each hero
idles from `start − Phase(family)`. `Update(now)`, called every drawn frame, starts the reactions whose turn came: the
first `FirstReaction` = 1.5 s after the start, then one every `ReactionEvery` = 6 s, in `ReactionOrder` (Bloom, Sprig,
Drop, Twig, then again), each at its hero's next seam. Each hero draws `Player(family).Pose(now)` (§3.12). Home keeps
drawing while it shows (at least the heroes' 24 fps, the petals at the display rate), under a card too.

Taps: a tap on a hero's seam picture box (`HeroMotion.PictureBox(HeroCell, Frame(family, Idle, 0))`) calls
`HomeMotion.Tap(family, now)`: the hero reacts at once, cross-fading from its idle, unless it already reacts. A hero
never takes a tap from Play, the side buttons, Settings, the Petals pill, the avatar, the plaque or the bottom menu: the
playtest cuts each hero's touch box clear of every Home control (the avatar's touch box too), of the menu from its top down and of the heroes in front of it (a part smaller than `size.touch_min` takes
none); Unity's clear touch boxes lie in the stage under the screen's controls, which keep their taps. The splash takes
no hero taps.

The splash shows the same stage from its first frame (the fountain is part of the garden at once), its heroes fading in
on it (the playtest also lifts them by `0.04W`, as the drawn stand-in's heroes rise in) in Home's motion and outfits, so
Home takes over without a jump. Once the Wardrobe is open each hero wears its outfit (§3.12).

Fallbacks: a family without frames shows its still hero in its cell; without the fountain layers Home shows the garden
alone, with no heroes (`HomeStage.ShowsHeroes`, Unity `HeroPictures.StageOf`); without the shadow or the petals
picture those are left out. The earlier measurement for the single Home picture (`HomeStage.AroundFountain`, removed)
is superseded by `HomeLayers.Placement`.

The drawn diorama (without the owner's picture) is `HomeStage.ReferenceDiorama(stage)` (in `u = min(0.88 × stage
width, stage height / 1.09)`, retuned for the owner's larger heroes): the well's stone ring `0.78u` wide with its foot
`0.09u` above the stage's bottom, the lotus fountain on it, Bloom raised behind the fountain (`0.64u` picture, feet
`0.51u` up), Drop at the right back (`0.44u` at `+0.30u`), Sprig at the left (`0.74u` at `−0.26u`), Twig in front at the
right (`0.48u` at `+0.37u`), so Bloom's eyes stay clear of Drop. The playtest shows the Daily Challenge's side button
once unlocked (`r.Daily`) and the bottom menu (§6.7); its splash shows the logo and the diorama in the same boxes. Its
header row draws Settings (`Kit.RoundButton`), the pill (`Kit.PetalsPill(align: 0.5f, decorate: true)`, the reward's
sparkles on its lotus) and the avatar (`HomeScreen.Avatar`, slot `ui.button.round` with the hero's and the cosmetics'
slots); a tap on the avatar runs `DesignApp.OpenProfile` (the click, then Home's toast).

Unity (`HomeScreen`, `SplashScreen`): the header row, Settings (`UiKit.RoundIconButton`), the pill
(`UiKit.PetalsPill(align: 0.5f, decorate: true)`) and the avatar (a clear `Button` in `r.Avatar` with `PressMotion` and
the click, holding a `ProfileAvatar` shown with `HomeModel.Profile` less its marker and `HomeModel.AvatarOutfit`; built
after the stage, so the heroes never take its taps); the Daily Challenge, the sun `ui.sun` with the green check badge
when done today, in `r.Daily`, and the bottom menu (§6.7, `BottomNavView`, built last over the stage, Home in its
medallion; its Leaderboard, Shop and Wardrobe places are the targets of the L10, L12 and L40 Home demos,
`HomeScreen.DemoTarget`; the profile avatar, `ProfileAvatar`, also stays in the Wardrobe's profile tab); the logo
shows in both looks, the owner's logo picture sized by width (`ReferenceHomeRegions.LogoPicture`: `0.82W` wide,
centered on the logo box, its top no higher than a tenth of its height above the header row's bottom); over the owner's layered Home both looks show its stage with the four animated heroes
(`HeroPictures.Stage`, `HomeLayersView` with one `HeroMotionView` per hero, built under the screen's controls;
`HeroPictures.StageOf` picks the stage); without the picture, the drawn `HomeStage.ReferenceDiorama`, where once the
Wardrobe is open each hero wears its outfit and the player's hero (`ProfileAvatar.HeroFamily`) swaps places with Sprig
at the left front; Play shows its label alone, `ReferenceHomeRegions.PlayLabelShare` of its height; the Petals pill
without its "+" starts the amount right after the lotus; the plaque is `0.5W`, wider when its letters need it (at most
`0.8W`); the free booster is the cream `CostPill` "Free" and takes taps in a clear box grown to `size.touch_min`; the splash takes the Home garden while its own picture is missing (`OwnerPictures.Resolve`)
and puts its logo and its heroes where Home shows them; over the layered Home the splash and Home share one `HomeMotion`
while both show, so Home takes over the splash's motion without a jump.

#### 6.4.1 The promo scenes and the Remove Ads card (spec FR-032, FR-033)

The owner's layers (pictures.md D14–D22, `Decor/promo-*.png`, whole canvases: the stands 1448 × 1086, the others
1254 × 1254, fitted into 724 × 543 and 512 × 512) placed and timed by `HomePromo` for both builds; a host draws
`HomePromo.Layers(scene, box, seconds since Home opened, calling)` back to front: each picture's canvas in its box, then
scaled and turned clockwise around its pivot, at its alpha (Unity `HomePromoView`; the playtest's painter with
`PushRotate`/`PushSquash`), and the label right after the stand.

| Part | Recipe |
|---|---|
| Stand | the owner's stone stand with flowers, the scene box's width, its canvas from `70/1448` of that under the box's top |
| Label | `home.promo_no_ads` "No Ads" / `home.promo_daily` "Daily" centered on the plaque's face (`HomePromo.Plaque`), letters 0.74 of its height in `type.level_pill` with the sign letters' look (`ink.brown`, light emboss), fitted to its width |
| No Ads, idle (2.4 s) | Sprig (0.38 of the scene's width) breathing (`1 → 1.03` tall, `1 → 0.99` wide, from his feet) and swaying (`+6/240` of the width, −3°); the crossed AD sign (0.31) in front of him, leaning 4° ± 2° away over its bottom-right corner, its left edge on his front palm (the palm's tip hidden by `75/1072` of his width); no lotus |
| No Ads, attention (3.2 s) | Sprig crouches (0.14 s: back `8/240`, `0.92` tall, +6°; the sign stays) and lunges (to 0.42 s: `+18/240`), pushing the sign, which tips to 16°; the sign flies off to the right (to 0.85 s: `+0.38` of the width, 50°, shrinking to 0.8, fading from 0.55 s); Sprig hops (`10/240`) and is gone (0.66–0.84 s); the lotus blooms where they stood (0.62–1.15 s: `0.4 → 1.12 → 1`, then breathing) until 2.6 s; then it folds away (to 2.9 s) and the two pop back (`0.6 → 1.06 → 1`, to 3.2 s) |
| Daily, idle (2.6 s) | the closed album (0.37 of the scene's width) floating (`−4/240`, −2°, `1.02`) on the stand; no sparkles, no badge |
| Daily, attention (2.6 s) | the album squashes (0.15 s: `0.94`, −4°) and the open album pops in (`0.85 → 1.05 → 1`, a small rise); the flower stamp (0.14) fades in over the right page (0.5–0.7 s), hovers, lifts (to 1.05 s) and presses down (1.27 s, squashing to `0.92`, the album dipping to `0.97`), settling at −8°; the petals and sparkles (0.6) burst out of the album only then (1.3–2.4 s: `0.5 → 1 → 1.15`, rising `20/240`, fading); the album closes again (2.2–2.55 s) |
| Schedule | every 12 s from Home's opening: No Ads at 1 s, the Daily at 6.5 s (never together); the Daily only while the reward waits (`calling`), else it idles |
| Taps | the scene box (at least the touch minimum) presses like a button: No Ads opens the Remove Ads card, the Daily the Daily Reward card |
| Stand-in | while a picture is missing: the label on a wooden sign (`ui.sign.wood`) in the scene box |

The Remove Ads card: the usual popup card (`ui.card`: parchment, the title, the close), "No Ads" as its title, the No
Ads scene idling (`calling` false) centered in its top part, `remove_ads.body` in `type.body`, the primary button
`remove_ads.price` ("Remove Ads · {price}"; `store.unavailable` disabled while purchases are off, with `store.offline`
under it) and the secondary `remove_ads.restore`. Once Remove Ads is owned: `remove_ads.owned`, the card closes and
Home's No Ads scene is gone.

### 6.5 Wardrobe (both builds)

| Element | Box |
|---|---|
| Header row | one line, the back button's middle (`PageHeader.CenterY`): the back button, the banner and the Petals pill centered on it (the owner's note of 2026-10-04: "the elements there are not on one line"; the banner sat about 0.04 W lower before, 46 px on a 1080 × 2340 phone); the banner, and so the page's title, in the middle of the screen (the owner's later note of 2026-10-04: the titles were off to the left, the banner's middle at 43% of W) |
| Back | cream round `0.12W`, left `0.04W`, top 2.5% of H |
| Banner | wooden sign with ivy, plank `0.1W` tall, its ivy clusters at 0.8 of their usual size (`PageHeader.IvyScale`: `0.1W`), centered on the screen's middle, reaching to the Petals box's left − `0.005W` − its right cluster's reach (`0.054W`): about 32% to 68% of W on every phone |
| Petals pill | box `0.24W × 0.068W` (from 74% of W), right edge − `0.02W`; the pill fits its amount at the box's right end ("1 240" with its "+" fills it) |
| Hero | the selected family's hero in the worn outfit, from 11% to 37% of H, on a pedestal `0.6W` wide (35%–43%) |
| Arrows | cream round ‹ › `0.09W` at 8% and 92% of W, 28% of H (previous/next family) |
| Name card | parchment from 42% to 57% of H, `0.92W`; a sign-like tab `0.5W` with the name (`type.title`), the role line, the description in two lines |
| Family tabs | from 56% to 68.5% of H, four tabs `0.24W` with the family's hero head and name; the selected one lighter and joined to the panel below |
| Outfit panel | parchment from 67% to the bottom (behind the bottom menu, §6.7): three cards per row `0.29W × 0.18H` with the hero wearing the item and its name; the worn one green with a check badge; pages or scroll for more |
| Footer | "Earn special outfits as you play!" at 93% of H, above the bottom menu |
| Bottom menu | the owner's wooden bar over the panel's foot, the Wardrobe in its medallion (§6.7) |

Fixed: the header (`ScreenLayout.PageHeader`, record `PageHeader`) is shared with the Store, Leaderboard and Collection
pages (§6.6, §6.8, §6.9); the banner's
ivy clusters (`PageHeader.BannerExtent`, `GardenLook.SignExtent` at `PageHeader.IvyScale`) keep `0.005W` from the
Petals box and more from the back button (and clear their touch boxes) on every phone from 16:9 to 21:9, the banner's
middle is the safe area's, and the plank less 1.25 × 0.8 of its height (the letters' room between the owner's ivy) is
`0.262W`, so "Wardrobe" shows at about 95% of `type.title`, "Collection" a little smaller and "Leaderboard" at about
80%; the hero box is an 8:9 box from
11% to 37% of H; the name tab spans 42%–47.5%, the role line 47.5%–50.5%, the description 50.5%–56%; `Tab(i, n)`
splits the tabs' `0.96W` into n tabs at most `0.24W` wide with `0.01W` between them (five with the Unity profile tab);
the panel spans `0.96W` to the screen's bottom; without chips the cards span 70%–88% of H, with the kind chips (`hasChips`, 69.5%–74%) 75%–89.5%; `Card(i)` is `0.29W` wide,
spread over `0.92W`; the footer box spans 91%–95% of H between the page arrows (`0.09W` at 8% and 92% of W). Since the
bottom menu (2026-10-04, FR-030) the fractions under the header apply to the page's height H′ = (the menu's top −
half the touch minimum − the safe top) / 0.93, so the page arrows' touch boxes, centered on the footer at 93%, end on
the menu's top (`ReferenceWardrobeRegions.NavTop`): H′ is 0.951 of the safe height on 1080 × 2340 (the footer at
1986–2069, the cards 1553–1924 without the chips), 0.931 on 1080 × 1920 and 0.960 on 1080 × 2520; the header keeps the
safe height's 2.5%. One row of three cards still fits. Before the Wardrobe opens (L40) the page shows locked: its header
and the lighter panel with the locked notice, nothing else (§6.7, `ScreenLayout.LockedPage`).

### 6.6 Store page (both builds)

The owner's note of 2026-10-04: "The Store must be a separate page, not a popup." A full-screen page like the
Wardrobe (`ScreenLayout.ReferenceStore(width, height, insets, hasCosmetics, hasStatus)` → `ReferenceStoreRegions`),
over the Wardrobe's garden; everything the card offered stays (FR-002: the rows, prices, purchases, tabs and states).

| Element | Box |
|---|---|
| Header row | the page header of §6.5 (`Header`): back (returns to where the Store was opened), the "Store" banner with ivy, the Petals pill (playtest: its "+" says the Petal packs are offline; Unity: no "+", the packs are Shop rows) |
| Panel | parchment `0.96W` wide (`0.02W` from the sides) from `0.03W` under the header row to the bottom of the screen (drawn a radius further, so no bottom corners show; the bottom menu lies over its foot, §6.7), radius `radius.card` of its width (at least `radius.card_min`) |
| Tabs | Shop / Cosmetics (`hasCosmetics`, after L40) `0.8W × 0.1W`, `0.045W` under the panel's top |
| Status | Unity's "Purchases are unavailable offline" (`hasStatus`) `0.88W × 0.05W`, `0.02W` under the tabs |
| List | `0.88W` wide (`0.06W` from the sides) from `0.04W` under the tabs (`0.02W` under the status line; `0.045W` under the panel's top without either) to `0.02W` over the bottom menu's top (`NavTop`; `0.04W` over the safe bottom before the menu) |
| Shop rows | `Row(slot, count)`: cream rows across the list from its top, `0.135W` (`RowShare`; `0.15W` before the bottom menu) to `0.19W` tall (`RowHeight`: a page of rows filling the list), `0.025W` apart, their names sized for a `0.15W` row (`RowTypeShare`) and grown or shrunk with it; `RowsPerPage(count)`: all the rows when they fit at `0.135W`, else as many as fit above the footer; the Shop's seven playtest rows fit one page on every phone |
| Cosmetics | `FamilyTabs` `0.21W` tall across the list's top; `OutfitPanel` (the lighter panel) from there to the list's bottom; `OutfitGrid` the panel less `0.02W`, above the footer; `OutfitCard(slot)` three to a row, `0.02W` apart, `OutfitRows` rows (as many as fit at 1.15 × the card's width, at least two), each card at most 1.45 × its width tall (its cost pill's room included) |
| Bottom menu | the owner's wooden bar over the panel's foot, the Shop in its medallion (§6.7) |
| Footer | `0.84W` wide, `max(0.12W, size.touch_min)` tall, `0.02W` over the list's bottom: "Page n / m" (Shop, only with more than a page) or "Earn special outfits as you play!" (Cosmetics) between the page arrows `0.09W` at its ends (`PagePrevious`, `PageNext`; touch-sized) |

Fixed: on a 1080 × 2340 phone (insets 110 / 63, the preview's 19.5:9) the header's line is at y 229, the panel starts
at 329, the tabs span 378–486, the list 529–2072 with the footer at 1918–2050 (529–2234 and 2080–2212 before the bottom
menu); the seven Shop rows are 197 px tall (`0.183W`; 205 px before); the cosmetics show three rows of 288 × 359 cards
(288 × 413 before; two rows on 16:9, three on 21:9, which showed four before the menu). Back returns to Home, or to the
page whose bottom menu's Shop or Petals "+" opened it (the Wardrobe; the Leaderboard and the Collection since they are
pages, §6.8, §6.9; the playtest's `DesignApp.StoreReturn`; Unity's page lies over the screen that opened it and hides);
the Android system back closes the page (and the other pages) as their back buttons do. Every entry point opens the
page: the bottom menu's Shop (§6.7) on Home or any page, and their Petals "+" (Home's Store
button until 2026-10-04). Before the Store opens (L12) only the bottom menu's Shop opens it (the Petals pills show no
"+" yet), locked: its header and panel with the locked notice in the list's box (§6.7, `ScreenLayout.LockedPage`).

### 6.7 Bottom menu (both builds; owner's request of 2026-10-04, the wooden variant)

The owner: "We also need to add a bottom menu. You will find the icons in the zip. On the picture you will find
variants. Try the wooden variant." Of the five bar styles on the owner's picture, the second row: a wide warm brown
wooden plank across the screen's bottom with wood grain and rounded ends, thin vertical grooves between the places, the
icons on the plank, and the active place in a raised round wooden medallion (a lighter wood disc in a darker rim, with
vines and two small white flowers) rising over the plank's top. The owner's review of the same day: "Remove the branches
to the right and left of the menu itself. And make the menu icons bigger: they must take more of the menu's plank, and
the free room on the plank must be minimal." So the plank has no vines at its ends (the medallion keeps its own), it is
`0.14W` tall instead of `0.12W`, the places share its whole length, and each icon is the plank's full height (the
owner's pictures keep their own thin margin): 151 px instead of 111 px on a 1080 px wide phone. The medallion rises
`0.03W` instead of `0.05W`, so the menu's top stays `0.17W` above the safe bottom and Home, the Store page and the
Wardrobe keep their room. Home, the Store page (§6.6), the Wardrobe (§6.5) and, since the owner's request of the same
day ("All the menu's places must be a separate page. Not popups."), the Leaderboard (§6.8) and Collection (§6.9) pages
show it; gameplay, the win, milestone, jam and pause
cards and the splash do not (FR-030). Kit: `BottomNav.cs` (`NavPlace`, `BottomNav`, `BottomNavRegions`,
`NavBarShape`, `ScreenLayout.BottomNav`, `ScreenLayout.BottomNavTop`), `LockedNotice.cs` (`LockedNoticeRegions`,
`LockedPageRegions`, `ScreenLayout.LockedNotice`, `ScreenLayout.LockedPage`) and `NavRaster.cs` (`UiRaster.NavBar`,
`UiRaster.NavMedallion`); components `Kit.BottomNav`, `Kit.NavLock` and `Kit.LockedNotice` (playtest, `KitNav.cs`) and
`UiKit.BottomNav` → `BottomNavView`, `UiKit.LockBadge` and `UiKit.LockedNotice` → `LockedNoticeView` (Unity,
`UiKitNav.cs`).

**Places** (`NavPlace`, `BottomNav.Order`), left to right: Shop, Wardrobe, Home, Leaderboard, Collection. All five
always show, from Level 1 (the owner's request of 2026-10-04: "The menu's places must always be visible. But if some
things are available only from a level, then on entering the menu's place the page must say that it is only available
after reaching level N."), keeping their order and sharing the span evenly. A place is open once its feature is
unlocked (`BottomNav.IsOpen(place, HomeLook)`): the Shop with `HomeLook.Store` (L12), the Wardrobe with
`HomeLook.Wardrobe` (L40), the Leaderboard with `HomeLook.Rank` (L10), the Collection with `HomeLook.Collection` (a
picture won, so from L2); Home always. `BottomNav.UnlockLevel(place, levelOf)` is the level a locked place names: the
build's own roadmap (`UnlockRoadmap.LevelOf` of `HomeLook.StoreUnlock`, `WardrobeUnlock`, `LeaderboardUnlock`: 12, 40,
10; the playtest's `PlaytestMeta.Progression.Roadmap`, Unity's `ProgressionService.Roadmap` in `HomeController`), the
Collection's `BottomNav.CollectionLevel` 2 (its first picture comes with Level 1's win), Home's 1. The active place
(the screen's own: Home on Home, the Shop on the Store page, the Wardrobe on the Wardrobe, the Leaderboard and the
Collection on their pages) sits in the medallion and takes no tap; a tap on another, with the click, goes straight
there from any page: the Shop opens the Store page (over the page that shows the menu, so its back returns there), the
Wardrobe the Wardrobe, Home returns to Home, the Leaderboard and the Collection open their pages (until the owner's
request of 2026-10-04 they opened cards over Home). No card opens over Home from the menu. A locked place opens its
page all the same, which shows the locked notice instead of its content (below). Back (the page header's back button
and the Android system back) returns from the Wardrobe, the Leaderboard and the Collection to Home, from a Collection
picture's detail to its grid first. The playtest's `DesignApp.Navigate` (`PlaceOpen`, `UnlockLevel`, `ActivePlace`,
the `Screen.Leaderboard` and `Screen.Collection` pages, `Back`), Unity's `HomeController` (`Navigate`, `NavLook`,
`StoreFrom`: it shows the place's page, or Home, and hides the others).

| Region (`BottomNavRegions`) | Box |
|---|---|
| `Bar` | the bar's picture: the whole screen's width, from the plank's top to the screen's bottom (no vines over it since the owner's review) |
| `Plank` | `0.14W` tall (`PlankShare`) from `0.03W` to `0.97W` (`EndShare`), its bottom on the safe bottom; the wood runs on behind the bottom inset to the screen's bottom |
| `PlaceBoxes` | the shown places' columns on the plank band, sharing `0.04W` to `0.96W` (`SpanStart`, `SpanEnd`) evenly: five are `0.184W` wide, centered at 13.2%, 31.6%, 50%, 68.4% and 86.8% of W |
| `Icon(i)` | a place's icon: a square the plank's full height (`IconShare` 1, no wider than its column) on its column's and the band's middle; the active place's `0.86` of the disc (`MedallionIconShare`) on its middle, a little larger than the plank's |
| `Medallion` | `0.2W` square (`MedallionShare`) centered on the active place, its top `0.03W` (`RiseShare`) over the plank's top, so it reaches `0.03W` under the plank into the inset; smaller where its disc would leave the screen (no bottom inset: its disc ends on the screen's bottom, `0.18W` on 1080 × 1920) |
| `Disc` | the medallion's wooden disc, `0.88` of its box (`DiscShare`); its leaves and flowers take the rest |
| `Touch(i)` | a place's touch box: its column from the safe bottom up the plank's height, at least `size.touch_min` (`TouchMin`); a neighbor of the active place cut clear of the medallion while it keeps the touch minimum; the active place has none (`Buttons` lists the others) |
| `Top` | the menu's highest point, the medallion's top, `0.17W` over the safe bottom (`ScreenLayout.BottomNavTop`, the same whatever the places): Home's bottom stack and the four pages' content end above it |
| `BottomNav.LockBox(Icon(i))` | a locked place's padlock badge (`ui.nav.lock`): a square `0.34` of its icon's side (`LockShare`, the whole badge with its ring) at the icon's lower right, `0.03` of the side (`LockInsetShare`) inside its right and bottom edges, so inside the plank's band; never on the active place |

Fixed: on a 1080 × 2340 phone (insets 110 / 63) the plank spans 2126–2277 (x 32–1048), the bar's picture 2126–2340,
the five places 43–1037 (199 px each), their touch boxes 2126–2277, the medallion 432–648 × 2093–2309 on Home (its disc
190 px, its icon 163 px; the plank's icons 151 px), and the menu's top is 2093. On 1080 × 1920 (63 / 0) the plank spans
1769–1920 and the medallion 195 px from 1736; on 1080 × 2520 (120 / 66) the plank 2303–2454, the top 2270. A locked
place's padlock badge is 51 px (the Shop's at 162–213 × 2221–2272 on 1080 × 2340), inside the band.

**Locked places** (the owner's request of 2026-10-04): the menu draws a locked place's icon unchanged, still tappable,
with the padlock badge (`ui.nav.lock`, `Kit.NavLock` / `UiKit.LockBadge`) over its lower right: the outfit cards'
`Kit.LockBadge` recipe, a domed cream disc (`cream.top` to `cream.face`, `BottomNav.LockDisc`: the box over 1.16) in a
`cream.line` ring 8% of the disc a side, the brown `ui.lock` (`ink.brown`, 56% of the disc), over a soft shadow; the
badge squashes with its icon when pressed. A locked place's page shows the locked notice (`ui.locked.notice`,
`Kit.LockedNotice` / `UiKit.LockedNotice`) instead of its content:

| Screen when locked | What it shows |
|---|---|
| Store page (before L12) | its garden, header (back, "Store" banner, the Petals pill without its "+": the Store it would open is this one) and parchment panel (`ScreenLayout.LockedPage`: the §6.6 layout without tabs and status line), the notice in `Notice` (the list's box) instead of the tabs, rows, page arrows and offline line; the bottom menu with the Shop raised; back as usual; no `store_open` (Unity), it is not a Store visit |
| Wardrobe (before L40) | its garden and header (back, "Wardrobe" banner, the Petals pill, its "+" opening the Store page once open), the page's lighter panel (`parchment.top` to `cream.top` with a `cream.line` outline, radius 26 units, `Kit.Panel`) in `LockedPage.Panel`, the notice in `Notice`, instead of the hero, name card, tabs, cards and footer; the bottom menu with the Wardrobe raised |
| Leaderboard page (before L10) | its garden, header (back, "Leaderboard" banner, the Petals pill, its "+" opening the Store page once open) and parchment panel (`ScreenLayout.LockedPage`, §6.8), the notice in `Notice` (the page's area) instead of the rows, the status line and Refresh; the bottom menu with the Leaderboard raised (a locked card until the owner's request of 2026-10-04: every place a page) |
| Collection page (before its first picture) | the same, "Collection", instead of the count, the pictures and the page arrows (§6.9; the playtest also opens it from Level 2 while it is empty: its dev row's skips collect no pictures) |

Home's and the Wardrobe's Petals pill take a tap only while their "+" shows, so before L12 a tap on the pill does
nothing in either build (Unity's pill took the tap with its "+" hidden until 2026-10-04 and opened the whole Store page);
the locked Store page opens from the menu's Shop.

| Region (`LockedNoticeRegions`, `ScreenLayout.LockedNotice(area)`) | Box (A: the area's width) |
|---|---|
| `Icon` | the place's owner icon (`OwnerPictures.NavIcon`; its stand-in glyph in `BottomNav.GlyphBox` while missing) `0.4A` square (`IconShare`), on the area's middle line; on a short area it shrinks to fit, down to `0.2A` (`MinIconShare`), below which the whole stack shrinks with it |
| `Badge` | the padlock badge, `BottomNav.LockBox(Icon)` (the menu's recipe, 0.34 of the icon at its lower right) |
| `Message` | "Available from level N" (`locked.message`), `0.05A` under the icon (`IconGapShare`), `0.94A × 0.1A` (`TextWidthShare`, `MessageShare`), in `type.title` `ink.brown` (plain look), its letters `0.72` of the line (`MessageTextShare`), shrunk to its width |
| `Hint` | "Keep playing to unlock it!" (`locked.hint`), `0.012A` under the message (`HintGapShare`), `0.94A × 0.07A` (`HintShare`), in `type.body` `ink.brown_soft`, its letters `0.66` of the line (`HintTextShare`), shrunk to its width |

The stack (`0.632A` tall at full size) is centered in its area; every share is of A, so the notice keeps its shape in
screen pixels and in Unity's canvas units alike. It is never a touch target. Fixed: on 1080 × 2340 (110 / 63) a locked
page's panel starts at 329 and its notice's area spans 65–1015 × 377–2071 (where the Store page's list would be): the
icon 380 px at 350–730 × 924–1304 with its badge 129 px, the message line 893 × 95 at 1352 (68 px letters), the hint
893 × 67 at 1458 (44 px letters), the same on the four locked pages (`LockedNoticeRegions.CardContent` and the locked
cards are gone). On 1080 × 1920 (63 / 0) the area spans 323–1715, on 1080 × 2520 (120 / 66) 392–2249, the notice the
same size.

**Recipe** (engine-free pictures, both builds draw the same bytes; cached by key and size):
- The bar (`ui.nav.bar`, `UiRaster.NavBar(width, height, NavBarShape)`, key `ui.nav.bar/…` from `BottomNavRegions.Shape`:
  the plank's ends, top and band bottom as shares of the bar's box, and the grooves' places): the plank in the warm
  dark wood (`WoodTone.Dark`: `wood.dark_top` to `wood.dark`, its grain along its length, mixed 30% toward
  `wood.dark_top` for warmth, with long darker `wood.dark_line` and lighter streaks, the band lit from above), its top
  corners rounded by 0.42 of the band's height (the bottom ones lie below the screen), a light bevel inside its top
  edge and ends and the `wood.dark_line` outline; the band's lower edge a little deeper, the wood under it (behind the
  bottom inset) darker; and between two places a carved groove half the band tall (a `wood.dark_line` line 3% of the
  band wide with a light line beside it). No vines at its ends (the owner's review of 2026-10-04).
- The medallion (`ui.nav.medallion`, `UiRaster.NavMedallion(size)`): a soft `garden.shadow` under it; the disc's rim
  in the bar's wood (a fifth of its radius, lighter at its top, the `wood.dark_line` outline and a thin line inside);
  its face light honey wood (`WoodTone.Light` mixed 35% toward `wood.grain`, lit from the upper left toward
  `wood.light`, deeper toward `wood.edge` at the lower right, a soft shadow under the rim's top, a faint growth ring);
  short green stems along the rim (`garden.leaf_3` with a `garden.leaf_line` outline and a lighter middle) with eight
  almond leaves at its four corners in the `garden.leaf_*` greens (one side lighter, a `garden.leaf_line` outline and
  midrib) and two small white five-petal flowers (`garden.flower`, `garden.flower_line`) with yellow middles
  (`garden.flower_center`), at the lower left and the right.
- The icons (`icon.nav.shop|wardrobe|home|leaderboard|collection`): the owner's pictures `Icons/nav-*.png`
  (`OwnerPictures.NavIcon`, pictures.md D9–D13) fitted into `Icon(i)` with their aspect kept; while a picture is
  missing, the place's glyph (`BottomNav.Fallback`: the reward basket, the shirt, the fountain, the trophy, the grid in
  their colors over a darker outline) in `BottomNav.GlyphBox` (the icon box less 14% a side). A pressed place's icon
  squashes like a tile (spec 003 FR-017).

Drawn: the playtest draws the bar after the screen's content (its places' hits over everything under them), Unity
builds one `BottomNavView` into each of the five screens (Home and the four pages) over its content (the bar's picture
takes the taps that fall on it, so none reach Home's stage; the medallion takes no tap; each place is a clear touch
target with its icon). The heroes' tap boxes on Home keep clear of the menu from its top down.

### 6.8 Leaderboard page (both builds; owner's request of 2026-10-04)

The owner: "All the menu's places must be a separate page. Not popups." (translated from Russian). The Leaderboard
card over Home (§4.3 until then) becomes a full-screen page on the Store page's frame
(`ScreenLayout.ReferenceLeaderboard(width, height, insets)` → `ReferenceLeaderboardRegions`, built on
`ScreenLayout.LockedPage`, so the four pages line up); it holds what the card showed, laid out for the page, and keeps
its data and rules (FR-002): the offline last page, the stale notice and the player's frame, badge and marker on their
own row (Unity), `leaderboard_view` on open.

| Element | Box |
|---|---|
| Header row | the page header of §6.5 (`Header`): back (to Home), the "Leaderboard" banner with ivy (`leaderboard.title`), the Petals pill (its "+" opens the Store page over this page once the Store is open; its back returns here) |
| Panel | the Store page's parchment panel (`Panel`, §6.6): `0.96W` from `0.03W` under the header row to the bottom of the screen |
| Area | the Store page's list box (`Area`, the locked page's `Notice`): `0.88W` from `0.045W` under the panel's top to `0.02W` over the bottom menu's top |
| Refresh | the cream secondary button with ⟳ (`leaderboard.refresh`), `0.54W` × `max(0.12W, size.touch_min)`, centered, `0.02W` over the area's bottom (where the Store page's footer is) |
| Status | `0.06W` tall across the area, `0.02W` over Refresh: "Offline: showing the last known ranks" when the page is stale, else empty (the playtest's offline form: "Offline: the leaderboard will update when you reconnect") |
| Rows | from the area's top to `0.02W` over the status line; `Row(line, lines)`: cream rows across it from its top, `0.11W` (`RowShare`) to `0.13W` (`RowMaxShare`, what the parts leave room for) tall, `0.02W` apart, filling the box; `LinesFitting` at `0.11W` (at least `MinLines` 8 on every phone from 16:9), `LinesShown(lines)` all of them when they fit, else `FirstLine(lines, shown, focus)` the window that keeps the player's own row in view, as near its middle as the ends allow |
| Row parts | `Parts(row)` (`LeaderboardRowParts`), positions as shares of the row's width as on the card, sizes of its height: the rank `0.13w` wide on its middle at `8.2%` (its medal `0.75h`), the portrait `0.7h` at `20%`, the name from `27%` to `60%`, the marker and the badge `0.55h` at `65.5%` and `74.5%`, the score `18.8%` wide on its middle `10.6%` from the right end; the letters drawn for a `0.11W` row (`RowTypeShare`) grow with the row |
| Empty | an empty board's line ("No ranks yet", or "Offline: the leaderboard will update when you reconnect" before the first read) on the rows' middle (`Empty`, `0.06W` tall) |
| Bottom menu | the owner's wooden bar over the panel's foot, the Leaderboard in its medallion (§6.7) |

Fixed: on 1080 × 2340 (110 / 63) the panel starts at 329, the area spans 65–1015 × 377–2071, the rows 377–1810 (ten
lines fit and fill them, 124 px each, 21 px apart; fewer lines grow up to 140 px, `0.13W`), the status line 1832–1897 and Refresh 248–831 ×
1918–2050; a row's parts at 1080 px: the rank 81–205 (its medal 105 px), the portrait 98 px at 206–304, the name
321–635, the marker 649–726, the badge 734–811, the score 825–1004. On 1080 × 1920 (63 / 0) the rows span 323–1453
(eight lines fit, 122 px each), Refresh 1561–1693; on 1080 × 2520 (120 / 66) the rows 392–1988 (eleven fit, 125 px
each), Refresh 2095–2227. The rows fill the panel's height (the owner's choice of 2026-10-04): the playtest's offline
page shows as many lines as fit, the placeholder ranks (`LinesFitting` − 2: six on 16:9, eight on 19.5:9, nine on
21:9), "…" and "You"; Unity reads five players above and below the player and the top eleven
(`LeaderboardClient.Neighbours`), enough for the eleven lines of 21:9. Back and the Android system back return to Home (the playtest's `DesignApp.CloseLeaderboard` and `Back`;
Unity's page lies over Home and hides). Before L10 the page shows locked: its header and panel with the locked notice
in the area (§6.7, `ScreenLayout.LockedPage`).

### 6.9 Collection page (both builds; owner's request of 2026-10-04)

The Collection card over Home (§4.3 until the owner's request of §6.8) becomes a full-screen page on the Store page's
frame (`ScreenLayout.ReferenceCollection(width, height, insets)` → `ReferenceCollectionRegions`, built on
`ScreenLayout.LockedPage`); it keeps its data and rules (FR-002): every finished picture, newest first, Unity's redraw
from the current content (or the level number when it cannot), `collection_open` on open; it is never a level selector.

| Element | Box |
|---|---|
| Header row | the page header of §6.5: back (from a picture's detail to the grid, from the grid to Home), the "Collection" banner with ivy (`collection.title`), the Petals pill (as on the Leaderboard page) |
| Panel, area | the Store page's (§6.8) |
| Count | "N pictures" (`collection.count_one`, `collection.count_many`) in `type.caption` `ink.brown_soft`, `0.06W` tall across the area's top (`Count`) |
| Grid | from `0.02W` under the count to the area's bottom (`Grid`); `Cell(slot)`: square frames, three to a row (`Columns`), `0.03W` apart (`CellGapShare`), as large as fit (`CellSize`, about `0.273W`, or down to `0.84` of it, `MinSideShare`, when one more row then fits above the footer: the owner's choice of 2026-10-04, as many rows as fit), centered across the grid, rows from the grid's top; `RowsFitting(footer)` the rows that fit, above the footer when there are more pages; `PerPage(count)` all the pictures when they fit, else as many full rows as fit above the footer; `Pages(count)` |
| Footer | the Store page's footer line (§6.6): `0.84W` × `max(0.12W, size.touch_min)`, right under a page's last row (`0.03W` lower; at most `0.02W` over the area's bottom), "n / m" (`common.page`) between the page arrows `0.09W` at its ends (`PagePrevious`, `PageNext`, touch-sized), only with more than one page |
| Detail | a picture's detail in the area's place, centered in it: `Picture` square, `0.8W` at most (less on a short area), then `0.04W` lower `Name` (`0.88W × 0.1W`, the picture's name in sentence case in `type.title` `ink.brown`) and `0.01W` lower `Level` (`0.07W`, "Completed at Level N", `collection.completed`, in `type.body` `ink.brown_soft`) |
| Bottom menu | the owner's wooden bar over the panel's foot, the Collection in its medallion (§6.7) |

Fixed: on 1080 × 2340 (110 / 63) the count spans 377–442, the grid 464–2071 with 258 px frames 32 px apart across
120–960 (295 px would leave a gap: four rows): five rows (fifteen a page) above the footer, the footer 86–993 ×
1918–2050 right under them with the arrows at 104–201 and 879–976; a picture's detail is 864 px at 108–972 × 674–1538, its name's line at 1581 and its level's at
1700. On 1080 × 1920 (63 / 0) four rows of 256 px frames (twelve a page) and the footer at 1561–1693, the detail's
picture at 468–1332; on 1080 × 2520 (120 / 66) five rows of 291 px frames (fifteen a page), the footer at 2095–2227, the
picture at 769–1633. The preview's frame 6 shows the 87 pictures of a player at Level 88 (six pages on 19.5:9 and 21:9,
eight on 16:9), frame 20 a picture's detail. Back and the
Android system back return from the detail to the grid, then to Home (the playtest's `DesignApp.CollectionBack`;
Unity's `CollectionScreen` back). Before the first picture the page shows locked: its header and panel with the locked
notice in the area (§6.7).

### 6.10 Guided spotlight (both builds; the owner's request of 2026-10-05, FR-035)

The onboarding's guided steps (`GuideTour`: which steps, `Spotlight`: where; drawn by the playtest's `GuidePainter`
and Unity's `GuideOverlay`), over the gameplay screen of §6.1, slot `ui.spotlight`. All sizes in reference units
(`u`, `DesignTokens.ScaleFor`) or screen pixels.

| Piece | Recipe |
|---|---|
| Holes | the lit places, each grown by `PadUnits` (14 u): the arches' bounds (Entry); the arches and each tile that blocks the way, grown 2 u so neighbours join (Blocked, `GuideTour.BlockingCells`); the guided pod's touch box (FirstTap); the booster's tile (Booster, BoosterKept); the plates Return can take back (Return's target); the board's grid (Bloom Burst's target) |
| Scrim | `UiRaster.SpotlightScrim`: `surface.scrim`'s color at `ScrimAlpha` (0.8) everywhere but the holes, each a rounded box (radius `RadiusUnits` 30 u, `CellRadiusUnits` 8 u over tiles) with a soft edge of `FeatherUnits` (12 u); rendered at `RasterShare` (a quarter) of the screen and stretched, cached by its holes (`ScrimKey`) |
| Ring | a `garden.glow` outline 6 u wide round the first hole, breathing out 3–10 u and from 0.95 to 0.5 alpha every `PulseSeconds` (1.2 s) |
| Bubble | parchment (`Kit.Paper`, radius 34 u) `BubbleShare` (0.88) of the safe width, centered across the screen, above the hole when it fits under the top bar (`TopBarUnits` 170 u below the safe top) with `TailUnits` + `GapUnits` (44 u) to the hole, else below it (under the hand), else at the safe bottom; a parchment tail (a turned square, 39 u) toward the hole's middle; padding 30 u; the icon (a booster on a cream face; the blocking variant's sticker tile) `IconUnits` (120 u) at its left; the first message key in `type.button_secondary` `ink.brown`, the next ones in `type.body` `ink.brown_soft`, `LineUnits` (58 u) a line, at most two lines each; "Tap to continue" (`demo.tap_continue`, `type.caption`) under a step that is not forced |
| Hand | a forced step's `ui.pointer` in white over an `ink.brown` copy 5 u larger, `HandUnits` (130 u) square, its fingertip on the hole's bottom edge, bobbing a tenth of its side |
| Taps | not forced: a tap anywhere goes on; forced: only the lit place's own targets work (the pod, the booster, Return's plates; Bloom Burst's whole board, the tile under the finger) |
| Fade | the step fades in over 0.25 s |

Messages: `demo.entry` "Bloomlings come in through this arch"; `demo.first_tap`; `demo.entry_blocked.1` "These tiles
are in the way" and `.2` "Clear them first to reach the others"; a booster's `demo.<booster>.1` and `demo.try_free`
"Tap it to try. This one is on us"; Return's and Bloom Burst's `.2`; `demo.booster_kept` "Your free one is still here
for later". The preview's frames 33 (the entry), 34 (the first tap), 35 (the blocked entry, Level 2), 21 (Extra
Slot's forced step, Level 3), 36 (the kept step), 37 (Return's plates, Level 6) and 38 (Bloom Burst's tiles, Level 9)
show them.
