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
| `LawnLight` | `lawn.light` | `#A2C447` | gameplay lawn (sunny yellow-green) |
| `LawnDark` | `lawn.dark` | `#6E9530` | lawn shade, grass strokes |
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
| `Lawn(w, h, int seed)` | Optional: the lawn texture for the gameplay backdrop if `BackdropRaster` does not take it (§4.2). |

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
| Water | `#5FB6FF` → `#1E6FD6` | a white curved highlight on the left |
| Dew | `#E6FFFF` → `#9EEFF3` | a white sparkle star at the top right |
| Wood | `#C47A3C` → `#7A3A12` | two concentric darker rings on the stump top |
| Acorn | `#E39A4A` → `#A35A18` (nut), cap `#7A4A22` | a cap line and a tiny stem |
| Expansion | `color.Lighten(0.2)` → `color.Darken(0.15)` | none |

The symbols themselves are redrawn in `ShapeLibrary` (research D13) so that each reads as in the reference strip while
the two variants of a family keep different silhouettes: leaf (a broad almond, about 1.5:1, with a barely visible
stem), moss (a round 1:1 cushion with about eleven soft scallops), flower (five clearly separated round petals with
deep notches), bud (a chunky rounded bud with three short tips and two sepals, no stem), water (pointed teardrop), dew
(round droplet, its sparkle as a separate part), wood (stump: a short cylinder with a ringed top), acorn (cap and nut).
The mask difference between any two symbols at 48 px stays above 0.08 (`ShapeLibraryTests`).

### 3.2 Wooden sign — `Kit.WoodSign(p, box, text, TypeStyle style, SignDecor decor)`

Reference crops: the gameplay top bar, "Level Complete!", the Wardrobe banner, the Home "LEVEL 88" plaque.

- `UiRaster.Plank` (Light: pale honey wood) filling `box`, radius 28% of the height, outline 2.5% of the height (min
  2 px), and a soft shadow below (`Kit.SoftShadow`, `GardenShadow` alpha 0.22, offset 7% of the height).
- Letters: `InkBrown` with a light emboss (`TextLook.Plain`-like, emboss `WoodLight.Lighten(0.4)`), centered, max width
  82% of the plank; the win and milestone sign (`SignDecor.Flowers`) uses `InkTitle`.
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
- **Pedestal** (`Kit.StonePedestal(p, box)`, `UiRaster.Pedestal`): an ellipse-topped stone drum: top ellipse `StoneTop`
  with a ring joint, side `StoneFace` in two courses of staggered blocks, `StoneLip` bottom, moss at the base. It
  returns the top ellipse's box (where the heroes stand). Win, Home (until the owner's diorama) and Wardrobe.

### 3.7 Tray pieces

- **Pod** (`Kit.PodFrame(p, box, state)` + `CandyTile` sticker + count): `UiRaster.Frame` (Dark) filling the pod box,
  radius 18%, border 11% of the width; inside it a panel tinted by the variant (`color.Mix(CreamTop, 0.78)` →
  `CreamFace`; queued, locked and mystery pods keep the plain `CreamTop` → `CreamFace`); the candy tile (sticker) at
  62% of the inner width, 3% below its top; the count below it, `type.count` `InkBrown` scaled to 1.05 of the room left
  (its digits about 17% of the pod tall, as the reference's), no "x". Pressed: the pod sinks (its shadow 1% of the width
  below it), darkens (`GardenShadow` at 0.08) and its sticker sinks into its lip. A short wooden handle
  (`WoodDark`, 22% × 7% of the box) on top of the exposed pod. Queued pods (the ones below in a column) draw the same
  frame at 55% brightness mixed toward `ParchmentBottom` and a dimmed tile. Locked: the frame with `StateLockBg` inner
  and the lock glyph; mystery: the lilac mystery tile; connected: the link bar between frames as now. The "+N" depth
  badge stays (count badge style, §3.4).
- **Waiting Slot** (`Kit.SlotPlate`): a cream plate (raised: `CreamTop`→`CreamFace`, `CreamLip`, `CreamLine`, radius
  20%); filled: the sticker tile at 70% of the width near the top and the count below it; empty: a dashed rounded inner
  outline (`CreamLine` alpha 0.8, dash 9%/6% of the side) on a slightly sunk face; stuck: grey tile + the hourglass
  badge; danger: the dashed outline in `StateDanger` with "!"; extra slot: the green "+" badge; locked: grey face with
  the lock.
