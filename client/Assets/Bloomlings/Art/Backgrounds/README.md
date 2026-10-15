# Garden backgrounds

The 2026-10-14 redesign uses the Shop's shaded teal/jade palette with mint atmospheric depth, warm stone and sparse edge flowers. Each page has its own location and a quiet center for UI:

| Resource name | Scene |
|---|---|
| `home` | Open courtyard, behind the existing separate fountain and hero layers |
| `wardrobe` | Wooden dressing pavilion |
| `profile` | Pond terrace, shared by Profile and Edit Profile |
| `leaderboard` | Ceremonial avenue |
| `collection` | Outdoor botanical archive, shared by gallery and detail |
| `gameplay-daylight` | Shaded clearing |
| `gameplay-pond` | Clearing with reeds and lilies at the edges |
| `gameplay-orchard` | Clearing with fruit-tree edges |
| `gameplay-moonlit` | Deeper teal clearing with silver-mint illumination |
| `gameplay-pear` | Pear Garden; fruit at the edges and a quiet teal center |
| `gameplay-citrus` | Citrus Grove; fruit at the edges and a quiet teal center |
| `gameplay-plum` | Plum Garden; fruit at the edges and a quiet teal center |
| `win` | Light clearing with a low stone dais; standing point at 0.615 image height |

The Shop retains its existing `Art/Shop/Resources/Shop/background.png`. Splash reuses the Home courtyard beneath the lotus loader, clipped outside the opening iris; level transitions use a plain matching teal cover. Popups retain their underlying page scene and dim it.

Opaque backgrounds are 852 × 1846 JPEGs. Since 2026-10-15 the Home courtyard and fountain layers share lighter ivory/mint/jade artwork with coherent focus; their original dimensions, layer coordinates, heroes and shadows remain unchanged. Home bypasses the old additional blur and dim finish; other scenes keep it. Unity loads `Resources/Backgrounds/{name}`; Android and Skia use `bg/{name}` through `OwnerPictures`. Both project asset globs include every JPEG and PNG here automatically.

The authoritative production sources are in `tools/heroanim/backgrounds/`. To rebuild their existing slight blur and dim finish:

```sh
cd tools/heroanim
npm ci --ignore-scripts --no-audit --no-fund
cd ../..
node tools/heroanim/backdrops.mjs
node tools/heroanim/check.mjs
```

This verifies all source/output hashes, retained Home layers and hero frames. Do not run the legacy `layers.mjs` against an older Home layer pack when only updating backgrounds: it would replace the approved courtyard. The layer manifest records the newer source while preserving the original canvas and foreground placements.

Generation provenance and original output hashes: `tools/artgen/models/garden-backgrounds-2026-10-14.md` and `tools/artgen/models/home-fountain-2026-10-15.md`; imported-resource records: `client/THIRD_PARTY_NOTICES.md`. Source generator PNGs and concept sheets are review artifacts and are not bundled in the game.

Additional gameplay scenes: `gameplay-fern` (Fern Glade, L250) and `gameplay-willow` (Willow Brook, L300). The nine themes rotate every 50 levels from L100: Pear Garden joins at L350, Citrus Grove at L400, Plum Garden at L450, and Daylight returns at L500. All additional scenes use the existing source/finish pipeline and quiet Shop palette.

Fruit garden source records: `tools/artgen/models/fruit-backgrounds-2026-10-15.md`. The same JPEG dimensions and finishing apply.
