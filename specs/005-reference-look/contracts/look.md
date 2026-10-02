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
| `WoodLight` | `wood.light` | `#EBC48C` | sign / rim face top |
| `WoodMid` | `wood.mid` | `#D3A066` | sign / rim face bottom |
| `WoodGrain` | `wood.grain` | `#A8733F` | grain lines (alpha 0.25–0.45) |
| `WoodEdge` | `wood.edge` | `#9A6232` | sign lower lip |
| `WoodLine` | `wood.line` | `#6B4020` | sign / rim outline |
| `WoodDark` | `wood.dark` | `#8A5634` | pod frame face |
| `WoodDarkTop` | `wood.dark_top` | `#A86F45` | pod frame top light |
| `WoodDarkLine` | `wood.dark_line` | `#4A2A14` | pod frame outline |
| `StoneTop` | `stone.top` | `#F4ECDB` | stone block top light |
| `StoneFace` | `stone.face` | `#E3D6BA` | stone block face |
| `StoneLip` | `stone.lip` | `#C2B08C` | stone block lower edge |
| `StoneLine` | `stone.line` | `#9E8B69` | stone outline and joints |
| `StoneMoss` | `stone.moss` | `#7DB24A` | moss patches |
| `ParchmentTop` | `parchment.top` | `#FFF8E8` | card / tray top |
| `ParchmentBottom` | `parchment.bottom` | `#F5E4C3` | card / tray bottom |
| `ParchmentEdge` | `parchment.edge` | `#E2C99B` | inner border line, plate depth |
| `ParchmentLine` | `parchment.line` | `#A97E4C` | card outline |
| `ParchmentWell` | `parchment.well` | `#EEDDBA` | inset wells (jam row, sunk tabs) |
| `CreamFace` | `cream.face` | `#FBF0DA` | cream button / slot / booster face |
| `CreamTop` | `cream.top` | `#FFFAF0` | cream face top |
| `CreamLip` | `cream.lip` | `#D9C196` | cream lower lip |
| `CreamLine` | `cream.line` | `#B99367` | cream outline |
| `InkBrown` | `ink.brown` | `#5A3418` | titles, sign letters, counts, glyphs on cream |
| `InkBrownSoft` | `ink.brown_soft` | `#7B5A3A` | body text on parchment |
| `LotusFill` | `lotus.fill` | `#F59CBF` | lotus petals |
| `LotusTip` | `lotus.tip` | `#FFD3E3` | lotus petal light |
| `LotusLine` | `lotus.line` | `#D9568A` | lotus outline |
| `BadgeGreen` | `badge.green` | `#245C34` | count badge disc |
| `LawnLight` | `lawn.light` | `#93CC5B` | gameplay lawn |
| `LawnDark` | `lawn.dark` | `#5E9E3D` | lawn shade, grass strokes |
| `IvyLeaf` | `ivy.leaf` | `#79BE47` | ivy / clover leaves on signs |
| `IvyLine` | `ivy.line` | `#3F7D26` | ivy outline |
| `ButtonBlue` | `button.blue` | `#45A3EE` | jam Return / Bloom Burst buttons |
| `ButtonOrange` | `button.orange` | `#F6B021` | orange buttons ("Next" in the strip) |
| `RayLight` | `ray.light` | `#FFF4C8` | win light rays (alpha) |

`ButtonPrimary` becomes `#4DB847` (top `#86DC5E`, lip `#2D8A32`, line `#1F6427`); `GardenLook.Green` follows it.

### 1.3 Color sets (`GardenLook`)