- **Booster tile** (`Kit.BoosterTile`): a cream squircle (radius 26%) in a cream-white bezel with a faint silver tint
  (`GardenLook.BoosterRim` = `CreamTop.Mix(StateStuck, 0.25)`), a cream lip (`GardenLook.BoosterLip` =
  `CreamLip.Mix(StateStuck, 0.3)`) and a soft tan outline (`GardenLook.BoosterLine` = `CreamLine.Mix(StateStuck,
  0.35).Darken(0.1)`), never a grey keycap; the booster icon (§3.8) at 62%, the count badge or the cost pill; selected:
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

## 4. Screens

Positions and order stay as in spec 002; only the looks change.

### 4.1 Gameplay (frames 7–9, 12–14, 21–23)

- Top bar: Pause squircle, level `WoodSign` (Ivy) instead of the level pill, speed pill.
- Board: lawn backdrop, `StoneBorder`, candy tiles (board style), restored ground as pale flat cells
  (`PictureColor` lightened 0.55, radius 10%, no bevel, a faint inner shadow), stones/keys/locks/layers/specials as now
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
  thick when the cells are at least 16 units; ground as cream cells, stones as stone blocks.
- Slot row: on a parchment band, `SlotPlate`s.
- Tray: parchment panel behind the columns; pods per §3.7.
- Booster bar: on the parchment, `BoosterTile`s.

### 4.2 Backdrop

`BackdropScene.Gameplay` becomes a sunny lawn: `LawnLight` → `LawnDark` fbm grass with fine darker strokes, scattered
small flowers (pink, white, orange `ButtonOrange`, yellow five-dot blossoms), leafy clumps and bushes along the screen
edges in greens only a little deeper than the grass (shade `LawnDark` × 0.8, bush deep × 0.72, leaf deep × 0.78), a light
vignette (0.12); the edges stay about as light as the middle, a sunlit garden, never a dark frame; no sky.
Home and Splash keep the sky, arches and hills but warmer (until the owner's pictures); the distant arches are signed
distances blended over 1.5 backdrop pixels, so their round doorways stay smooth at a fifth of the resolution, the
hedges carry a leafy texture, and their blossoms (fewer, of varied sizes, pink and white with yellow middles) gather
toward the hedges.

The lawn (`BackdropRaster`) draws, back to front: soft patches of sun and shade with a finer mottle; one short tapered
blade per 0.011-width cell, dark or light; soft bushes right at the edges (bumpy, lit from the upper left, a leafy
speckle, a soft shadow); almond leaves fanned inward from the nearest edge (a few in spring green); five-petal flowers,
pink most often, then white, orange, the theme's own and yellow, many more of them near the edges; the vignette. The theme still
shows (`DesignTokens.Backdrop`, frame 18): its accent's hue tilts the grass (Pond teal, Orchard warm, Moonlit blue-green)
and colors a fourth flower, and the Moonlit Garden's dimmer background darkens the lawn toward dusk; the Daylight Garden
keeps `lawn.light` and `lawn.dark`. Hosts render it at a third of the screen's resolution
(`BackdropRaster.Downscale(scene)`; Home and Splash stay at a fifth). The slots stay `bg.theme.*` (the owner's pictures
B2–B5 replace the lawn).

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

- `WoodSign` (Flowers) "Level complete!" (`type.level_home`, 146 units tall, at most 80% of the card wide) across the
  card's top edge, its center 30 units below it.
- The finished picture in full color: each picture cell as a flat candy tile (no lip, small gloss, board-style symbol
  of its role's variant) inside a `StoneBorder` (thin, 0.3 cell); 520 units tall (24% of the safe height on a shorter
  phone, at least 400), with the light sweep once. A milestone level adds a cream pill with the gold medal and
  "Milestone reached!" over the picture's top edge.
- The heroes group on a `StonePedestal` above the sign, in the room up to the safe area's top (left out under 150
  units), laid out by `HomeStage.Celebration` (the pedestal 80% of the group's width at the stage's bottom, the group
  picture standing on its top with the heroes' feet, `CharacterArt.GroupFeetShare`, a tenth of the top's half height
  below its middle, as large as the room from the heads to the pedestal's foot allows), with light rays behind (clipped
  above the card, fading in, alpha 0.85) and falling petals around. The group picture has no base of its own. When the
  owner's celebrating hero of the level's main family exists (pictures.md A7, `char.hero3d.cheer.*`; the family of the
  variant with the most work), it stands alone on the pedestal instead of the group. A light sprinkle of confetti falls
  for 2.2 s only above the card, so the picture, the reward and Next stay clean. The top bar fades out in 0.3 s and takes
  no taps while the card shows. The card keeps the screen redrawing for as long as it is open (rays, petals, Next
  breathing; a host may drop to about 30 frames a second once only that motion is left).
