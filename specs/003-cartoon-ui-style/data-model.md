# Data Model: Cartoon UI Style — the "Garden" direction

Engine-free values in `client/Assets/Bloomlings/UI/Design/`, shared by the Unity client and the playtest. No save,
content or economy data changes.

## ColorSet

The shades of one element color (FR-008).

| Field | Meaning | Derivation from the base color |
|---|---|---|
| `Face` | the button face | the base color |
| `Top` | the lighter top of the face gradient | lighten 0.18 |
| `Lip` | the darker band along the bottom edge | darken 0.25 |
| `Line` | the button outline, and the label outline and extrusion | darken 0.42 |

- **Named sets** (`DesignTokens.Garden.Sets`):
  - green (primary), cream (secondary), white (icon), blue (level pill), dark (2×), red (close, HARD), purple (SUPER
    HARD);
  - booster.extra_slot, booster.shuffle, booster.return, booster.bloom_burst.
- **Rules.**
  - `Line` is never pure black.
  - A white label on `Face` reaches 3:1 against `Line` (FR-025, tested).
  - The cream and white sets use a brown `Line` (`garden.outline`).

## Garden recipe tokens

[contracts/garden-tokens.md](contracts/garden-tokens.md) has the names and values. In short:

- **Plate:**
  - `garden.plate` (cream top and bottom);
  - `garden.outline` (brown);
  - `garden.plate_depth` (6);
  - `garden.plate_inset` (11 large, 8 medium, 5 small);
  - `garden.outline_width` (3).
- **Button:** `garden.lip` (13 large, 10 medium, 6 small) and `garden.highlight` (alpha 0.5, top 36%).
- **Wood:** `garden.wood` (frame outline and thickness) and `garden.paper` (card surface).
- **Cells:** `garden.cell_lip` (14) and `garden.cell_highlight` (0.4); **pods:** `garden.pod_lip` (20).
- **Decoration:**
  - `garden.decorations` (on);
  - `garden.deco_size` (0.75 of the button height);
  - `garden.leaf` (3 greens) and `garden.flower` (white, a grey outline, a yellow center).
- **Sizes:**
  - `size.play` (540 × 204);
  - `size.card_primary` (620 × 140);
  - `size.card_secondary_width` (580);
  - `size.booster_tile` (152 × 156).

## TextLook

How a label is drawn (FR-009, FR-025).

| Field | Type | Meaning |
|---|---|---|
| `FillTop`, `FillBottom` | color | vertical gradient of the letters |
| `Outline` | color | outline and extrusion color (the set's `Line`) |
| `OutlineEm` | float | outline width as a fraction of the font size (0.036) |
| `ExtrudeEm` | float | extrusion depth below the letters (0.09) |
| `ShadowAlpha` | float | soft shadow under the extrusion (0.3); Unity draws none (one underlay only) |
| `Emboss` | color or none | a light line just under plain labels on cream faces (white at 70%) |

Three kinds:

- **`TextLook.OnColor(ColorSet)`.** For green, blue, red, dark, booster, header and HUD faces: a cream-white gradient,
  the outline and extrusion in the set's `Line`.
- **`TextLook.Plain(color)`.** For cream and white faces and body text: one color, no outline, no extrusion, the light
  emboss.
- **`TextLook.Headline`.** Home's "Level N" over the garden: the cream-white gradient outlined and extruded in dark
  brown, as on the approved mockup.

## Font

| Role | File | Used by |
|---|---|---|
| Display (all bold `TypeStyle`s) | `Nunito-ExtraBold.ttf` (800) | buttons, titles, pills, counts, badges, the level text |
| Body (non-bold `TypeStyle`s) | `Nunito-SemiBold.ttf` (600) | body, captions |

- The files and `OFL.txt` live in `client/Assets/Bloomlings/UI/Fonts/Resources/`.
- If loading fails, each host falls back to its system font ([contracts/fonts.md](contracts/fonts.md)).

## TypeStyle (changed)

- `Upper` becomes false for `type.title_caps`, `type.level_home`, `type.level_pill`, `type.button_large`,
  `type.button` and `type.button_secondary`. `type.badge` stays uppercase.
- `Outline` keeps its meaning: whether the style takes an outlined look on colored faces.

## BoosterTileState

What a booster tile shows (FR-031, [contracts/booster-tile.md](contracts/booster-tile.md)).

| Field | Source |
|---|---|
| `Booster` | the booster id (Extra Slot, Shuffle, Return, Bloom Burst) |
| `Charges` | the economy (owned charges) |
| `Price` | the economy (Petal price) |
| `Selected` | the level screen's targeting (Return, Bloom Burst only) |
| `Usable` | `LevelSession.Check(command).IsAllowed` and not won (spec 001 FR-046) |
| `Affordable` | charges > 0, or Petals ≥ price |

The look is derived from these fields:

| Look | When |
|---|---|
| `Charges` | `Charges > 0` |
| `Price` | `Charges == 0` |
| `Selected` | `Selected` (overrides the others) |
| `Disabled` | `!Usable` or (`Charges == 0` and `!Affordable`) |
| `Pressed` | finger down on an enabled tile |

## Decoration

| Field | Meaning |
|---|---|
| `Corners` | top-left and bottom-right of the button box |
| `Size` | `garden.deco_size` × the button height |
| `Parts` | three leaves (fanning up-left, up and right) and a white flower with a yellow center, each with its own outline (`DecorationPart`, `ShapeLibrary.DecorationPartSdf`) |
| Applied to | PLAY, and the primary button of the pause, win, milestone and Daily Reward cards |

The decoration never takes touch input, and is drawn after the button so it may overlap the plate's corner.

## BloomlingLook (FR-032)

One kawaii Bloomling picture (contracts/bloomling-look.md).

| Field | Meaning |
|---|---|
| `Family` | the body shape: Sprig egg, Bloom bulb, Drop droplet, Twig stump |
| `Color` | the variant color, or its muted (queued) or stuck shade |
| `IconId` | the variant's icon id: picks the crest and turns the belly badge on; null for the family's own crest without a badge |
| `Mood` | `Happy`, `Sleepy`, `Worried` or `None` |
| `Badge` | draw the belly badge when there is an icon |
| `Halo` | the walkers' white sticker edge |
| `Key` (derived) | every field above; the same key always renders the same pixels |

Rules:

- The body color is the variant color, lightened 0.18 when its luminance is below 0.5. The outline is a darker shade
  of the body, never black.
- The symbol on the badge is the variant color darkened until it reaches 3:1 on `char.badge`.
- On a card the figure fits between the card top and the count pill, so the pill never covers the badge.
- The `char.*` shapes are the figure's outer edge, so skins and hit areas follow it.

## Asset slot (new)

| Id | Category | Placeholder | Readability |
|---|---|---|---|
| `ui.deco.garden` | UI kit | Shape (the whole cluster; its parts are drawn one by one in their colors) | no |

The `ui.play` triangle is a shape of the existing `ui.*` family, and gets its own slot (`ui.play`, UI kit, Shape).
