# Garment repairs and Drop additions

The owner's Blender project remains the maintained source. Original delivered FBXs
and ZIPs are unchanged. This pass repairs **19 garments** (Sprig 4, Bloom 5, Twig 5,
Drop 5) and fits seven additional Drop Head items. The same repaired meshes are
exported to the FBX handoff and used for game sprites.

Repairs operate on geometry: remove tiny reconstruction scraps; trim ragged upper
cuts; remove narrowly selected donor skin fragments in the Drop shirts/overalls
and Sprig vest; relax a small band at open edges; fill small accidental tears;
and add a 1.5 mm inward fabric return. Neck, sleeve, waist and leg openings remain
open. Buttons, pockets and original albedo/UV layouts on retained faces are kept.
New rim/cap faces inherit adjacent UVs and normalized skin weights. Vertex/face
counts deliberately change. This is not a new rig/animation pass.

The source files and all fit axes are pinned in `fit.json`. Each family report
records mesh counts, trimming, repairs and an actual FBX reimport error below
1e-5 m. Accepted base skin/head world vertices are checked unchanged. The source
FBX skeletons remain in the final handoff; new Drop parts use the accepted Drop
skeleton. New Head items are rigidly weighted to Head; the two light tops reuse
weights sampled from the accepted Drop coat. No `.blend` is produced or maintained.

## Rebuild

Dependencies: Blender 4.3.2 and the existing Node/Chromium hero art toolchain.
Prepare the accepted family caches first using their existing documented scripts.
Then run, from the repository root, **one family at a time**:

```bash
for family in drop sprig twig bloom; do
  blender -b --factory-startup -t 2 --python-exit-code 1 \
    --python tools/heroanim/prepare-wardrobe-finish-v1.py -- --family "$family"
done
blender -b --factory-startup -t 2 --python-exit-code 1 \
  --python tools/heroanim/validate-wardrobe-finish-v1.py
python tools/heroanim/package-wardrobe-finish-v1.py
for family in drop sprig twig bloom; do
  node tools/heroanim/bake-wardrobe-finish-v1.mjs --family "$family" --review
  node tools/heroanim/bake-wardrobe-finish-v1.mjs --family "$family"
done
node tools/heroanim/check.mjs
python tools/heroanim/review-wardrobe-finish-v1.py
```

`content/work/wardrobe-finish-v1/` is disposable. Preparation replaces only its
family's entries in the one FBX ZIP. The baker merges family-local tables after
all four families have been baked. Frontal and inward-facing Home views use the
accepted camera, palette and supersampling. Garment/scarf/eyewear depth masks are
regenerated for every affected outfit. Existing IDs, profile choices and level
pictures are retained.

## Blender import

Extract `tools/heroanim/products/Wardrobe_Clothes_Cleaned_Drop_Additions.zip`.
Import the matching family FBXs into a temporary collection in the owner's master.
Replace the older garment meshes by matching filename; do not keep both visible.
FBXs have packed textures and the matching existing skeleton. When merging into
one rig, redirect the Armature modifier and preserve world/rest transforms.
Drop Head items fit the rounded `Base_Head`; hide `NativeDrop` when using them.
The default game Head is still the native droplet, never the bald authoring base.