- The reward as a cream pill (`CostPill` style, 104 units tall, `ui.pill.reward`) "+N" with the lotus, counting up, a
  sparkle at the lotus and petals bursting out; a dropped booster charge below it as its icon and "+1 Name".
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
- Heroes, until the owner's Home picture (pictures.md B1): the drawn stage `HomeStage.Diorama` (kit `HomeLook.cs`): a
  `StonePedestal` 0.84 of the stage wide, the lotus fountain on it (`Kit.LotusFountain`, `ui.fountain`: a small pedestal
  as its basin, water, two lily pads, the lotus) and the four heroes in an arc as in the reference (Sprig at the left,
  Bloom raised behind the fountain, Drop, Twig in front at the right edge), the guest (spec 004 R17) at the left front.
  Over the owner's picture the group picture stands in front with no drawn pedestal. Progressed: the player's hero on a
  `StonePedestal` (`HomeStage.HeroOnPedestal`), the guest at its right. The splash shows the same stage without the guest.
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
- Outfit cards: cream cards with a beige picture well, the name below; the worn one has a green face tint, a 4 px green
  (`GardenLook.Green.Face`) border and a green check badge. A hat sits on the hero's head in full color
  (`CharacterArt.HatOnHero`: 55% of the picture wide over `HeadTopHero`, its brim overlapping the head by 15%), its own
  tint with a `tint.Darken(0.45)` outline and a light top-left side.
- Footer: "Earn special outfits as you play!" (`InkBrownSoft`).
- The playtest's Store cosmetics (preview frame 26): the Store card's `WoodSign` (Ivy) header, `Kit.FamilyTabs`
  (`ui.tab.family`) over the lighter panel, and `Kit.OutfitCard`s (`ui.card.outfit`) six to a page (3 × 2): "Default",
  worn while the family wears nothing, then each item for sale shown on the chosen family's hero (a frame, badge or
  marker as its shape) with its cost pill on the card's bottom edge (a tap buys); the footer between cream ‹ › page
  arrows (`Kit.ArrowButton`). The Shop tab's rows are cream rows with the booster tile and its count badge, the name and
  a cost pill; the Daily Reward, Leaderboard and Collection cards carry a `WoodSign` (None) header.

## 5. Asset slots

New slots (kind `Procedural` unless noted) registered in `AssetSlots` and marked where drawn:
`mat.wood.light`, `mat.wood.dark`, `mat.stone`, `mat.parchment`, `tile.candy`, `tile.candy.sticker`,
`ui.sign.wood`, `ui.sign.ivy`, `ui.sign.flowers`, `ui.button.rim`, `ui.button.choice`, `ui.pill.cost`,
`ui.pill.speed`, `ui.badge.count` (restyled), `board.border.stone`, `board.arch`, the lawn (the `bg.theme.*` slots
restyled, §4.2; `tile.base`, `tile.ground`, `tile.entry`, `tile.layer_peek` and `tile.picture` restyled), `fx.rays`, `fx.petals` (kind `Shape`: one petal), `ui.pedestal`, `ui.tab.family`, `ui.card.outfit`,
`ui.logo.wood`, `ui.back`, `ui.fast`, `booster.extra_slot`/`shuffle`/`return`/`bloom_burst` (redrawn), `ui.sign.ivy`
(kind `Shape`: the clover cluster), `ui.jam.slots` (the jam's slot row), `ui.pill.reward` (the win's and the milestone's reward pills). The `mat.` prefix is the `Material` category and `board.` belongs to `BoardTile`. Owner pictures (kind `Picture`, research
D16 and `pictures.md`) keep or add their `bg.*`, `char.hero.*` and `ui.logo` slots with the drawn stand-in as fallback;
the optional celebrating heroes (A7) are `char.hero3d.cheer.sprig|bloom|drop|twig` (`CharacterArt.CheerSlot`, picture
`CharacterArt.Cheer(family)` = `3d/{family}-cheer`), with the group picture standing in until they exist.
