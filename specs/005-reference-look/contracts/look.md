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
  groups at the top and bottom corners of the plank's end with one leaf bridging them. `SignDecor.Flowers`: lush clusters at the top-left and the bottom-right ends (win sign), 1.35 × the
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
- **Petals pill** (`Kit.PetalsPill`): the cream style, lotus on the left (overlapping the edge by 10%), amount in
  `InkBrown`, the green round "+" on the right.
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
  screen says so (Win, Store, Wardrobe); the cream round close button over the top-right corner, on the top card only
  (a card covered by another, as Pause under Settings, shows none).

### 3.6 Board furniture

- **Stone border** (`Kit.StoneBorder(p, gridBox, cell)`): blocks of `UiRaster.Stone` around the grid (outline 3.5% of
  the block's shorter side), thickness 0.42 cell, lengths alternating 1.0 and 0.8 cell, nearly rectangular (rounded 14%;
  the corners are square blocks 0.42 × 0.42 rounded 30%), a thin dark joint of 0.04 cell between blocks (`StoneLine` at
  alpha 0.45), seeds by position so the border never flickers. A 0.04 cell dark gap
  (`GardenLook.BoardGap` = `LawnDark.Darken(0.55)`) between the stones and the tiles, which also shows as the thin dark
  lines between tiles.
- **Garden Entry** (`Kit.StoneArch(p, EntryArch arch)`): a big half ring of 9 sandy stone blocks (outer radius 1.5
  cells, so about 3 cells wide and 1.5 tall; the ring 28% of the outer radius thick; `UiRaster.Arch`) standing on two
  straight stone piers (`BoardLayout.ArchPier` = 0.2 of the radius, beyond the ring's base away from the board;
  `EntryArch.Picture` is the picture box with them: `UiRaster.Arch` turns any extra picture depth into piers) on the
  entry's side of the board, its crown toward the board and its opening away from it, as in the reference's gameplay
  screen; the opening shows the lawn (`GardenLook.ArchOpening` = `LawnLight.Darken(0.12)`, alpha 0.8) and a fainter
  sandy flagstone path fanning out from the ground (two rings of staggered flags with `StoneLine` joints, alpha 0.6, so the
  lawn shows through), with a soft shadow under the crown and a soft `GardenShadow` ellipse (alpha 0.18) on the lawn under
  the piers' feet; the walkers stand in it. (`BaseX`, `BaseY`) is the middle of the ring's open base, where the
  Bloomlings come out.
