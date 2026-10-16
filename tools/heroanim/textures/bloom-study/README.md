# Bloom texture study — 2026-10-16

Owner request: make the in-game flower less blurry and closer to the clean character illustrations.
The owner approved this variant on 2026-10-16 and requested installing it in the game. The production bake now
replaces Bloom’s 132 existing frames with this appearance. Source textures and review artifacts remain outside APK assets.

The source is the owner's `../../models/heroes.glb`, Bloom's existing 5,551-vertex mesh, original UVs,
34-joint skeleton and original Breathing/SmallJump clips. No vertex, skin weight, skeleton, animation or source GLB is changed.

- `front-original.png`: Blender orthographic, unlit albedo render of that exact posed mesh.
- `front-clean.png`: imagegen cleanup of that reference, preserving pose, silhouette and feature placement. Generated
  for this project on 2026-10-16. The prompt is in `cleanup.prompt.txt`. This is a paint reference, not the game sprite.
- `basecolor.png`: actual 2048×2048 UV texture, baked back onto the original surface with Blender. Projection is frozen
  into the original UVs, so it follows the existing animation. Original texture remains on the back/hidden surfaces;
  smooth corner-normal and visibility weights blend the frontal cleanup without changing geometry.
- `look.json`: candidate color/lighting/finish and 0.35 dithering strength. The production pipeline keeps its existing
  full-strength dithering by default. Neutral illumination avoids compensating the brighter paint with extra glow.

Direct imagegen editing of the fragmented UV atlas was rejected: even small island shifts cause visible seams.
Projection uses the original mesh/camera and bakes into the original atlas instead. The new paint still inherits the
source model's eye/lip geometry and some baked shadows. Rear surfaces, large turns and extreme facial changes still need further painting/geometry checks.
The installed pass targets the game’s existing frontal meta-screen clips.

## Reproduce

From the repo root, with `tools/heroanim` dependencies installed:

```bash
blender --background --factory-startup --python tools/heroanim/bloom-projection.py -- prepare
blender --background --factory-startup --python tools/heroanim/bloom-projection.py -- bake tools/heroanim/textures/bloom-study/front-clean.png
# Compare the freshly baked content/work/bloom-texture/projection/bloom-clean-basecolor.png to basecolor.png.
HERO_LAB_CHROMIUM=/usr/bin/chromium node tools/heroanim/review-bloom.mjs
HERO_LAB_CHROMIUM=/usr/bin/chromium node tools/heroanim/review-bloom.mjs --angles
node tools/heroanim/check.mjs
```

`HERO_LAB_CHROMIUM` is optional when Playwright's Chromium is installed. The review uses the real production resolution
448×504, supersampling 2.5, 24 fps and a shared 256-color palette. Before frames come from the pinned pre-adoption production
manifest with hash verification; after frames are rendered on the same mesh and clips with the replacement albedo.
The historical baseline is pinned by `baseline.json` to the pre-adoption Git commit (it must exist locally).
Frame count and head/top anchors must agree. `--quick` samples at 1 fps; `--angles` samples at −25°, 0°, +25°.
All rendered frames go under ignored `content/work/bloom-texture/review`, never into Unity Resources or playtest assets.

PNG RGBA export was also measured as an alternative to palette grain; a single-frame lossless WebP trial was larger,
so this candidate retains indexed frames and reduces dithering. Same pixel dimensions mean the same decoded memory
budget. All 132 cropped candidates were measured with the actual lossless asset packer: 5,716,494 bytes vs
6,612,519 bytes for the old frames (13.55% less). No APK was built here.

Review images/video and measurements: `../../../../output/bloom-texture/`.

## Production bake

```bash
HERO_ANIM_CHROMIUM=/usr/bin/chromium node tools/heroanim/bake.mjs --only bloom
node tools/heroanim/check.mjs
```

`heroes.json` references this texture/recipe and `paletteSpace: "cell"`. The bake quantizes the full cell before
cropping to preserve the approved pixels exactly. Its manifest records the source texture/recipe hashes.
All other heroes keep their previous source, palette mode, dithering, lighting and files.
