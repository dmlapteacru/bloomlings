# Pictures the owner makes (spec 005 FR-019)

Everything else of the reference look is drawn in code. These pictures replace drawn or generated stand-ins; until a
file exists, the stand-in shows. Drop a file at its path with its exact name; both builds pick it up (the playtest
embeds the folders at build time, Unity loads them from `Resources`). The 3D heroes of section A also need one
`tools/artgen -- adopt` command (section A).

Common rules:
- PNG, sRGB, 8-bit. Characters and the logo with a transparent background; backgrounds opaque.
- Characters: front or three-quarter view, the whole body inside the picture with a 4% margin (at least 2% stays fully
  transparent on every side), no ground or pedestal (the game draws them), at most a soft contact shadow under the feet,
  soft light from the upper left as in the reference. The hosts stand a picture on its feet line: 90% down a solo hero
  picture (A1–A5, A7; `HomeStage.FeetShare`) and 62% down the group (A6; `CharacterArt.GroupFeetShare`), so put the
  feet on that line and keep the space below it clear but for that shadow.
- Backgrounds: 1080 × 2340 (19.5:9). Keep the important part inside the middle 1080 × 1920; the top 12% sits under the
  top bar and the bottom 30% under the buttons or the tray.
- Note the tool, the author and the licence of each picture in a source record in the repository (by default
  `tools/artgen/models/owner-pictures.md`; `tools/artgen/models/leafling.md` is an example). The originality test
  needs it: section A pictures name it through `adopt`, and every background or logo (B, C) needs a row in
  `client/THIRD_PARTY_NOTICES.md` with its path and the record as its licence file, or
  `OriginalityTests.Client_HasNoImportedArtAudioOrFonts` fails.

## A. 3D heroes (Home, win and milestone, Wardrobe, profile)

Folder: `client/Assets/Bloomlings/Art/Characters/Resources/Characters/3d/`, with the generated pictures' exact names
and sizes (`CharacterArt.Hero`, `CharacterArt.Group`, `CharacterArt.Cheer`). A file replaces the generated picture of
that name; the cheer pictures (A7) are new.

`tools/artgen` manages this folder: its `manifest.json` lists every file with its hash, `-- check` fails on a file it
does not list or that differs from a fresh render, and `-- build` rewrites the generated pictures. So record each owner
picture before committing it: write its source record (tool, author, licence; by default
`tools/artgen/models/owner-pictures.md`), drop the file in, then run
`dotnet run --project tools/artgen -- adopt 3d/<file>.png`. The manifest then marks it `"source": "owner"`: `build`
keeps it, `check` verifies only its size, margin, hash and record, and the originality test accepts it with its record
(`tools/artgen/README.md`, "The owner's pictures"). When a hero (A1–A4) is the owner's, its face sits where the owner
drew it: set `CharacterArt.FaceCenterHero` to it, so a worn expression lands on the face, and deliver its blank twin
(A5) too.

| # | File | Size | Pose | Where | Slot |
|---|---|---|---|---|---|
| A1 | `sprig.png` | 512 × 576 | idle, happy, looking at the viewer | Home (progressed), Wardrobe, profile | `char.hero3d.sprig` |
| A2 | `bloom.png` | 512 × 576 | idle, happy | same | `char.hero3d.bloom` |
| A3 | `drop.png` | 512 × 576 | idle, happy | same | `char.hero3d.drop` |
| A4 | `twig.png` | 512 × 576 | idle, happy | same | `char.hero3d.twig` |
| A5 | `sprig-blank.png` … `twig-blank.png` | 512 × 576 | the same four pictures without eyes and mouth (a worn cosmetic expression draws the face) | Wardrobe with an expression | the family's `char.hero3d.{family}` |
| A6 | `group.png` | 1200 × 720 | the four together, Sprig left, Bloom, Drop, Twig right, as in the reference's strip; no base (the game stands them on its stone pedestal); their feet on the line 62% down the picture (`CharacterArt.GroupFeetShare`), their heads from about 15% down (`CharacterArt.GroupHeadShare`) and the space below the feet clear, as in the generated group | Home (early), win, milestone, splash | `char.hero3d.group` |
| A7 | `sprig-cheer.png` … `twig-cheer.png` | 512 × 576 | celebrating: arms up, eyes closed with joy (the reference's win Bloom); feet on the line 90% down (`HomeStage.FeetShare`), as the solo heroes | win and milestone cards (the family of the level's main variant; the group stands in while it is missing) | `char.hero3d.cheer.{family}` (`CharacterArt.CheerSlot`) |

