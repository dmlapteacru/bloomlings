# Simplified Tripo heroes — current in-game model trial

Owner delivery, 2026-10-17: four FBX / texture ZIPs generated from the approved simplified character references.
The originals are renamed without modifying bytes under `source/`; `source-manifest.json` records hashes and
original names. Reference illustrations are preserved in `../../references/simple-heroes/`.

| Game family | Original archive | Source triangles |
|---|---|---:|
| Bloom | pink flower character | 96,570 |
| Drop | blue droplet | 96,594 |
| Twig | acorn character (1) | 98,931 |
| Sprig | cute plant character | 95,902 |

All four have a single textured mesh, UVs and **no rig / actions**. The base color is retained. On the derived copy,
noisy normal maps and generated metallic / roughness maps are disconnected; metallic is zero and roughness 0.68.
A single uniform root scale fits each complete model to unit height; it does not reshape heads or bodies.
The earlier Twig X=0.88 adjustment does not apply to this new source. The owner's master remains the authoring source;
no editable second `.blend` is made.

## Offline preparation and rendering

From repository root (Blender 4.3.2, Node with existing `tools/heroanim` dependencies):

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-simple-heroes.py
node tools/heroanim/bake-simple-heroes.mjs
node tools/heroanim/check.mjs
```

Prepared embedded-texture GLBs live only in ignored `content/work/simple-tripo/build/`. The build report records source
bounds, uniform scales, rig absence, polygon counts and hashes. The bake writes four cropped RGBA8 PNGs into
`Art/Heroes/Resources/HeroMotion/simplehero-*.png`, generates `SimpleHeroData.cs`, and records all inputs / outputs in
`../../simple-heroes.json`. Front camera, original painted light plus a restrained 25% diffuse contribution (no PBR / specular pass), fixed
448×504 logical cell, feet at 90%, 84% height
fill, 3× supersampling with alpha-safe linear-light reduction. Total runtime bytes: 411,390.

## In-game behavior

The four bases route through the shared `HeroGearArt` renderer on Home / splash, Wardrobe / Shop previews and the
existing Sprig / Twig victory and milestone hosts. Static source poses use only the existing subtle 2D breathing;
new skeletal animations are deferred. Core gameplay tiles / workers and independent profile avatar pictures retain
their art and selection.

The previous model-specific Sprig / Twig cap, glasses and backpack entries remain in save / service data and their
archived art is retained. Their `metaOnly` parts are withheld from this trial's Shop / Wardrobe shelves and stripped
from the displayed outfit, without changing the saved selection. Generic existing 2D cosmetics remain supported.
These new models do not pretend to have fitted replacement headwear. Refit accessories and author actions later.

The APK carries only the four small 2D images, never source ZIPs, FBXs or GLBs. Full-color Android decoding and
Unity's uncompressed sprite import retain the gradients. No APK is built here. Current game captures:
`output/simple-heroes-in-game/`.

## Light / material correction from the Tripo viewer comparison

The owner noted darker faces than in the Tripo viewer. Exported GLBs contained no AO layer; source albedos already
contain illumination, while the initial physical render applied another diffuse / specular shading pass. The final
bake uses a 75% original-albedo contribution plus 25% soft white diffuse light, without physical specular / metallic /
normal shading. This keeps face and eye whites clean while retaining gentle volume. No albedo repaint, blanket
exposure gain, UI tint or geometry change is used. Reproducible values are in `simple-heroes.json`'s render recipe.
