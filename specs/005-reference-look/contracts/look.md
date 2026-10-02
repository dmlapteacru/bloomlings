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
- States: `Dimmed` (queued pods) mixes face and symbol 45% toward `ParchmentBottom`; `Grey` (stuck) uses
  `color.Grey()`; `Pressed` sinks the face into the lip (`Kit.Block` press); `Mystery` uses `TileMystery` with a white
  "?" (`tile.mystery`) and no symbol.

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

- **Pod** (`Kit.PodFrame(p, box, state)` + `CandyTile` sticker + count): `UiRaster.Frame` (Dark) filling the pod box,
  radius 18%, border 11% of the width; inside it a panel tinted by the variant (`color.Mix(CreamTop, 0.78)` →
  `CreamFace`; queued, locked and mystery pods keep the plain `CreamTop` → `CreamFace`); the candy tile (sticker) at
  62% of the inner width, 3% below its top; the count below it, `type.count` `InkBrown` scaled to 1.05 of the room left
  (its digits about 17% of the pod tall, as the reference's), no "x". Pressed: the pod sinks (its shadow 1% of the width
  below it), darkens (`GardenShadow` at 0.08) and its sticker sinks into its lip. A short wooden handle (a dark
  `UiRaster.Plank`, 34% × 15% of the box's width, on a small `WoodDarkLine` stem) on top of the exposed pod. Queued
  pods (the ones below in a column) draw the same frame at 55% brightness mixed toward `ParchmentBottom` and a dimmed
  tile. Locked: the frame with `StateLockBg` inner and the lock glyph; mystery: the lilac mystery tile; connected: the
  link bar between frames as now, one color per connected group of the tray (`state.link`, then `state.link_2` and
  `state.link_3`). The "+N" depth badge stays (count badge style, §3.4).
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
- Since the owner's review the rows sit in the reference layout of §6.1 (the slots, the four booster boxes and one deck
  per Source stack on one parchment tray). Unity twin: `GameplayHud.Layout(hasBadge, hasBoosters, entrySides,
  stackCount)` places every region from `ScreenLayout.ReferenceGameplay` and gives the views their boxes in their own
  canvas units (`SlotCells`, `DeckCells`, `BoosterCells`, and `FitBoard` for `BoardView.Fit`, all wired by
  `GameplayController`); `UiKit.TrayPanel` (the frame and its bands), `UiKit.DeckPod` (the front pod, `PodView`) and
  `UiKit.BuriedPod` (`pod.deck`) in `UiKitDeck.cs` draw the playtest's recipe of §6.1 "Drawn"; `TrayView` lays out
  the decks, their "+N" badges, the wells and the links. A connected group whose shown members lie at different depths
  (connected pods do not always share one), on two rows of decks, or that shows a single member marks each shown member
  with a ring of its link color (0.07 of the deck's width, a white rim 30% wider, at 0.11 of the width from the front
  pod's top-left corner or in the middle of a buried pod's band). Flights start at the deck's tile
  (`TrayView.TilePosition`, `TileSize`) and land on the slot's tile (`SlotRowView.TilePosition`, `TileSize`).

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
  owner's celebrating hero of the level's main family exists (pictures.md A7, `char.hero3d.cheer.*`; the family of the
  variant with the most work), it stands alone on the pedestal instead of the group. A light sprinkle of confetti falls
  for 2.2 s only above the card, so the picture, the reward and Next stay clean. Pause stays visible and usable over the
  win card in both builds (FR-002: the card changes no tap outcome, so Home, Restart and Settings stay reachable from
  it, as before spec 005). The celebration (rays, petals, Next breathing) animates for about
  8 s after the card shows, then rests on its last frame until the next input, so an idle win card costs no frames.
- The reward as a cream pill (`CostPill` style, `size.reward_pill_height` = 104 units tall, `ui.pill.reward`) "+N"
  with the lotus, counting up, a sparkle at the lotus and petals bursting out; a dropped booster charge below it as its
  icon and "+1 Name".
- Next: `PrimaryButton` (wood rim, decorated, breathing). ×2: cream secondary with the ad glyph.
- Milestone (frame 16): the same sign ("Level N"), heroes, rays and petals; "Milestone reached!" in `InkBrownSoft`;
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
- Heroes, deferred by the owner on 2026-10-02 (placing them around the painted fountain is hard; they come back
  animated in a later task): over the owner's Home picture (pictures.md B1) Home and the splash show no heroes, no
  pedestal and no drawn fountain, only the picture with the logo, Settings, the Petals pill, the side buttons, the
  plaque, Play and the pills (`HomeStage.ShowsHeroes`). Without the picture, the drawn stage
  `HomeStage.ReferenceDiorama` (kit `HomeLook.cs`, §6.4): a `StonePedestal` ring, the lotus fountain on it
  (`Kit.LotusFountain`, `ui.fountain`: a small pedestal as its basin, water, two lily pads, the lotus) and the four
  heroes around it as in the reference (Bloom raised behind the fountain, Drop at the right back, Sprig at the left,
  Twig in front at the right), early and progressed alike, each in its outfit once the Wardrobe is open. The splash
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