Optional, if the outfits should be modelled instead of drawn over the hero (no build loads these yet: they need their
slots and loading code first):
- A8: one picture per family and outfit, `{family}-{outfit-id}.png`, 512 × 576 (the reference's "Flower Hat",
  "Explorer"). Without them the drawn hats, expressions and trails stay.
- A9: tab heads, `{family}-head.png`, 256 × 256, the head and shoulders (Wardrobe family tabs). Without them the solo
  picture is used, scaled.

## B. Backgrounds

Folder: `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`. The names are `OwnerPictures.Home`,
`Splash`, `Wardrobe` and `OwnerPictures.Gameplay(themeId)`; `OwnerPictures.SlotOf` gives each one's slot.

| # | File | Where | What | Slot |
|---|---|---|---|---|
| B1 | `home.png` | Home | the garden diorama of the reference: ruins with stone arches, a round stone well with the lotus fountain in the middle, flowers and bushes, soft sunlight; **without the heroes** (they stand in front, wearing their outfits); empty space at the top for the logo and at the bottom for the level plaque and Play | `bg.home` |
| B2 | `gameplay-daylight.png` | levels 1–99 and every fourth band | a lawn seen from above with flowers and bushes at the edges, the middle calm (the board covers 90% of the width between 14% and 70% of the height) | `bg.theme.daylight_garden` |
| B3 | `gameplay-pond.png` | from level 100 | the same lawn with water lilies and a pond edge | `bg.theme.pond` |
| B4 | `gameplay-orchard.png` | from level 150 | the lawn with fruit trees' shade and fallen fruit | `bg.theme.orchard` |
| B5 | `gameplay-moonlit.png` | from level 200 | the lawn at dusk with fireflies | `bg.theme.moonlit_garden` |
| B6 | `splash.png` | splash | the Home garden, more blossoms | `bg.splash` |
| B7 | `wardrobe.png` | Wardrobe (Unity; the playtest has no Wardrobe screen) | the garden arches of the reference's Wardrobe, empty middle for the hero on the pedestal | `bg.wardrobe` |

## C. Logo

Folder: `client/Assets/Bloomlings/Art/Brand/Resources/Brand/` (names `OwnerPictures.Logo` and `OwnerPictures.Tagline`).

| # | File | Size | What | Slot |
|---|---|---|---|---|
| C1 | `logo.png` | 1200 × 440, transparent | the reference's wooden "Bloomlings" letters with leaves and small flowers | `brand.wordmark` |
| C2 | `tagline.png` | 1000 × 80, transparent | "SMALL FRIENDS. BIG GARDENS." (optional, for later: neither build shows a tagline yet, so the slot is registered as not drawn) | `brand.tagline` |

## D. Optional painted UI (only if the code-drawn versions should be replaced)

All of these are drawn in code today (contracts/look.md) and need no picture. If the owner prefers painted art, these
are the pieces, each transparent, at 2× the size it appears on a 1080-wide screen: the wooden sign plank (9-slice,
512 × 128), the dark wooden pod frame (9-slice, 256 × 256), a stone block (128 × 64), the four booster icons (256 ×
256), the eight variant sticker icons (256 × 256), the lotus (128 × 128), the ivy and flower clusters (256 × 256).
