# Pictures the owner makes (spec 005 FR-019)

Everything else of the reference look is drawn in code. These pictures replace drawn or generated stand-ins; until a
file exists, the stand-in shows. Drop a file at its path with its exact name; both builds pick it up (the playtest
embeds the folders at build time, Unity loads them from `Resources`).

Common rules:
- PNG, sRGB, 8-bit. Characters and the logo with a transparent background; backgrounds opaque.
- Characters: front or three-quarter view, the whole body inside the picture with a 4% margin, feet on the bottom
  margin, no ground, pedestal or shadow (the game draws them), soft light from the upper left as in the reference.
- Backgrounds: 1080 × 2340 (19.5:9). Keep the important part inside the middle 1080 × 1920; the top 12% sits under the
  top bar and the bottom 30% under the buttons or the tray.
- The art is the project's own (see `tools/artgen/OWNERSHIP.md` for how ownership is recorded); note the tool and
  licence of each picture (for example in `tools/artgen/models/*.md`).

## A. 3D heroes (Home, win and milestone, Wardrobe, profile)

Folder: `client/Assets/Bloomlings/Art/Characters/Resources/Characters/3d/` (replaces the generated pictures; keep the
names and sizes).

| # | File | Size | Pose | Where | Slot |
|---|---|---|---|---|---|
| A1 | `sprig.png` | 512 × 576 | idle, happy, looking at the viewer | Home (progressed), Wardrobe, profile | `char.hero3d.sprig` |
| A2 | `bloom.png` | 512 × 576 | idle, happy | same | `char.hero3d.bloom` |
| A3 | `drop.png` | 512 × 576 | idle, happy | same | `char.hero3d.drop` |
| A4 | `twig.png` | 512 × 576 | idle, happy | same | `char.hero3d.twig` |
| A5 | `sprig-blank.png` … `twig-blank.png` | 512 × 576 | the same four pictures without eyes and mouth (a worn cosmetic expression draws the face) | Wardrobe with an expression | `char.hero3d.*` |
| A6 | `group.png` | 1200 × 720 | the four together, Sprig left, Bloom, Drop, Twig right, as in the reference's strip | Home (early), win, milestone, splash | `char.hero3d.group` |
| A7 | `sprig-cheer.png` … `twig-cheer.png` | 512 × 576 | celebrating: arms up, eyes closed with joy (the reference's win Bloom) | win card (the family of the level's main variant) | `char.hero3d.cheer.*` |

Optional, if the outfits should be modelled instead of drawn over the hero:
- A8: one picture per family and outfit, `{family}-{outfit-id}.png`, 512 × 576 (the reference's "Flower Hat",
  "Explorer"). Without them the drawn hats, expressions and trails stay.
- A9: tab heads, `{family}-head.png`, 256 × 256, the head and shoulders (Wardrobe family tabs). Without them the solo
  picture is used, scaled.

## B. Backgrounds

Folder: `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`.

| # | File | Where | What | Slot |
|---|---|---|---|---|
| B1 | `home.png` | Home | the garden diorama of the reference: ruins with stone arches, a round stone well with the lotus fountain in the middle, flowers and bushes, soft sunlight; **without the heroes** (they stand in front, wearing their outfits); empty space at the top for the logo and at the bottom for the level plaque and Play | `bg.home` |
| B2 | `gameplay-daylight.png` | levels 1–99 and every fourth band | a lawn seen from above with flowers and bushes at the edges, the middle calm (the board covers 90% of the width between 14% and 70% of the height) | `bg.theme.daylight_garden` |
| B3 | `gameplay-pond.png` | from level 100 | the same lawn with water lilies and a pond edge | `bg.theme.pond` |
| B4 | `gameplay-orchard.png` | from level 150 | the lawn with fruit trees' shade and fallen fruit | `bg.theme.orchard` |
| B5 | `gameplay-moonlit.png` | from level 200 | the lawn at dusk with fireflies | `bg.theme.moonlit_garden` |
| B6 | `splash.png` | splash | the Home garden, more blossoms | `bg.splash` |
| B7 | `wardrobe.png` | Wardrobe | the garden arches of the reference's Wardrobe, empty middle for the hero on the pedestal | `bg.wardrobe` |

## C. Logo

| # | File | Size | What | Slot |
|---|---|---|---|---|
| C1 | `client/Assets/Bloomlings/Art/Brand/Resources/Brand/logo.png` | 1200 × 440, transparent | the reference's wooden "Bloomlings" letters with leaves and small flowers | `brand.wordmark` |
| C2 | `client/Assets/Bloomlings/Art/Brand/Resources/Brand/tagline.png` | 1000 × 80, transparent | "SMALL FRIENDS. BIG GARDENS." (optional; the game can also write it) | `brand.tagline` |

## D. Optional painted UI (only if the code-drawn versions should be replaced)

All of these are drawn in code today (contracts/look.md) and need no picture. If the owner prefers painted art, these
are the pieces, each transparent, at 2× the size it appears on a 1080-wide screen: the wooden sign plank (9-slice,
512 × 128), the dark wooden pod frame (9-slice, 256 × 256), a stone block (128 × 64), the four booster icons (256 ×
256), the eight variant sticker icons (256 × 256), the lotus (128 × 128), the ivy and flower clusters (256 × 256).