| Set | Face | Top | Lip | Line | Label |
|---|---|---|---|---|---|
| `Green` | `#4DB847` | `#86DC5E` | `#2D8A32` | `#1F6427` | white, outlined `Line` |
| `Blue` (jam) | `ButtonBlue` | lighten 0.3 | darken 0.25 | darken 0.42 | white, outlined `Line` |
| `Orange` | `ButtonOrange` | lighten 0.3 | darken 0.22 | darken 0.42 | white, outlined `Line` |
| `Cream` | `CreamFace` | `CreamTop` | `CreamLip` | `CreamLine` | `InkBrown`, plain |
| `White` (round buttons, Petals pill) | = `Cream` | | | | `InkBrown` |

The level pill sets (`Blue`/`Lilac` for the gameplay level) are replaced by the wooden sign; Super Hard tints the sign's
letters `BadgeSuperHard` and keeps the SUPER HARD badge under it.

## 2. Material pictures (`UiRaster`, engine-free)

`client/Assets/Bloomlings/UI/Design/UiRaster.cs`. Every function returns **straight-alpha RGBA bytes, rows from the
top**, `w * h * 4` long, anti-aliased edges (1 px), deterministic for the same arguments (hash noise, no `Random`).
Sizes are pixels. Common helpers: rounded-rect SDF, value noise / fbm, smoothstep, `Rgba.Mix`.

| Function | Picture |
|---|---|
| `Plank(w, h, radius, outline, WoodTone tone, int seed)` | A wooden plank: vertical gradient top→bottom, horizontal grain (fbm stretched 1:12 along x plus 3–5 darker thin wavy lines and an occasional knot ellipse), a 1–2 px lighter bevel inside the top edge, a darker lip band along the bottom (12% of h), the outline. `WoodTone.Light` uses `WoodLight`/`WoodMid`/`WoodGrain`/`WoodEdge`/`WoodLine` (signs, rims); `WoodTone.Dark` uses `WoodDarkTop`/`WoodDark`/`WoodDarkLine` (pod frames). Two tiny nail dots near the ends when `h > 48`. |
| `Frame(w, h, radius, border, WoodTone tone, int seed)` | The plank material as a ring of width `border` (hole transparent) with an inner shadow along the hole's top and a light inner edge along its bottom: the pod frame. |
| `Stone(w, h, radius, outline, int seed)` | A stone block: `StoneTop`→`StoneFace` gradient, mottled fbm (±6% light), a few darker speckles, a light bevel at the top, `StoneLip` along the bottom 14%, `StoneLine` outline; `seed` varies the mottling and an optional moss patch (`StoneMoss`) on a corner. |
| `Tile(size, Rgba color, string iconId, TileStyle style)` | The candy tile (§3.1), square. |
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

- Shape: a square, corner radius 16% of its side (board) or 20% (sticker).
- Face: vertical gradient from `color.Lighten(0.28)` (top) to `color` (60%) to `color.Darken(0.06)` (bottom).
- Lip: `color.Darken(0.28)` along the bottom 9% (board) / 10% (sticker), under the face.
- Outline: 1.5% of the side (min 1 px) in `color.Darken(0.45)` at 80% alpha (board tiles look separated by thin dark
  lines, as in the reference).
- Gloss: a white band from 6% to 34% of the face height, inset 10% left/right, alpha 0.38 → 0 downward; plus a small
  white dot (6% of the side) near the top-left corner at alpha 0.55.
- Symbol, **board style**: the variant symbol (`ShapeLibrary.SymbolId(iconId)`) at 54% of the side, centered 2% above
  the middle, drawn twice: a light copy `color.Lighten(0.42)` at alpha 0.55 offset (−1.5%, −2%) of the side, then the
  symbol in `color.Darken(0.24)`. Below 28 px per tile the gloss dot and the light copy are skipped.
- Symbol, **sticker style** (pods, slots, jam row, Collection, the strip): the symbol at 66% of the side with a rim
  (the symbol grown by 7% of its size) in `color.Lighten(0.55)`, a fill from `icon top` to `icon` (per variant, §3.1.1),
  a white highlight ellipse clipped to the symbol's upper left (alpha 0.5), and the variant's detail.
