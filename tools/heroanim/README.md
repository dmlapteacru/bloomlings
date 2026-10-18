# Accepted modular hero pipeline

All four production identities are **SprigHeroV2, BloomHeroV2, DropHeroV2 and TwigHeroV2**.
The owner accepted the quartet permanently. Previous hero meshes, experiments, animation
lab, archived motion frames and their bake scripts have been removed from the current tree.
Git history contains earlier iterations. Runtime art is 2D only; no model, rig or source
archive is included in the APK.

The owner's Blender file remains the only editable master. Current deliveries, all fitted
items, rigs, clips, UVs, source hashes and rebuild instructions are in:

- [Sprig V2](models/sprig-v2/README.md)
- [Bloom V2](models/bloom-v2/README.md)
- [Drop V2](models/drop-v2/README.md), including the final FBX handoff ZIP
- [Twig V2](models/twig-v2/README.md)

Current generation references, including bald modeling bases, are in `references/{family}-v2/`.
A bald base is never a stock game appearance. Stock Head is Leafy / Petals / NativeDrop /
Acorn. The shared round glasses have four family-specific material colors. Wardrobe combines
Head, Face, Body and Accessories; Shop has no Cosmetics tab.

## Prepare and bake

Node 22+, Blender 4.3+, Chromium and .NET 10 are used by the local tools. Install the pinned
JS dependencies with `npm ci` in this directory. Use the existing source ZIP / FBX deliveries:

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-sprig-v2.py
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-bloom-v2.py
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-v2.py
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-drop-v2-game.py
node tools/heroanim/bake.mjs
node tools/heroanim/check.mjs
```

`bake.mjs --only sprig|bloom|drop|twig` rebuilds one frontal family followed by the four
Home views. Each `bake-{family}-v2.mjs` can also run directly. Frontal sprites serve Wardrobe,
icons and victory. Home uses gentle three-quarter views toward the lotus: Sprig +30°,
Bloom 0°, Drop −25°, Twig −30°, in `home-heroes-look.json`. `bake-home-heroes.mjs --review`
writes the four stock views to ignored work files; normal baking covers every outfit recipe,
with depth occlusion and garment-aware scarf fits. Identical pixels / crop coordinates share
one PNG. See [Home review](models/home-heroes/README.md).

All builds write disposable caches to ignored `content/work/`. To refresh the profile and
fallback identities, run `bake-current-blanks.mjs`, `bake-twig-blank.mjs`, then
`python3 tools/heroanim/pack-current-heroes.py`. This publishes current-model avatars,
achievement medals, No Ads sprites, solo / blank / group fallback art and their source records.
Avatar and item IDs, ownership, saved selection and prices are stable.

The background pipeline is independent: `layers.mjs`, `saturation.mjs`, `backdrops.mjs`.
`check.mjs` verifies the current sources, all runtime sprite hashes and sizes, generated C#,
references, identity art, Home views and finished backgrounds. Run it before any art commit.
The separate action / skeletal animation feature remains planned; the old model-specific
lab has been removed. See [source and rights](SOURCE.md).

The [shared rig repair and six baseline actions](rigging/README.md) provide a checked
FBX handoff for all four accepted heroes and their equipment. This authoring package
imports into the owner's existing Blender master; it does not alter the pinned mobile
sprite library. [Download / reassemble the package](products/rig-fix-v1/README.md).
