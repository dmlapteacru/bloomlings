# Owner-supplied eyewear, 2026-10-18

Six separate glasses FBXs from the owner’s Blender master. Original files are retained
unchanged under `source/`; `source-manifest.json` records their SHA-256 and explicit fit.
There is no second maintained Blender scene. These FBXs are authoring sources and are
outside Unity Resources, so they are not packaged in the APK.

| Hero | Owner file | Wardrobe Face ID |
|---|---|---|
| Sprig | SprigHeroV2_GlassesRoundGreen.fbx | `glasses.sprig_round` |
| Sprig | SprigHeroV2_GlassesSquare.fbx | `glasses.sprig_square` |
| Bloom | BloomHeroV2_GlassesPink.fbx | `glasses.bloom_round` |
| Bloom | BloomHeroV2_GlassesHeart.fbx | `glasses.bloom_heart` |
| Twig | TwigHeroV2_GlassesBrown.fbx | `glasses.twig_round` |
| Twig | TwigHeroV2_GlassesOctagon.fbx | `glasses.twig_octagon` |

## Inspection and fitting

Each delivery contains one eyewear mesh and one armature. Every vertex is weighted
100% to `Head`, with no body-bone influence. The prepared rest mesh retains its original
world coordinates and UVs: `build-report.json` records zero geometry error for all six.
Front, Home three-quarter, and both side views were rendered with the accepted hero.
Existing alignment fits: all declared translations are zero and scales are one. No
geometry correction or reshaping was necessary. The original frames have open lenses;
no opaque lens surface or embedded face is added. Delivered albedos are retained, using
the same bright material finish as the heroes. Review images are in `review/`.

The three old round IDs now show the new deliveries, preserving saved Face selections.
The alternate shapes add three IDs. All six are free testing items in Wardrobe; Drop’s
existing glasses remain unchanged. Head, Body and Accessories selections remain separate.
The standalone card icons show only glasses, while fitting changes only the main stand.

## Rebuild

Prepare the accepted Sprig, Bloom and Twig caches as described in the parent README,
then run from the repository root:

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-eyewear-v1.py
node tools/heroanim/bake-eyewear-v1.mjs --review
node tools/heroanim/bake-eyewear-v1.mjs
node tools/heroanim/check.mjs
```

The main `bake.mjs` also runs this extension after the legacy family and Home bakes.
It adds cropped RGBA layers and standalone icons, without rebaking the accepted base,
Head, garment or scarf art. Depth-only occluders include every selected hero part;
scarf garment morphs remain active. All supported Head × Body × scarf combinations
are mapped for front and Home, with deduplication of identical pixels and crop offsets.
`eyewear-v1.json` pins inputs, generated C# tables and all output PNG hashes.
The app uses these tables through `EyewearArt`; the prior static family art remains
an input to this extension rather than an alternative selectable glasses item.

## Game review

[Rendered Wardrobe and Home screens](review/game-board.png) come from fixtures 171–177
at 1080×2340. Each fixture taps a real Face card, visits Home, checks the angled sprite,
and returns to the frontal stand. Alternate-shape fixtures also wear headwear / hair,
clothes and a scarf. Run with:

```sh
dotnet run --project playtest/preview -- --frames 171,172,173,174,175,176,177 --shape 19.5x9 --out content/work/eyewear-v1/game-screens
```

Publication contains 41 unique cropped PNGs (615,733 bytes), covering 912 outfit/view
mappings. The unchanged FBXs total about 25 MB and remain authoring inputs only.