- States: `Dimmed` (queued pods) mixes face and symbol 45% toward `ParchmentBottom`; `Grey` (stuck) uses
  `color.Grey()`; `Pressed` sinks the face into the lip (`Kit.Block` press); `Mystery` uses `TileMystery` with a white
  "?" (`tile.mystery`) and no symbol.

#### 3.1.1 Sticker icon colors and details

| Variant | Icon fill (top → bottom) | Detail |
|---|---|---|
| Leaf | `#5FB84A` → `#2F8C32` | a lighter midrib and two side veins `#8ED86A` |
| Moss | `#22B79C` → `#0D7C68` | three lighter dimples |
| Flower | `#FFD2E2` → `#F79AC0` | a yellow center `#FFD35C` with an orange dot |
| Violet Bud | `#C58BF5` → `#8E4BD8` | a lighter middle line and two green sepals `#5BAA3A` at the base |
| Water | `#5FB6FF` → `#1E6FD6` | a white curved highlight on the left |
| Dew | `#E6FFFF` → `#9EEFF3` | a white sparkle star at the top right |
| Wood | `#C47A3C` → `#7A3A12` | two concentric darker rings on the stump top |
| Acorn | `#E39A4A` → `#A35A18` (nut), cap `#7A4A22` | a cap line and a tiny stem |
| Expansion | `color.Lighten(0.2)` → `color.Darken(0.15)` | none |

The symbols themselves are redrawn in `ShapeLibrary` (research D13) so that each reads as in the reference strip while
the two variants of a family keep different silhouettes: leaf (pointed oval with a stem), moss (scalloped round
cushion), flower (five round petals), bud (tulip bud with three tips), water (pointed teardrop), dew (round droplet,
its sparkle as a separate part), wood (stump: a short cylinder with a ringed top), acorn (cap and nut).

### 3.2 Wooden sign — `Kit.WoodSign(p, box, text, TypeStyle style, SignDecor decor)`

Reference crops: the gameplay top bar, "Level Complete!", the Wardrobe banner, the Home "LEVEL 88" plaque.

- `UiRaster.Plank` (Light) filling `box`, radius 28% of the height, outline 2.5% of the height (min 2 px), and a soft
  shadow below (`GardenShadow` alpha 0.22, offset 6% of the height).
- Letters: `InkBrown` with a light emboss (`TextLook.Plain`-like, emboss `WoodLight.Lighten(0.4)`), centered, max width
  82% of the plank.
- `SignDecor.Ivy`: clover/ivy clusters (3–4 leaves, `IvyLeaf`/`IvyLine`) overlapping both ends (gameplay level sign,
  Wardrobe/Store banner). `SignDecor.Flowers`: a cluster of 2–3 big leaves and one white flower with a yellow center at
  the top-left and the bottom-right ends (win sign). `SignDecor.None`: Home plaque, card headers.
- Never a touch target.

### 3.3 Buttons

- **Primary** (`Kit.PrimaryButton`): the green set's face (spec 003 `Kit.Face`) with a stronger highlight band (top 8%
  to 46%, alpha 0.45 → 0) and a light wood rim instead of the cream plate: `UiRaster.Plank` (Light) behind the face,
  inset by 7% of the height on every side, outline `WoodLine`. White label outlined in the set's line, extruded.
  Pressed: the face sinks (existing press depth) and darkens by 8%. Decorations (leaves and flower at the corners) stay.