## 6. Reference layouts (owner's review, spec 005 FR-020 to FR-025)

Measured on the reference's phone screens (crops `g-game.png`, `g-jam.png`, `g-win.png`, `g-home.png`,
`g-ward.png` with a 5% grid). `W` and `H` are the safe area's width and height; positions are fractions of them
unless given in `W` units. The engine-free `ScreenLayout` computes every region for both builds; screens place
elements only from those regions. On screens shorter than 19.5:9 the tray rows scale down by
`k = clamp((H / W) / 2.0, 0.8, 1)` (`ScreenLayout.ReferenceAspect` = 2.0, the safe shape of a 19.5:9 phone with its
insets: `k` is 1 at 19.5:9 and 21:9, about 0.93 at 18:9 and 0.86 at 16:9) and the board takes what is left.

The functions (engine-free, `client/Assets/Bloomlings/UI/Design/ReferenceLayout.cs`, partial `ScreenLayout`; tests in
`ReferenceLayoutTests`) are `ScreenLayout.ReferenceGameplay` → `ReferenceGameplayRegions` (with `PodDeck` for one
deck), `ScreenLayout.JamCard` → `JamCardRegions`, `ScreenLayout.WinScreen` → `WinRegions`, `ScreenLayout.ReferenceHome`
→ `ReferenceHomeRegions` and `ScreenLayout.ReferenceWardrobe` → `ReferenceWardrobeRegions`; `ScreenLayout.ReferenceScale`
is `k`. Where the measurements left a choice, the implementation fixes it as noted under each table ("Fixed:").

### 6.1 Gameplay