- **Board layout** (`BoardLayout.Fit(area, width, height, entries)`, engine-free, both builds): the cells take the
  largest size that fits the grid, the border (`Rim` = 0.46 cell), 0.2 cell of lawn on the left and right, and per entry
  side a 0.14 cell strip of lawn, an arch of at least 1.2 cells with its piers, and 0.22 cell of lawn beyond them
  (`ArchFoot`, so an arch never sits on the tray's edge); the room the region has left in that direction lets the
  arches grow up to 1.5 cells. The group is centered in the board region; arches stay within the border's span, are
  centered on their entry cell where they can, and shrink so neighbors on one side never overlap. The walkers appear at
  the arch's door, 42% of its radius inside the opening (`EntryArch.Door`). Entries may be on any side, several per level.
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

- **Frames**: `Art/Heroes/Resources/HeroMotion/{family}-{idle|react}-{NN}.png` (`HeroMotion.Folder`, `FrameName`),
  slots `char.hero3d.motion.{family}` (`HeroMotion.Slot`). 24 frames a second (`Fps`; 12 until the owner's 60 fps models, research
  D22): per family 96 idle frames (a 4 s loop) and 48 reaction frames (2 s) (`FrameCount`, `Seconds`, `Has`). Both clips start on the idle's first frame
  (the seam) and the reaction ends on it, so the idle and a reaction join without a jump. Each file is an 8-bit
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
  (stone obstacles in `StoneFace` tones), entries as `StoneArch`, walkers unchanged. In detail (`BoardPainter`):
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
- Pause, Settings, Store, Daily reward, Collection, Leaderboard, Themes, Milestone: `Card` per §3.5; the Store and
  milestone use a `WoodSign` header. Pause: brown title, the cream close, Resume (primary, decorated), Restart (⟳),
  Settings (gear) and Home (`ui.back`) as cream secondaries with their glyphs. Settings: cream rows with brown labels and
  the garden toggle (on: the green set's glossy track with a white ✓ and the knob right; off: a parchment well; the knob
  a domed cream cushion like the round buttons).
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
  owner's celebrating hero of the celebrating family exists (pictures.md A7, `char.hero3d.cheer.*`; Twig,
  `CharacterArt.Celebrant`, since the owner's choice of 2026-10-03, before it the family of the variant with the most
  work), it stands alone on the pedestal instead of the group. Since the owner's delivery
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
- Settings, Petals pill per §3.3–3.4.
- Heroes (`HomeStage.ShowsHeroes`; first deferred by the owner on 2026-10-02 over the single Home picture, then
  delivered animated the same day, FR-028): over the owner's layered Home (pictures.md B1: the garden with its
  fountain layers) Home and the splash stand the four animated heroes on the painted fountain (§6.4 "The layered
  Home", §3.12), no pedestal and no drawn fountain, with the logo, Settings, the Petals pill, the side buttons, the
  plaque, Play and the pills over them. Without the owner's picture, the drawn stage
  `HomeStage.ReferenceDiorama` (kit `HomeLook.cs`, §6.4): a `StonePedestal` ring, the lotus fountain on it
  (`Kit.LotusFountain`, `ui.fountain`: a small pedestal as its basin, water, two lily pads, the lotus) and the four
  still heroes around it as in the reference (Bloom raised behind the fountain, Drop at the right back, Sprig at the
  left, Twig in front at the right). Over an owner picture without the fountain layers (a splash picture of its own,
  B6) no heroes show. Early and progressed alike, each hero wears its outfit once the Wardrobe is open. The splash
  shows the Home picture until its own (B6) exists (`OwnerPictures.Resolve`) and the same stage as Home, so it turns
  into Home without a jump. The Leafling guest (spec 004 R17) was removed by the owner on 2026-10-02.
- The milestone teaser and the rank row are parchment pills (`Kit.ParchmentPill`) with the outlined pink gift or gold
  trophy and `InkBrown` text; the Daily Challenge card is parchment with the sun on a cream disc; the avatar a cream disc.
- Backdrop: `HomeStage.Garden` warms the Home and splash colors (a clearer blue sky, sunlit horizon and hills, lush
  bushes with pink blossoms, sandy arches; 15% of the band's theme tint stays).

### 4.6 Wardrobe (Unity) and the Store's cosmetics (playtest)

- Back (`ui.back`) round button, `WoodSign` (Ivy) banner, Petals pill.
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
- The playtest's Store cosmetics (preview frame 26): the Store card's `WoodSign` (Ivy) header, `Kit.FamilyTabs`
  (`ui.tab.family`) over the lighter panel, and `Kit.OutfitCard`s (`ui.card.outfit`) six to a page (3 × 2): "Default",
  worn while the family wears nothing, then each item for sale shown on the chosen family's hero (a frame, badge or
  marker as its shape) with its cost pill on the card's bottom edge (a tap buys); the footer between cream ‹ › page
  arrows (`Kit.ArrowButton`). The Shop tab's rows are cream rows with the booster tile and its count badge, the name and
  a cost pill; the Daily Reward, Leaderboard and Collection cards carry a `WoodSign` (None) header.
- The playtest's Wardrobe (owner's review, FR-025; preview frame 27, `playtest/design/WardrobeScreen.cs`, opened from
  Home's Wardrobe button and the avatar): the §6.5 layout without the kind chips and the profile tab; the name card is
  `Kit.NameCard` (parchment whose middle rises into the name tab); the cards, three a page, are "Default" (nothing worn;
  a tap takes everything off), the owned worn items (a tap wears one, or takes it off when worn), the worn items for sale
  (cost pill; a tap buys and wears) and the ones earned later (`Kit.OutfitCard(…, locked: true)`: the picture faded to
  `GardenLook.PictureDisabledAlpha`, the `Kit.LockBadge` padlock where the check would be); each shows the family's hero
  in its outfit with the item in its kind's place. Equipping and buying go through `WardrobeService`.

## 5. Asset slots

New slots (kind `Procedural` unless noted) registered in `AssetSlots` and marked where drawn:
`mat.wood.light`, `mat.wood.dark`, `mat.stone`, `mat.parchment`, `tile.candy`, `tile.candy.sticker`,
`ui.sign.wood`, `ui.sign.flowers`, `ui.button.rim`, `ui.button.choice`, `ui.pill.cost`,
`ui.pill.speed`, `ui.badge.count` (restyled), `board.border.stone`, `board.arch`, the lawn (the `bg.theme.*` slots
restyled, §4.2; `tile.base`, `tile.ground`, `tile.entry`, `tile.layer_peek` and `tile.picture` restyled), `fx.rays`, `fx.petals` (kind `Shape`: one petal), `ui.pedestal`, `ui.tab.family`, `ui.card.outfit`,
`ui.logo.wood`, `ui.back`, `ui.fast`, `booster.extra_slot`/`shuffle`/`return`/`bloom_burst` (redrawn; the owner's
icon pictures replace them, §3.10), `tile.grass` (the picture's background cells, §4.1), `bg.win` (the win's garden,
§4.2), `ui.sign.ivy`
(kind `Shape`: the clover cluster), `ui.jam.slots` (the jam's slot row), `ui.pill.reward` (the win's and the milestone's reward pills),
`tile.icon.{id}` and `tile.gem.{id}` (the owner's detailed and simplified variant icons, §3.11; `OwnerPictures.IconSlot`,
`GemSlot`). The `mat.` prefix is the `Material` category and `board.` belongs to `BoardTile`. Owner pictures (research D16 and
`pictures.md`) keep or add their `bg.*`, `char.hero3d.*` and `brand.wordmark` slots, whose kind is their stand-in's
(`Procedural` backdrops, the `Text` wordmark, the `Generated` heroes), with the drawn or generated stand-in as fallback:
`bg.home`, `bg.splash`, `bg.wardrobe` (Unity's Wardrobe) and `bg.theme.*` (`OwnerPictures.SlotOf`); the optional
tagline is `brand.tagline` (kind `External`, not drawn yet); the optional celebrating heroes (A7) are
`char.hero3d.cheer.sprig|bloom|drop|twig` (`CharacterArt.CheerSlot`, picture `CharacterArt.Cheer(family)` =
`3d/{family}-cheer`), with the group picture standing in until they exist. The owner's 3D pictures share the
`tools/artgen` folder: `adopt` marks them `"source": "owner"` in its `manifest.json` (`tools/artgen/README.md`).
The owner's layered Home and animated heroes (FR-028, §3.12, §6.4) add `bg.home.fountain_back`, `bg.home.lotus`,
`bg.home.fountain_front`, `bg.home.shadow` and `bg.home.petals` (`HomeLayers.SlotOf`; `bg.home` stays the garden
layer) and `char.hero3d.motion.sprig|bloom|drop|twig` (`HeroMotion.Slot`), with the still heroes as their stand-in.

## 6. Reference layouts (owner's review, spec 005 FR-020 to FR-025)

Measured on the reference's phone screens (crops `g-game.png`, `g-jam.png`, `g-win.png`, `g-home.png`,
`g-ward.png` with a 5% grid). `W` and `H` are the safe area's width and height; positions are fractions of them
unless given in `W` units. The engine-free `ScreenLayout` computes every region for both builds; screens place
elements only from those regions. On screens shorter than 19.5:9 the tray rows scale down by
`k = clamp((H / W) / 2.0, 0.8, 1)` (`ScreenLayout.ReferenceAspect` = 2.0, the safe shape of a 19.5:9 phone with its
insets: `k` is 1 at 19.5:9 and 21:9, about 0.93 at 18:9 and 0.86 at 16:9) and the board takes what is left.

The functions (engine-free, `client/Assets/Bloomlings/UI/Design/ReferenceLayout.cs`, partial `ScreenLayout`; tests in
`ReferenceLayoutTests`) are `ScreenLayout.ReferenceGameplay` → `ReferenceGameplayRegions` (with `PodChip` for one
pod of the tray's grid), `ScreenLayout.JamCard` → `JamCardRegions`, `ScreenLayout.WinScreen` → `WinRegions`,
`ScreenLayout.ReferenceHome` → `ReferenceHomeRegions` and `ScreenLayout.ReferenceWardrobe` → `ReferenceWardrobeRegions`;
`ScreenLayout.ReferenceScale` is `k`. Where the measurements left a choice, the implementation fixes it as noted under
each table ("Fixed:").

### 6.1 Gameplay

| Region | Box |
|---|---|
| Top bar | top `0.012W`, height `0.13W`: Pause squircle `0.13W` at the left edge + `0.04W`; the level sign `0.42W × 0.115W` centered, with ivy over its ends; the speed pill `0.2W × 0.115W` at the right edge − `0.04W` |
| Board | between the top bar (+ `0.02W`) and the entry strip: the stone border's outer box at most `0.86W` wide, centered; the grid inside it (border 0.42 cell + gap 0.04 cell); cells as large as fit |
| Entry strip | under the board, `0.17W` tall: lawn with the arch for bottom entries (the arch `0.24W` wide, its door on the board's edge); `0.04W` when no entry is at the bottom (side and top entries keep their arches beside the board) |
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
- **Board**: `ReferenceGameplayRegions.FitBoard(width, height, entries)` runs `BoardLayout.Fit` over `BoardArea` (the
  board's top to the entry strip's bottom, the safe width less `0.02W` a side). It narrows the fit until the stone
  border's outer box is at most `0.86W` (`MaxBoardShare`), so a bottom arch stands in the entry strip.
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
| Hero | Twig (`CharacterArt.Celebrant`: its animated hero, else its celebrating picture, else the group), centered, from 50% to 76% of H, overlapping the picture's foot |
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
of the celebrating picture, played by a `HeroMotionPlayer(family, idleOrigin: t0)` with `React(t0, waitForSeam: true)`,
t0 the moment the hero appears (after the entrance delay the win already has: in the playtest 0.1 s after the card
shows, as it starts rising in; in Unity when the celebration shows). The reaction (2 s) therefore plays first, from the
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
| Settings | cream round `0.13W`, left `0.04W`, top 2.5% of H |
| Petals pill | `0.38W × 0.095W`, right edge − `0.02W`, top 2.5% of H |
| Logo | `0.8W` wide centered, from 10% to 20.5% of H; the owner's logo picture (C1, with transparent margins) is sized by width, `0.82W` (`ReferenceHomeRegions.LogoPicture`), so its letters span about `0.8W` and fill 10%–20.5% |
| Diorama | from 22% to 70% of H: the owner's layered Home over the whole screen with the four animated heroes on its fountain (below, "The layered Home"); else the drawn garden with the still heroes on a pedestal with the lotus fountain, centered at 50% |
| Side buttons | Wardrobe, Collection (left) and Daily Challenge, Store (right) as cream round buttons `0.13W` stacked from 24% of H at `0.04W` from the edges; the rank as a small parchment pill (its place: see "Fixed") |
| Level plaque | wooden sign `0.5W × 0.085H`, centered, from 64% to 72.5% of H |
| Play | the primary button (wood rim, decorated, breathing), `0.85W` wide, from 73.5% to 88.5% of H; the label "Play" alone (no arrow), half the button's height (`ReferenceHomeRegions.PlayLabelShare`) |
| Teaser | the milestone teaser as a small parchment pill centered under Play (89.5%–93.5%); the free booster as a cream pill beside it when offered |

Fixed: the fractions apply to the safe height less `bottomReserve` (0 in both builds: the playtest's dev row lies small
and faded at 70% alpha over the garden in the band under the teaser, so the layout keeps the reference's fractions); the
Petals pill (without the "+" while the Store is locked, its amount follows the lotus) is centered
on the Settings button's height; the logo starts at 10% of H or `0.01W` under Settings, whichever is lower; the side
columns start at 24% of H or `0.02W` under the logo and stack `0.13W` buttons `0.03W` apart (`SideButton(right, i)` for
more, such as the avatar); the rank pill (`0.3W × 0.075W`) lies in the top row, centered between Settings and the
Petals pill on Settings' middle (`ReferenceHomeRegions.Rank`; under the right column, its first place, it covered Drop's
head on the layered Home); the teaser row (`0.04H`, the teaser `0.5W`, the free booster
from `0.02W` right of it to `0.02W` from the edge) moves down when the free booster's touch box would reach Play.

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
8. the UI: the logo, Settings, the Petals pill, the side buttons, the plaque, Play and the pills.

Every layer lies at `HomeLayers.Place(HomeLayers.Cover(screen), layer)`, `screen` the full-screen box the backdrop
cover-fits the garden into: `Cover` lays the 852 × 1846 picture (`PictureWidth`, `PictureHeight`) over it at the larger
scale, centered, and `Place` scales a layer's box in the picture's pixels into it, so the heroes stay on the fountain
on every screen shape.

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
never takes a tap from Play, the side buttons, Settings, the Petals pill or the plaque: the playtest cuts each hero's
touch box clear of every Home control and of the heroes in front of it (a part smaller than `size.touch_min` takes
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
right (`0.48u` at `+0.37u`), so Bloom's eyes stay clear of Drop. The playtest stacks only the
unlocked side buttons (left: Wardrobe, Collection, the avatar; right: Daily Challenge, Store), the rank pill in the top row
(`r.Rank`); its splash shows the logo and the diorama in the same boxes.

Unity (`HomeScreen`, `SplashScreen`): each side column packs the buttons it shows from its top with
`SideButton(right, i)` (left: Wardrobe `ui.shirt`, Collection `ui.grid`, the profile avatar; right: the Daily Challenge,
the sun `ui.sun` with the green check badge when done today, and the Store, the lotus), and the rank pill in the top row
(`r.Rank`); the logo shows in both looks, the owner's logo picture sized by width
(`ReferenceHomeRegions.LogoPicture`: `0.82W` wide, centered on the logo box, its top no higher than a tenth of its
height above Settings' bottom); over the owner's layered Home both looks show its stage with the four animated heroes
(`HeroPictures.Stage`, `HomeLayersView` with one `HeroMotionView` per hero, built under the screen's controls;
`HeroPictures.StageOf` picks the stage); without the picture, the drawn `HomeStage.ReferenceDiorama`, where once the
Wardrobe is open each hero wears its outfit and the player's hero (`ProfileAvatar.HeroFamily`) swaps places with Sprig
at the left front; Play shows its label alone, `ReferenceHomeRegions.PlayLabelShare` of its height; the Petals pill
without its "+" starts the amount right after the lotus; the plaque is `0.5W`, wider when its letters need it (at most
`0.8W`); the free booster is the cream `CostPill` "Free"; the rank pill and the free booster take taps in clear boxes
grown to `size.touch_min`; the splash takes the Home garden while its own picture is missing (`OwnerPictures.Resolve`)
and puts its logo and its heroes where Home shows them; over the layered Home the splash and Home share one `HomeMotion`
while both show, so Home takes over the splash's motion without a jump.

### 6.5 Wardrobe (both builds)

| Element | Box |
|---|---|
| Back | cream round `0.12W`, left `0.04W`, top 2.5% of H |
| Banner | wooden sign with ivy `0.46W × 0.055H`, from 24% to 70% of W, top 4.5% |
| Petals pill | `0.28W` (from 70% of W), right edge − `0.02W`, top 3.5%, clear of the banner's right ivy |
| Hero | the selected family's hero in the worn outfit, from 11% to 37% of H, on a pedestal `0.6W` wide (35%–43%) |
| Arrows | cream round ‹ › `0.09W` at 8% and 92% of W, 28% of H (previous/next family) |
| Name card | parchment from 42% to 57% of H, `0.92W`; a sign-like tab `0.5W` with the name (`type.title`), the role line, the description in two lines |
| Family tabs | from 56% to 68.5% of H, four tabs `0.24W` with the family's hero head and name; the selected one lighter and joined to the panel below |
| Outfit panel | parchment from 67% to the bottom: three cards per row `0.29W × 0.18H` with the hero wearing the item and its name; the worn one green with a check badge; pages or scroll for more |
| Footer | "Earn special outfits as you play!" at 93% of H |

Fixed: the Petals pill is `0.08W` tall; the hero box is an 8:9 box from 11% to 37% of H; the name tab spans 42%–47.5%,
the role line 47.5%–50.5%, the description 50.5%–56%; `Tab(i, n)` splits the tabs' `0.96W` into n tabs at most `0.24W`
wide with `0.01W` between them (five with the Unity profile tab); the panel spans `0.96W` to the screen's bottom; without
chips the cards span 70%–88% of H, with the kind chips (`hasChips`, 69.5%–74%) 75%–89.5%; `Card(i)` is `0.29W` wide,
spread over `0.92W`; the footer box spans 91%–95% of H between the page arrows (`0.09W` at 8% and 92% of W).