- **Secondary** (`Kit.SecondaryButton`): the cream set on a cream plate (`ParchmentEdge` depth), brown label, optional
  brown glyph on the left (Restart's ⟳).
- **Round / squircle icon buttons** (`Kit.RoundButton`): cream face with a thick light rim (`CreamTop`, 9% of the size)
  and a `CreamLip` lower edge, `CreamLine` outline, soft shadow; brown glyph (`InkBrown`) at 46% of the size. Pause and
  speed in the top bar are squircles (radius 34% of the height); Settings, back and close are circles. Close is cream
  with a brown ✕ (no longer red).
- **Speed pill** (`Kit.SpeedPill`, replaces `DarkPill`): the cream squircle style, wider, with the speed text ("1×" or
  "2×", `InkBrown`) and the `ui.fast` glyph (▶▶) after it.
- **Choice button** (`Kit.ChoiceButton(p, box, ColorSet set, iconDraw, label, cost)`, jam): a rounded rectangle
  (radius 22% of its height) in the green or blue set with the glossy face, the icon (36% of the height) in the upper
  half, the white outlined label below it, and a cost pill (§3.4) centered on its bottom edge, overlapping by 40% of the
  pill's height.
- Orange (`GardenLook.Orange`) is available for a highlighted secondary call to action.
- Disabled buttons use `ColorSet.Disabled()` and alpha 0.55 as before.

### 3.4 Pills and badges

- **Count badge** (`Kit.CountBadge`): a `BadgeGreen` disc with a 10% white ring and a 3% `GardenShadow` outline; white
  digits (`type.badge` size scaled to the disc). Booster tiles place it at the bottom-right corner, overlapping by a
  third.
- **Cost pill** (`Kit.CostPill(p, box, Cost cost)`): cream (`CreamFace` → `ParchmentBottom`), `CreamLine` outline,
  soft shadow; contents: the lotus and the price (`InkBrown`), or a green ▶ square and "Free", or "×N" charges.
- **Petals pill** (`Kit.PetalsPill`): the cream style, lotus on the left (overlapping the edge by 10%), amount in
  `InkBrown`, the green round "+" on the right.
- **Lotus** (`Kit.Petal`, shape `currency.petal` redrawn as a lotus): five pointed petals (three in front, two behind)
  in `LotusFill` with `LotusTip` tips and `LotusLine` outline.

### 3.5 Surfaces

- **Parchment** (`Kit.Paper`, all cards, the jam sheet, the tray panel, the slot band): `ParchmentTop` →
  `ParchmentBottom` gradient, a `ParchmentLine` outline (frame width token), a thin `ParchmentEdge` inner line 1.2% of
  the shorter side inside it, a soft shadow; no wood frame.
- **Well** (`Kit.Well`): `ParchmentWell` with an inner shadow along the top and a `ParchmentEdge` outline (jam slot
  row, sunk tabs, empty plates).
- **Card** (`Kit.Card`): parchment; a title in `type.title` `InkBrown` (no green band) or a `WoodSign` header when the
  screen says so (Win, Store, Wardrobe); the cream round close button over the top-right corner.

### 3.6 Board furniture

- **Stone border** (`Kit.StoneBorder(p, gridBox, cell)`): blocks of `UiRaster.Stone` around the grid, thickness 0.42
  cell, lengths alternating 1.0 and 0.8 cell (corners are square blocks 0.42 × 0.42 rounded 40%), a dark joint of 0.04
  cell between blocks (`StoneLine` at alpha 0.6), seeds by position so the border never flickers. A 0.06 cell dark green
  (`LawnDark.Darken(0.3)`) gap between the stones and the tiles.
- **Garden Entry** (`Kit.StoneArch`): a half ring of 5 small stone blocks (outer radius 0.55 cell) opening toward the
  board on the entry's side, with a darker opening (`LawnDark.Darken(0.45)`).
- **Lawn**: the gameplay backdrop scene becomes a lawn (§4.2).
- **Pedestal** (`Kit.StonePedestal(p, box)`): an ellipse-topped stone drum: top ellipse `StoneTop`, side `StoneFace`
  with 4 block joints, `StoneLip` bottom, moss tufts at the base. Win, Home (until the owner's diorama) and Wardrobe.

### 3.7 Tray pieces

- **Pod** (`Kit.PodFrame(p, box, state)` + `CandyTile` sticker + count): `UiRaster.Frame` (Dark) filling the pod box,
  radius 18%, border 11% of the width; inside it a cream panel (`CreamTop` → `CreamFace`); the candy tile (sticker) at
  70% of the inner width near the top; the count below it, `type.count` `InkBrown`, no "x". A short wooden handle
  (`WoodDark`, 22% × 7% of the box) on top of the exposed pod. Queued pods (the ones below in a column) draw the same
  frame at 55% brightness mixed toward `ParchmentBottom` and a dimmed tile. Locked: the frame with `StateLockBg` inner
  and the lock glyph; mystery: the lilac mystery tile; connected: the link bar between frames as now. The "+N" depth
  badge stays (count badge style, §3.4).
- **Waiting Slot** (`Kit.SlotPlate`): a cream plate (raised: `CreamTop`→`CreamFace`, `CreamLip`, `CreamLine`, radius
  20%); filled: the sticker tile at 70% of the width near the top and the count below it; empty: a dashed rounded inner
  outline (`CreamLine` alpha 0.8, dash 9%/6% of the side) on a slightly sunk face; stuck: grey tile + the hourglass
  badge; danger: the dashed outline in `StateDanger` with "!"; extra slot: the green "+" badge; locked: grey face with
  the lock.
- **Booster tile** (`Kit.BoosterTile`): a cream squircle (radius 26%) with a grey-beige rim (`CreamLine.Mix(StateStuck,
  0.4)`), the booster icon (§3.8) at 62%, the count badge or the cost pill; selected: the existing glow ring and lift;
  disabled: greyed.

### 3.8 Icons (`ShapeLibrary`, multi-part icons drawn by `Kit.BoosterIcon`)

| Icon | Look |
|---|---|
| Extra Slot | a blue disc (`#3E9BEA`, outline darker) with a white bold "+" |
| Shuffle | two curved arrows chasing each other, orange `#F2A33A` and green `#57B847`, white outline |
| Return | a fat curved arrow pointing left, yellow `#FFC23D` with an orange `#E08A1E` outline |
| Bloom Burst | a five-petal flower, pink `#F58CC8` petals, yellow center |
| `ui.fast` | two brown chevrons ▶▶ |
| `ui.back` | a brown left arrow |
| `ui.restart` | a circular arrow (existing, brown on cream) |

### 3.9 Decorations

- **Ivy cluster**, **flower cluster**: `ShapeLibrary` leaf shapes in `IvyLeaf` with `IvyLine` outline, white flower
  (`GardenFlower`) with a yellow center; never touch targets.
- **Light rays** (win): 10 soft wedges from the pedestal's center, `RayLight` alpha 0.0–0.35, slowly turning (0.05 turn
  per second).
- **Falling petals** (win): 10 pink petal shapes (`LotusFill`, `LotusTip`) drifting down and swaying.

## 4. Screens

Positions and order stay as in spec 002; only the looks change.

### 4.1 Gameplay (frames 7–9, 12–14, 21–23)

- Top bar: Pause squircle, level `WoodSign` (Ivy) instead of the level pill, speed pill.
- Board: lawn backdrop, `StoneBorder`, candy tiles (board style), restored ground as pale flat cells
  (`PictureColor` lightened 0.55, radius 10%, no bevel, a faint inner shadow), stones/keys/locks/layers/specials as now
  (stone obstacles in `StoneFace` tones), entries as `StoneArch`, walkers unchanged.
- Slot row: on a parchment band, `SlotPlate`s.
- Tray: parchment panel behind the columns; pods per §3.7.
- Booster bar: on the parchment, `BoosterTile`s.

### 4.2 Backdrop

`BackdropScene.Gameplay` becomes a lawn: `LawnLight` → `LawnDark` fbm grass with fine darker strokes, scattered small
flowers (white, pink, yellow five-dot blossoms), darker leafy clumps along the screen edges, a soft vignette; no sky.
Home and Splash keep the sky, arches and hills but warmer (until the owner's pictures).

### 4.3 Popups and cards (frames 10, 11, 17–20)

- Jam sheet: parchment sheet; title "No more space!" (`InkBrown`, `type.title`); subtitle "All Waiting Slots are full.
  Choose a way to continue." (`InkBrownSoft`); an inset `Well` with the slot contents (sticker tiles with counts); one
  `ChoiceButton` per recovery booster (Extra Slot green, Return blue, Bloom Burst blue; Shuffle green if offered) with
  its cost pill (×N charges, or lotus + price); the free rescue as a `ChoiceButton`-wide green button with "▶ Free"
  pill, or the primary button with the ad glyph; Restart as a cream secondary button with ⟳.
- Pause, Settings, Store, Daily reward, Collection, Leaderboard, Themes, Milestone: `Card` per §3.5; the Store and
  milestone use a `WoodSign` header.

### 4.4 Win (frame 15)

- `WoodSign` (Flowers) "Level complete!" as the header above the card.
- The finished picture in full color: each picture cell as a flat candy tile (no lip, small gloss, board-style symbol
  of its role's variant) inside a `StoneBorder` (thin, 0.3 cell).
- The heroes group on a `StonePedestal` with light rays and falling petals behind and around (3D pictures; the owner's
  celebrating hero later).
- The reward as a cream pill (`CostPill` style, bigger) "+N" with the lotus, counting up.
- Next: `PrimaryButton` (wood rim, decorated, breathing). ×2: cream secondary with the ad glyph.

### 4.5 Home, Splash (frames 1–3)

- Logo: "Bloomlings" in `type.wordmark` with a wooden look: fill `WoodLight` → `#E39B4E`, outline `WoodLine`, a darker
  extrusion, two leaf clusters and a small pink flower over the letters' ends.
- Level: a `WoodSign` (None) plaque with "Level N".
- Play: the big primary button in its wood rim.
- Settings, Petals pill per §3.3–3.4; heroes on a `StonePedestal` until the owner's diorama.

### 4.6 Wardrobe (Unity) and the Store's cosmetics (playtest)

- Back (`ui.back`) round button, `WoodSign` (Ivy) banner, Petals pill.
- Hero on a `StonePedestal` with ‹ › cream round arrows (Unity Wardrobe).
- Name card: parchment with a small sign-like tab carrying the name (`type.title`), the role line (`type.body`
  `InkBrownSoft`) and a description.
- Family tabs: cream tabs, top corners rounded; the selected one lighter and joined to the panel below; each shows the
  family's hero picture (or silhouette) and its name.
- Outfit cards: cream cards with a beige picture well, the name below; the worn one has a green face tint, a 4 px green
  (`GardenLook.Green.Face`) border and a green check badge.
- Footer: "Earn special outfits as you play!" (`InkBrownSoft`).

## 5. Asset slots

New slots (kind `Procedural` unless noted) registered in `AssetSlots` and marked where drawn:
`mat.wood.light`, `mat.wood.dark`, `mat.stone`, `mat.parchment`, `tile.candy`, `tile.candy.sticker`,
`ui.sign.wood`, `ui.sign.ivy`, `ui.sign.flowers`, `ui.button.rim`, `ui.button.choice`, `ui.pill.cost`,
`ui.pill.speed`, `ui.badge.count` (restyled), `board.border.stone`, `board.arch`, `board.lawn` (or `bg.gameplay`
restyled), `fx.rays`, `fx.petals`, `ui.pedestal`, `ui.tab.family`, `ui.card.outfit`, `ui.logo.wood`, `ui.back`,
`ui.fast`, `booster.extra_slot`/`shuffle`/`return`/`bloom_burst` (redrawn). Owner pictures (kind `Picture`, research
D16 and `pictures.md`) keep or add their `bg.*`, `char.hero.*` and `ui.logo` slots with the drawn stand-in as fallback.