| Region | Box |
|---|---|
| Top bar | top `0.012W`, height `0.13W`: Pause squircle `0.13W` at the left edge + `0.04W`; the level sign `0.42W × 0.115W` centered, with ivy over its ends; the speed pill `0.2W × 0.115W` at the right edge − `0.04W` |
| Board | between the top bar (+ `0.02W`) and the entry strip: the stone border's outer box at most `0.86W` wide, centered; the grid inside it (border 0.42 cell + gap 0.04 cell); cells as large as fit |
| Entry strip | under the board, `0.17W` tall: lawn with the arch for bottom entries (the arch `0.24W` wide, its door on the board's edge); `0.04W` when no entry is at the bottom (side and top entries keep their arches beside the board) |
| Tray | from the entry strip to the bottom of the screen (under the bottom inset too), full width, parchment with rounded top corners (radius `0.06W`) and a soft top shadow; inner padding `0.035W` at the sides, `0.025W` at the top, the bottom inset + `0.02W` at the bottom |
| Slots row | `0.19W·k` tall: five plates `0.165W` wide each (portrait, height = row), spread evenly across `0.92W`; the extra slot (sixth) narrows them to fit |
| Separator | a thin `ParchmentEdge` line with a light line under it, `0.02W·k` gap above and below |
| Booster row | `0.23W·k` tall: four cream squircle boxes `0.195W` square, spread evenly across `0.9W` (centers at 14.5%, 37.5%, 61.5%, 84.5% of W), the green badge on each box's bottom-right |
| Separator | as above |
| Pods row | `0.31W·k` tall: one deck per Source stack, up to 4 at `0.23W` wide spread evenly across `0.96W`; 5 or more shrink to fit one row (never below `0.17W`, then a second row) |

Fixed: the top bar's Pause box is `0.13W` square at `0.04W`, the sign and the speed pill are centered on the bar; a
Hard or Super Hard badge (`hasBadge`) is a `0.36W × 0.052W` box under the bar and pushes the board down by its height;
the entry strip shrinks by `k` too; without boosters (`hasBoosters: false`) the booster row and its line collapse; the
tray box spans the whole screen width and runs to the screen's bottom (`TrayRadius` = `0.06W`), its content box
(`TrayContent`) stops `0.02W` above the bottom inset; the separators are lines `0.92W` wide and `max(2 px, 0.005W)`
thick in the middle of their `0.04W·k` gaps; the slots spread their `0.92W` with at least `0.0238W` between them, the
boosters their `0.9W` (the measured centers 14.5%–84.5% become the symmetric 14.75%, 38.25%, 61.75%, 85.25%); the
decks are at most `0.23W·k` wide with gaps from `0.0133W` to at most `0.02W`, centered (2 or 3 stacks sit together in
the middle, as the reference's touching decks); each deck's front pod fills its bottom 82% (`PodDeck.FrontShare`, about
`0.23W × 0.254W`), the two buried pods' bands above it; a Waiting Slot's sticker tile is
`min(0.74 face width, 0.66 face height)` 8% of the face under its top (`ReferenceGameplayRegions.SlotTile`, about
`0.12W`), the count under it; more stacks than fit at `0.17W` wrap into two rows of `0.26W·k` with `0.015W` between them.
`ReferenceGameplayRegions.FitBoard(width, height, entries)` runs `BoardLayout.Fit` over `BoardArea` (the board's top to
the entry strip's bottom, the safe width less `0.02W` a side), narrowed until the stone border's outer box is at most
`0.86W` (`MaxBoardShare`), so a bottom arch stands in the entry strip. `BoosterBadge(i)` is the badge disc as
`Kit.BoosterTile` draws it (0.34 of the box, its center 0.55 of it inside the bottom-right corner).

**Pod deck** (§3.7, FR-021): the deck box is the pod row cell. The front pod fills its bottom 78% (frame, cream panel,
the sticker tile at 62% of the frame's width in the upper part, the count below it in `type.count` ×1.3, dark
`InkBrown`). Up to two buried pods are frames of the same width stacked behind it, each raised by 9% of the deck height
over the one in front; the visible band of each shows the frame's top edge and a strip of its variant color with the
variant's small sticker symbol (at most 70% of the band's height) in its middle. A "+N" count badge on the deck's
top-right counts the pods beyond the two shown. Empty stacks show a sunk parchment well. Fixed (`PodDeck.In(deck)`):
`Front` is the bottom 78%; `Buried1`/`Buried2` are the front frame raised by 9% and 18% of the deck's height, and
`Band(depth)` the strip of each that shows; `Inner` is the front frame inset by its 11% border; `Tile` the sticker tile
(62% of the deck's width, at most 78% of the panel's height, 3% of the panel below its top); `Count` the panel under it;
`Badge` a disc 0.26 of the width centered 0.32 of it inside the deck's top-right corner.

**Drawn** (the playtest's `LevelScreen`, `PodPainter`, `SlotPainter`, `BoosterBarPainter`; the recipe the Unity twin
follows): the tray is the old banded parchment on the new regions, as the reference's tray: a frame of deep parchment
(`parchment.edge` mixed 55% toward `parchment.line`, a dark outline, a soft shadow rising onto the lawn) showing
`0.012W` round one band per row (`parchment.well` mixed 55% toward `wood.light` at the top to `parchment.edge` mixed 40%
toward `parchment.well` at the bottom, its edges aged with `parchment.line`, a light bevel along its top, a thin outline;
the first band's top corners `TrayRadius` less the margin, the others `0.03W`), the bands parted by a `0.009W` groove at
each separator's middle (`LevelScreen.TrayPanel`).
A deck's front pod is `Kit.PodFrame` without the handle, its panel the variant's color lightened 0.5 at the top and 0.8
at the bottom (the reference's lime, pink, sky blue and orange panels), the sticker tile in `PodDeck.Tile` and the
count in `PodDeck.Count` at 1.2 × its room. A buried pod (`pod.deck`) is the same frame, of which its band shows the
frame's top edge (30% of the band) and a strip of its variant color lightened 0.4 → 0.12 with a dark outline, and on
it the variant's symbol as a dark silhouette (the color darkened 0.52, a light halo; its shape about 70% of the strip);
a hidden mystery pod shows a lilac strip with a white "?", a locked one a grey strip with the padlock; each buried pod
sits in a faint shade (`garden.shadow` 0.06 per depth). An emptied stack is a `Kit.Well` over `PodDeck.Front`. Only the
front pod takes a tap; a committed pod's tile flies from `PodDeck.Tile` to the slot's tile (`SlotPainter.TileBox`).
Connected pods are linked front to front (or band to band when buried, as connected pods share a depth); a group
split over two rows of decks marks each member with a ring of its link color.

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
| Hero | the celebrating hero of the level's main family (or the group), centered, from 50% to 76% of H, overlapping the picture's foot |
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
| Diorama | from 22% to 70% of H: the owner's Home picture behind everything, without heroes for now (`HomeStage.ShowsHeroes`); else the drawn garden with the heroes on a pedestal with the lotus fountain, centered at 50% |
| Side buttons | Wardrobe, Collection (left) and Daily Challenge, Store (right) as cream round buttons `0.13W` stacked from 24% of H at `0.04W` from the edges; the rank as a small parchment pill under the Petals pill |
| Level plaque | wooden sign `0.5W × 0.085H`, centered, from 64% to 72.5% of H |
| Play | the primary button (wood rim, decorated, breathing), `0.85W` wide, from 73.5% to 88.5% of H; the label "Play" alone (no arrow), half the button's height (`ReferenceHomeRegions.PlayLabelShare`) |
| Teaser | the milestone teaser as a small parchment pill centered under Play (89.5%–93.5%); the free booster as a cream pill beside it when offered |

Fixed: the fractions apply to the safe height less `bottomReserve` (0 in both builds: the playtest's dev row lies small
and faded at 70% alpha over the garden in the band under the teaser, so the layout keeps the reference's fractions); the
Petals pill (without the "+" while the Store is locked, its amount follows the lotus) is centered
on the Settings button's height; the logo starts at 10% of H or `0.01W` under Settings, whichever is lower; the side
columns start at 24% of H or `0.02W` under the logo and stack `0.13W` buttons `0.03W` apart (`SideButton(right, i)` for
more, such as the avatar); the rank pill (`0.3W × 0.075W`) sits under the right column, right-aligned at `0.04W` (under
the Petals pill there is no room for its touch target); the teaser row (`0.04H`, the teaser `0.5W`, the free booster
from `0.02W` right of it to `0.02W` from the edge) moves down when the free booster's touch box would reach Play.
Over the owner's Home picture there are no heroes for now (`HomeStage.ShowsHeroes`): the owner deferred them on
2026-10-02, and they come back animated in a later task. For that task, the placement measured on the reference (its
code, `HomeStage.AroundFountain`, is removed): the picture cover-fitted over the screen (the larger scale, centered),
the anchor F its lotus's middle (50%, 45.5% of the picture) and the unit L the lotus's width (0.30 of the picture's
width, 324 px at 1080 wide); back to front (`HomeStage.Figure(x, feet, height)`): Bloom `(F.x, F.y − 0.27L, 1.47L)`
behind the lotus, Drop `(F.x + 0.70L, F.y + 0.55L, 1.30L)` at the right back, Sprig `(F.x − 0.97L, F.y + 0.86L, 1.81L)`
at the left front and Twig `(F.x + 1.13L, F.y + 1.05L, 1.40L)` at the right front; the player's hero swapped into
Sprig's place; when Bloom's head (12% down its box) would rise above the logo's letters all four shrank about F until
it cleared.
The drawn diorama (without the owner's picture) is `HomeStage.ReferenceDiorama(stage)` (in `u = min(0.88 × stage
width, stage height / 1.09)`, retuned for the owner's larger heroes): the well's stone ring `0.78u` wide with its foot
`0.09u` above the stage's bottom, the lotus fountain on it, Bloom raised behind the fountain (`0.64u` picture, feet
`0.51u` up), Drop at the right back (`0.44u` at `+0.30u`), Sprig at the left (`0.74u` at `−0.26u`), Twig in front at the
right (`0.48u` at `+0.37u`), so Bloom's eyes stay clear of Drop. The playtest stacks only the
unlocked side buttons (left: Wardrobe, Collection, the avatar; right: Daily Challenge, Store), the rank pill following
the right column; its splash shows the logo and the diorama in the same boxes.

Unity (`HomeScreen`, `SplashScreen`): each side column packs the buttons it shows from its top with `SideButton(right, i)`
(left: Wardrobe `ui.shirt`, Collection `ui.grid`, the profile avatar; right: the Daily Challenge, the sun `ui.sun` with
the green check badge when done today, and the Store, the lotus), and the rank pill sits under the right column's last
button; the logo shows in both looks, the owner's logo picture sized by width (`ReferenceHomeRegions.LogoPicture`:
`0.82W` wide, centered on the logo box, its top no higher than a tenth of its height above Settings' bottom); over the
owner's Home picture both looks show no heroes for now (`HomeStage.ShowsHeroes`); without the picture, the drawn
`HomeStage.ReferenceDiorama`, where once the Wardrobe is open each hero wears its outfit and the player's hero
(`ProfileAvatar.HeroFamily`) swaps places with Sprig at the left front; Play shows its label alone, `ReferenceHomeRegions.PlayLabelShare` of its
height; the Petals pill without its "+" starts the amount right after the lotus; the plaque is `0.5W`, wider when its
letters need it (at most `0.8W`); the free booster is the cream `CostPill` "Free"; the rank pill and the free booster
take taps in clear boxes grown to `size.touch_min`; the splash takes the Home garden while its own picture is missing
(`OwnerPictures.Resolve`) and puts its logo (and, on the drawn stand-in, its heroes) where Home shows them.

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
