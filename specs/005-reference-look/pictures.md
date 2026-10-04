# Pictures the owner makes (spec 005 FR-019)

Everything else of the reference look is drawn in code. These pictures replace drawn or generated stand-ins; until a
file exists, the stand-in shows. Drop a file at its path with its exact name; both builds pick it up (the playtest
embeds the folders at build time, Unity loads them from `Resources`). The 3D heroes of section A also need one
`tools/artgen -- adopt` command (section A); the animated heroes (A10) and the Home layers (B1) go through
`tools/heroanim` instead.

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
  `tools/artgen/models/owner-pictures.md`). The originality test
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
| A1 | `sprig.png` | 512 × 576 | idle, happy, looking at the viewer | Home on the drawn stand-in (and for a family whose animated frames, A10, are missing), Wardrobe, profile | `char.hero3d.sprig` |
| A2 | `bloom.png` | 512 × 576 | idle, happy | same | `char.hero3d.bloom` |
| A3 | `drop.png` | 512 × 576 | idle, happy | same | `char.hero3d.drop` |
| A4 | `twig.png` | 512 × 576 | idle, happy | same | `char.hero3d.twig` |
| A5 | `sprig-blank.png` … `twig-blank.png` | 512 × 576 | the same four pictures without eyes and mouth (a worn cosmetic expression draws the face); until they are adopted, the hosts keep the owner's hero and show a worn expression as a small badge beside its face (`CharacterArt.HasMatchingBlank`, `OwnerBlanks`) | Wardrobe with an expression | the family's `char.hero3d.{family}` |
| A6 | `group.png` | 1200 × 720 | the four together, Sprig left, Bloom, Drop, Twig right, as in the reference's strip; no base (the game stands them on its stone pedestal); their feet on the line 62% down the picture (`CharacterArt.GroupFeetShare`), their heads from about 15% down (`CharacterArt.GroupHeadShare`) and the space below the feet clear, as in the generated group | Home (early), win, milestone, splash | `char.hero3d.group` |
| A7 | `sprig-cheer.png` … `twig-cheer.png` | 512 × 576 | celebrating: arms up, eyes closed with joy (the reference's win Bloom); feet on the line 90% down (`HomeStage.FeetShare`), as the solo heroes | win and milestone cards (the level's celebrant's, Twig or Sprig by turns, `CharacterArt.CelebrantOf`, the owner's choices of 2026-10-03; before them the family of the level's main variant) while its animated frames (A10) are missing; the group stands in while this is missing too | `char.hero3d.cheer.{family}` (`CharacterArt.CheerSlot`) |

Optional, if the outfits should be modelled instead of drawn over the hero (no build loads these yet: they need their
slots and loading code first):
- A8: one picture per family and outfit, `{family}-{outfit-id}.png`, 512 × 576 (the reference's "Flower Hat",
  "Explorer"). Without them the drawn hats, expressions and trails stay.
- A9: tab heads, `{family}-head.png`, 256 × 256, the head and shoulders (Wardrobe family tabs). Without them the solo
  picture is used, scaled.

**Delivered (2026-10-02): the animated heroes (A10, spec FR-028).** Four rigged FBX models made by the owner with Meshy
AI, one per family, carrying the clips of the owner's table (a 4 s idle, a 2 s reaction). `tools/heroanim` renders them
offline into flat frames (constitution VII: the game never loads a model; research D18):
`client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion/{family}-{idle|react|win|win2}-{NN}.png`, 144 a Meshy family (96 idle and 48
reaction frames at 24 fps, from the owner's 60 fps export of 2026-10-03, research D22) and 180 of Twig (its Blender model
of 2026-10-03: 72 idle, 36 reaction and 72 frames of the win's cheer, research D23) and 288 of Sprig (its Blender model of
2026-10-03: 96 idle, 72 reaction, 48 celebrate and 72 clap frames, research D24), 8-bit palette PNG files cropped from a 448 × 504 cell with the feet 90% down, listed in the
folder's `manifest.json`; slots `char.hero3d.motion.sprig|bloom|drop|twig` (`HeroMotion.Slot`); models in
`tools/heroanim/models/`, source record `tools/heroanim/SOURCE.md`. They show on Home and the splash (on the layered
fountain, B1) and on the win and the milestone (Twig and Sprig by turns, `CharacterArt.CelebrantOf`); the still pictures A1–A7 stay for the Wardrobe, the
profile, the group, the drawn stand-in and as the fallbacks. To change a hero, send its model again (an FBX or a `.glb`
with its textures embedded, its clips named in `tools/heroanim/heroes.json`), then run `node bake.mjs --only <family>` and `node check.mjs` in
`tools/heroanim` (`tools/heroanim/README.md`).

## B. Backgrounds

Folder: `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`. The names are `OwnerPictures.Home`,
`Splash`, `Wardrobe` and `OwnerPictures.Gameplay(themeId)`; `OwnerPictures.SlotOf` gives each one's slot. The opaque
pictures are stored as JPEG (quality 90) whatever their delivered format; a picture still awaited (B6) may come as PNG.

| # | File | Where | What | Slot |
|---|---|---|---|---|
| B1 | `home.jpg` and its layers (below) | Home, splash | **delivered (2026-10-02) as the owner's layered Home** (`bloomlings_home_assets.zip`): the garden diorama of the reference (ruins with stone arches, a round stone well with the lotus fountain in the middle, flowers and bushes, soft sunlight) in layers, **without the heroes**, so the animated heroes (A10) stand between them (spec FR-028, contracts/look.md §6.4); empty space at the top for the logo and at the bottom for the level plaque and Play (since 2026-10-04 its garden layer `home.jpg` is the owner's calm `01_home_calm_garden.png`, `bloomlings_calm_backgrounds.zip`, under the same fountain layers; `05_home_lotus_fountain_calm.png`, a garden with the fountain painted in, is not used: the heroes stand between the fountain's layers) | `bg.home` and the layer slots below |
| B2 | `gameplay-daylight.jpg` | levels 1–99 and every fourth band | a lush garden seen from above: grass, bushes and flowers at the edges, the middle calm (the board covers 86% of the width between 8% and 53% of the height, the tray the bottom 38%) (since 2026-10-04 the owner's calm `04_lotus_courtyard_calm_background.png`, `bloomlings_calm_backgrounds.zip`: fewer flowers, calmer colors) | `bg.theme.daylight_garden` |
| B3 | `gameplay-pond.jpg` | from level 100 | the same lawn with water lilies and a pond edge (since 2026-10-04 the owner's calm `06_lily_pond_calm_background.png`, `bloomlings_calm_backgrounds.zip`: fewer flowers, calmer colors) | `bg.theme.pond` |
| B4 | `gameplay-orchard.jpg` | from level 150 | the lawn with fruit trees' shade and fallen fruit (since 2026-10-04 the owner's calm `03_orchard_calm_background.png`, `bloomlings_calm_backgrounds.zip`: fewer flowers, calmer colors) | `bg.theme.orchard` |
| B5 | `gameplay-moonlit.jpg` | from level 200 | the lawn at dusk with fireflies (since 2026-10-04 the owner's calm `07_evening_fireflies_calm_background.png`, `bloomlings_calm_backgrounds.zip`: fewer flowers, calmer colors) | `bg.theme.moonlit_garden` |
| B6 | `splash.png` | splash | the Home garden, more blossoms | `bg.splash` |
| B7 | `wardrobe.jpg` | Wardrobe and the Store page (both builds) | the garden arches of the reference's Wardrobe, empty middle for the hero on the pedestal (since 2026-10-04 the owner's calm `08_wardrobe_calm_garden.png`, `bloomlings_calm_backgrounds.zip`: fewer flowers, calmer colors) | `bg.wardrobe` |
| B8 | `win.jpg` | the full-screen win | a garden with soft light from the middle, calm in the middle (the picture, the hero and the pedestal cover it); without it the gameplay garden shows, blurred and lightened (since 2026-10-04 the owner's calm `02_win_calm_garden_glow.png`, `bloomlings_calm_backgrounds.zip`: fewer flowers, calmer colors, the hero on its round stone stage, `OwnerPictures.WinStageShare` 0.60) | `bg.win` |

Since the owner's note of 2026-10-04 (spec FR-031, contracts/look.md §1.4) every background (B1 to B8) keeps at most
70% of the animated heroes' mean saturation, scaled offline by `tools/heroanim/saturation.mjs` (the B1 layers by
`layers.mjs`, as one scene); the other pictures stay as delivered.

The B1 layers (the Backgrounds folder; prepared by `tools/heroanim/layers.mjs` from the owner's five 852 × 1846 layers,
their boxes in `HomeLayersData.cs`, source record `tools/heroanim/SOURCE.md`):

| File | Layer | Slot |
|---|---|---|
| `home.jpg` | the garden: sky, arches, flowers and paving, without the fountain (852 × 1846, JPEG) | `bg.home` |
| `home-fountain-back.png` | the lotus fountain's basin, rim and lotus, behind the heroes | `bg.home.fountain_back` |
| `home-lotus.png` | the lotus, cut out of the fountain's back, drawn again over Bloom, who stands behind it | `bg.home.lotus` |
| `home-fountain-front.png` | the fountain's front stones and flowers, over the heroes' feet | `bg.home.fountain_front` |
| `home-shadow.png` | one soft shadow (the sheet's front left one), drawn under every hero | `bg.home.shadow` |
| `home-petals.png` | the pink petals, drifting down over the scene | `bg.home.petals` |

To change them, send the five full-size layers again (the garden opaque, the others transparent, in the same order),
then run `node layers.mjs <folder>` and `node check.mjs` in `tools/heroanim`. Without the fountain layers Home shows the
garden alone, with no heroes.

## C. Logo

Folder: `client/Assets/Bloomlings/Art/Brand/Resources/Brand/` (names `OwnerPictures.Logo` and `OwnerPictures.Tagline`).

| # | File | Size | What | Slot |
|---|---|---|---|---|
| C1 | `logo.png` | 1200 × 440, transparent | the reference's wooden "Bloomlings" letters with leaves and small flowers | `brand.wordmark` |
| C2 | `tagline.png` | 1000 × 80, transparent | "SMALL FRIENDS. BIG GARDENS." (optional, for later: neither build shows a tagline yet, so the slot is registered as not drawn) | `brand.tagline` |

## D. Owner pictures that replace drawn icons and leaves (owner's review, spec 005 FR-027)

The owner supplies these ("картинки я тебе дам, листочков, иконки бафов"). Until a file exists, the drawn version
shows; once it exists, both builds draw it in the drawn version's place (contracts/look.md §3.10: a booster icon
wherever the booster's icon shows, the leaves mirrored or turned as listed). Each is transparent, sRGB, drawn in the reference's style (soft light from the upper left, a darker outline,
gloss). Any size with the given proportions works: the session resizes delivered files to these sizes.

Folder `client/Assets/Bloomlings/Art/Icons/Resources/Icons/` (both builds embed or load it):

| # | File | Size | What | Slot |
|---|---|---|---|---|
| D1 | `booster-extra_slot.png` | 512 × 512 | Extra Slot: a white "+" on a blue disc; the icon only, no box (the game draws the cream box, the badge and the price); about 6% margin | `booster.extra_slot` |
| D2 | `booster-shuffle.png` | 512 × 512 | Shuffle: two turning arrows | `booster.shuffle` |
| D3 | `booster-return.png` | 512 × 512 | Return: a yellow arrow pointing back | `booster.return` |
| D4 | `booster-bloom_burst.png` | 512 × 512 | Bloom Burst: a pink flower | `booster.bloom_burst` |

Folder `client/Assets/Bloomlings/Art/Decor/Resources/Decor/`:

| # | File | Size | What | Slot |
|---|---|---|---|---|
| D5 | `ivy.png` | 512 × 512 | the clover/ivy cluster on the LEFT end of a wooden sign (the level sign, the Wardrobe and Store banners); the game mirrors it for the right end; the cluster's middle sits on the plank's end | `ui.sign.ivy` |
| D6 | `flowers.png` | 512 × 512 | the leaves with white flowers on the top-left of the "Level complete!" sign; mirrored for the other end | `ui.sign.flowers` |
| D7 | `button-leaves.png` | 256 × 256 | the small sprig with a white flower on the top-left corner of the Play and Next buttons; mirrored and turned for the bottom-right corner | `ui.deco.garden` |
| D8 | `logo-leaves.png` | 512 × 512 | only if the logo picture (C1) does not come: the leaf cluster at the left end of the drawn wordmark; mirrored for the right | `ui.logo.wood` |

The bottom menu's icons (spec FR-030, contracts/look.md §6.7): **delivered on 2026-10-04** by the owner in
`bloomlings_bottom_nav_icons_clean.zip` (1254 × 1254, trimmed to their alpha and fitted into 512 × 512 with a 20 px
margin; record `tools/artgen/models/owner-pictures.md`, notices `client/THIRD_PARTY_NOTICES.md`), in the Icons folder
`client/Assets/Bloomlings/Art/Icons/Resources/Icons/` (names `OwnerPictures.NavIcon(place)`). Each sits on the wooden bar
in its place (a square 0.86 of the plank's height) or, for the screen's own place, in the raised medallion (0.7 of its
disc); while a file is missing, the place's drawn glyph shows (`BottomNav.Fallback`).

| # | File | Size | What | Slot |
|---|---|---|---|---|
| D9 | `nav-shop.png` | 512 × 512 | the Shop: a cottage shop with a pink and white striped awning and flowers | `icon.nav.shop` |
| D10 | `nav-wardrobe.png` | 512 × 512 | the Wardrobe: a pink shirt with a white flower on a gold hanger | `icon.nav.wardrobe` |
| D11 | `nav-home.png` | 512 × 512 | Home: a cream cottage with a red roof, a wooden door and flowers | `icon.nav.home` |
| D12 | `nav-leaderboard.png` | 512 × 512 | the Leaderboard: a gold trophy with a star, flowers at its foot | `icon.nav.leaderboard` |
| D13 | `nav-collection.png` | 512 × 512 | the Collection: a purple album with a white flower and gold corners | `icon.nav.collection` |

Home's promo scenes (spec FR-032, contracts/look.md §6.4.1): **delivered on 2026-10-04** by the owner in
`bloomlings_daily_noads_anim_assets_fixed.zip` (separate transparent layers with an animation guide), in the Decor
folder `client/Assets/Bloomlings/Art/Decor/Resources/Decor/` (names `HomePromo.Pictures`; record
`tools/artgen/models/owner-pictures.md`, notices `client/THIRD_PARTY_NOTICES.md`). Each layer keeps its whole canvas
(the stands 1448 × 1086 fitted into 724 × 543, the others 1254 × 1254 into 512 × 512), so the kit's pivots and offsets,
in the owner's pixels, hold. The pack's red "!" badge (`05_daily_notification_badge.png`) is not used (the owner's note:
no notification mark). While a file is missing, the scene's label shows on a wooden sign.

| # | File | Size | What | Slot |
|---|---|---|---|---|
| D14 | `promo-noads-platform.png` | 724 × 543 | No Ads: the flowered stone stand with an empty wooden plaque (the game writes "No Ads") | `ui.promo.no_ads` |
| D15 | `promo-noads-sprig.png` | 512 × 512 | No Ads: Sprig pushing, palms to the right | `ui.promo.no_ads` |
| D16 | `promo-noads-sign.png` | 512 × 512 | No Ads: the wooden sign with the crossed-out "AD" | `ui.promo.no_ads` |
| D17 | `promo-noads-lotus.png` | 512 × 512 | No Ads: the glowing lotus with petals and sparkles that blooms in their place | `ui.promo.no_ads` |
| D18 | `promo-daily-platform.png` | 724 × 543 | Daily: the flowered stone stand with an empty wooden plaque (the game writes "Daily") | `ui.promo.daily` |
| D19 | `promo-daily-book.png` | 512 × 512 | Daily: the closed purple album with a flower | `ui.promo.daily` |
| D20 | `promo-daily-book-open.png` | 512 × 512 | Daily: the album open on its pages | `ui.promo.daily` |
| D21 | `promo-daily-stamp.png` | 512 × 512 | Daily: the pink flower stamp | `ui.promo.daily` |
| D22 | `promo-daily-sparkles.png` | 512 × 512 | Daily: the ring of petals and sparkles that bursts out after the stamp | `ui.promo.daily` |

## G. Gameplay characters and variant icons (owner's request, 2026-10-02)

The owner makes these next, in the style of their heroes (soft 3D volume, gloss, light from the upper left), as
transparent PNG files sent in the chat; the session cuts, sizes and wires them.

**G1–G8: the eight variant characters** — the small Bloomlings that walk from the arch to their tiles (spec 004
walkers) and fill the Bloomlings sheet. One per variant (not one per family): the two variants of a family differ in
**shape**, not only color (spec 001 FR-005, research D10). 512 × 512, full body, facing the viewer, happy, feet about
90% down, no ground or shadow. Optional: a second "step" pose for a walk cycle.

| # | Variant | Family | Look | Body color |
|---|---|---|---|---|
| G1 | Leaf | Sprig | Sprig with a fresh leaf, bright lime | `#99D323` |
| G2 | Moss | Sprig | a puffy moss-cushion Sprig, teal | `#0FB198` |
| G3 | Flower | Bloom | Bloom, hot pink, five petals | `#FF3B89` |
| G4 | Violet Bud | Bloom | a closed tulip bud with green sepals, purple | `#7F3CC4` |
| G5 | Water | Drop | a pointed drop, blue | `#3485E7` |
| G6 | Dew | Drop | a round dewdrop with a sparkle, light cyan | `#61DAE1` |
| G7 | Wood | Twig | a little stump with rings on its top, brown | `#9B4904` |
| G8 | Acorn | Twig | an acorn in its cap, amber | `#CF7F20` |

**G9–G16: the eight variant icons** — on pods, Waiting Slots and the jam card (the reference's "Target Variants"
strip). 512 × 512, the icon only (the game draws the colored tile), glossy with a dark outline like the booster icons,
in a shade of its tile color: Leaf a leaf with a vein, Moss a round moss cap, Flower a pink flower with a yellow
center, Violet Bud a purple bud, Water a pointed blue drop, Dew a light round droplet with a sparkle, Wood a stump or
log slice with rings, Acorn an acorn.

**G17–G24 (optional): simplified board gems** — the same eight shapes as flat gems with a thick dark outline and a
highlight, 256 × 256, for the small board tiles (40–60 px on a phone). Without them the drawn gems stay.

**Delivered (2026-10-02, `bloomlings_all_icons_assets.zip`):** G9–G24 and the currency lotus, in
`client/Assets/Bloomlings/Art/Icons/Resources/Icons/` (record `tools/artgen/models/owner-pictures.md`):
`variant-{leaf,moss,flower,bud,drop,dew,log,acorn}.png` (G9–G16, 512 × 512; `OwnerPictures.VariantIcon`, slots
`tile.icon.{id}`), `field-{…}.png` (G17–G24, 256 × 256; `OwnerPictures.FieldIcon`, slots `tile.gem.{id}`) and
`currency-lotus.png` (256 × 256; `OwnerPictures.CurrencyLotus`, slot `currency.petal`). Both builds draw the candy tile's
face and the icon over it (contracts/look.md §3.11): the field icon on the board and the finished picture, the detailed
one on pods, slots, the jam row, flights and every other sticker tile; the lotus wherever the Petals show.

**Later (optional):** the expansion variants (Vine, Berry, Mist, Bark) for G1–G24, and the mechanics' board objects
(stone obstacle, gate, fountain, chest, statue, bridge, key, lock, the "?" mystery tile), all drawn in code today.

## E. Optional painted UI (only if the code-drawn versions should be replaced)

All of these are drawn in code today (contracts/look.md) and need no picture: the wooden sign plank (9-slice,
512 × 128), the dark wooden pod frame (9-slice, 256 × 256), a stone block (128 × 64). (The eight variant icons and the
lotus came as G9–G24 and `currency-lotus.png`, above.)

## F. What the owner sends, in priority order

1. The four booster icons (D1–D4) and the leaves (D5–D7).
2. The Home garden without the heroes (B1) and the gameplay garden (B2; B3–B5 for the other themes).
3. The 3D heroes: idle (A1–A4), celebrating (A7) and the group (A6); any size with the 8:9 and 5:3 proportions.
4. The logo (C1).
5. Optionally: the win garden (B8), the Wardrobe garden (B7), the splash (B6), the faceless heroes (A5).
Files may be sent in the chat; the session places, resizes and records them (source record, notices, `adopt`).

Delivered by 2026-10-02: D1–D7; B1 (as the layered Home), B2–B5, B7 and B8; A1–A4, A6 and A7 (cut from the owner's
character sheet) and the animated heroes (A10); C1; G9–G24 and the lotus. On 2026-10-04: the calm backgrounds (B1's
garden, B2–B5, B7, B8) and the bottom menu's icons D9–D13. What is still open is in H.

## H. Still awaited from the owner, and open questions (2026-10-03)

1. ~~The Meshy plan of the four models~~: answered on 2026-10-03, a personal Meshy licence, so the models are the
   owner's and need no attribution (`tools/heroanim/SOURCE.md`).
2. G1–G8: the eight variant characters (walkers, the Bloomlings sheet); `tools/artgen` draws them until then.
3. A5: the faceless still heroes; `tools/artgen` draws the blanks until then.
4. Later: the expansion variants (Vine, Berry, Mist, Bark) for G1–G24, and the mechanics' board objects.
5. Optional: the splash picture (B6), the tagline (C2), modelled outfits (A8), tab heads (A9).
6. Optional, for the animated heroes:
   - faceless clips (the same idle and reaction without eyes and mouth), so a worn expression can draw the face
     instead of sitting on a badge beside it;
   - more reaction clips, for example a celebration for the win (Bloom's and Drop's files already carry a happy jump
     and a joyful dance, Drop's also a victory; unused for now);
   - a blink: the idle guide (`3.webp`) shows one, but the models' rig has no face bones, so the clips do not blink.
7. **The updated reference (`4.webp`) differs from what is built in four places**; the owner decided on 2026-10-03
   not to build them (they stay as built):
   - the win's reward: one chip per variant of the level (its candy tile and "+30" each) instead of the one "+N"
     Petals pill; it would change what the reward shows (spec 001 economy), not only its look;
   - the jam card: an acorn character (Twig) peeks over the card's top edge;
   - the Wardrobe: four outfit cards in a row, "Default", "Flower Hat", "Explorer" and "Sunny Scarf" (ours are three a
     page from the cosmetics catalog, which has no scarf);
   - the jam card's Bloom Burst button is purple (ours is blue, `button.blue`, as in `reference.jpg`).

   Its logo's tagline also reads "Small friends. Brighter gardens." (C2 and the idle guide say "Big gardens"); no
   build draws the tagline yet.
