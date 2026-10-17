# Segmented Tripo Twig: one base face, fitted acorn / cap

Owner delivery, 2026-10-17: three original Tripo ZIP exports, preserved byte-for-byte under `source/` with hashes in
`source-manifest.json`. They contain segmented FBXs, UVs and color / normal / roughness / metallic textures.

| Delivery | Source segments | Used |
|---|---:|---|
| `cute+cartoon+character+3d+model.zip` | 8 | All base parts. Head / ears: 0; mouth: 4, 7. |
| `pumpkin+head+3d+model.zip` | 3 | 1: acorn shell / rolled rim; 2: stem / leaf. Part 0 contains a different face and a generated body. |
| `cute+emoji+head+3d+model.zip` | 15 | 0: crown; 6: flower; 8: visor; 10: top button. Other parts form a different head / ears and an extra clothed body. |

All three source models are unrigged and have no actions. The extra bodies and faces generated for the two hats are
excluded from the assembly. Both completed head exports reuse **exactly the same** base head, ears, mouth, UV and
paint. Headwear is resized / translated and the cap tilted up using `../../twig-tripo-fit.json`; no base vertex is
moved and no source UV or texture image is repainted. Materials are nonmetallic without noisy generated normal maps;
a separate low-roughness eye region adds highlights using the same original color image. The generated paint still
needs a later texture pass (including gray eye whites and small facial / surface artifacts); it is not final art.

## Exchange with the owner's master

No editable `.blend` master is created. The output GLBs are exchange imports into the owner's existing Blender project:

- `TwigHero_Base.glb`: complete bald base, body and head as separate named groups.
- `TwigHero_AcornHead.glb`: identical base head / ears / mouth plus the fitted acorn hat.
- `TwigHero_FlowerCapHead.glb`: identical base head / ears / mouth plus the fitted cap.
- `TwigHero_AcornHat.glb`, `TwigHero_FlowerCapHat.glb`: fitted headwear alone, for retaining one shared head object.

All exports use the same world origin, units and fitted placement. `TwigHero_HeadPivot` is at Blender `(0, 0, 0.35)`.
The source character's feet stay at Z=0. No skeleton has been invented; the pivot is an attachment empty, not a bone.

In Blender: **File → Import → glTF 2.0**. Import the base. To use a whole-head variant, hide the base
`TwigHero_HeadPivot` hierarchy, then import ONE `*Head.glb`. To use headwear alone, keep the base head and import ONE
`*Hat.glb`; parent its hat group to the base head pivot with **Object (Keep Transform)**. Remove the redundant imported
empty roots afterwards. Do not scale each file independently or import both hats visibly at the same time. Texture
images are embedded in the GLBs. Later rigging / weights and animation fitting happen in the same master.

The owner subsequently requested this assembled Twig in the game. `bake-twig-tripo.mjs` now publishes five small
2D images (bare / acorn / cap plus two Shop icons) from `TwigHero_Review.glb`. The game routes Twig through the
shared equipment renderer on Home, Wardrobe and victory. Both hats are free Twig-only items; saved selection
persists across those screens. Profile avatar pictures remain separately selected. No 3D exports or source ZIPs enter the APK.
The static source pose uses the existing 2D breathing; skeletal actions are deferred.

## Rebuild and verify

From repository root, with Blender 4.3 and the existing `tools/heroanim` Node dependencies:

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/twig-tripo-study.py
node tools/heroanim/twig-tripo-check.mjs
node tools/heroanim/twig-tripo-review.mjs
node tools/heroanim/check.mjs
```

Derived GLBs / reports: `content/work/twig-tripo-study/build/`. Review renders: `content/work/twig-tripo-study/review/`.
Each render uses the same camera and body placement. `twig-tripo-check.mjs` checks actual exported head positions,
normals, UVs, triangle indices, embedded base-color images, world transforms and neck pivot against the base GLB.
Original archive hashes and unchanged source geometry / UV signatures are checked as well. Actual review and the
Blender import package: `output/twig-tripo-study/`.

## Publish the runtime images

After rebuilding and checking the fitted GLBs above:

```sh
node tools/heroanim/bake-twig-tripo.mjs
node tools/heroanim/check.mjs
```

The active bake records source hashes in `../../twig-gear.json`, generates `TwigGearData.cs` and updates the five
`twigmod-*` RGBA8 PNGs plus their asset manifest. `bake-twig-gear.mjs` belongs to the retired earlier FBX pilot;
do not use it to publish the current Tripo hero. Current game captures: `output/twig-tripo-in-game/`.
