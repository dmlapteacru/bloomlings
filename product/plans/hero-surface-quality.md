# Hero surface quality

**Current status (2026-10-17):** the owner prefers the original Twig. Its original model / animation frames are active
again. The quality pilot below is archived study material, not the current in-game Twig. Use the identity references
in `tools/heroanim/references/twig-legacy/` for the next bald base / head variants.

Owner direction (2026-10-17): cute, smooth, vivid heroes like the approved pictures, with sharp eyes and clean broad
color transitions. No photographic bark, pores, grit or artificial sharpening halos. Short bodies and approved anatomy
remain. This is a surface / rendering pass, not a new character design.

## Production approach

The owner's Blender scene remains the only modeling master. The assistant supplies textures, material recipes and
offline rendering; changes to derived render inputs are reproducible from exports. Original owner FBXs remain intact.
No runtime 3D, model loading or heavy PBR textures are added to the APK.

1. **Surface first.** AI meshes can contain tiny sculpted wrinkles, dents and irregular outlines even without a texture.
   Smooth shading removes faceting; filtered render normals reduce small surface noise. Deep grooves, bad silhouettes
   and intersections need actual mesh cleanup in the master. Increasing image resolution cannot remove them.
2. **Stable UVs.** Before final animated, all-angle paint, unwrap the accepted mesh without overlaps. Allocate generous
   space to the face and put seams behind the head / under limbs. Keep that UV layout when exporting new motions.
   Author a 2048² body base-color map; 512–1024² usually suffices for a small accessory. Inspect the actual screen-size
   result before increasing either. Avoid stretching, blurry packed eyes and JPEG artifacts on the face.
3. **Clean color.** Paint broad, saturated local colors and gentle gradients. Keep lighting, cast shadows and bright
   specular eye reflections out of final base-color maps. Treat eyes, sclera, mouth, skin, woody limbs and accessories
   as separate material regions. Do not bake gritty wood grain onto the face or introduce lips / makeup.
4. **Material separation.** Use nonmetallic materials. As an initial Twig recipe: skin roughness 0.60, accessories 0.50,
   eyes 0.28. These are starting values for the studio lights, not universal constants. No skin roughness noise or dense
   normal-map detail; preserve only the character's broad intentional forms. Soft highlights should describe volume,
   with the strongest clean highlights on the eyes.
5. **Consistent light.** Neutral studio environment, broad key, softer fill and modest rim. Preserve cream / gold facial
   color and readable undersides. Check for clipped forehead / sclera highlights, muddy shadows and doubled highlights
   already painted into a provisional reference texture.
6. **Clean offline output.** Render above delivery resolution and reduce linear-light, premultiplied-alpha samples.
   Keep full color for the few current static outfit states. Do not apply a shared 256-color palette or dithering there.
   Check transparent edges on the actual game backdrop. For hundreds of animated frames, evaluate compression and APK
   / memory cost separately before applying this policy to the whole library.
7. **Acceptance.** Review actual mesh renders and Home / Shop / Wardrobe / Win at phone resolution; an image-generation
   illustration is an art reference, not evidence that the model renders that way. Check source hashes, clipping,
   packaging, decoded pixels, cold navigation and saved headwear selection.

## Twig pilot delivered

- `tools/heroanim/twig-materials.json` records normal filtering, roughness and lighting. Twelve adjacency-filter passes
  smooth only render normals, with an angular feature threshold and no blending between disconnected surfaces. Mesh
  coordinates, weights and topology stay as before this quality pass; the already approved derived acorn fit remains.
- The renderer uses a neutral studio environment, supersampling 3× (1344×1512 hero cell), then reduces to the existing
  448×504 logical delivery cell. Isolated icons render at 960×960 before reduction to 320×320. No sharpening is added.
- Five runtime images retain RGBA8 colors and alpha without palette quantization, dithering or Unity block compression.
  Both existing painters support direct bitmap fallback; Android dimension lookup and the opt-in asset verification
  now support it as well. PNG decoding remains cached and participates in the existing staged navigation loader.
- Payload: **124,390 → 479,663 bytes** for the five PNGs (**+355,273 bytes**, about 0.34 MiB before APK compression).
  Dimensions stay the same. All five decoded RGBA images together contain **1,872,960 bytes** of pixels (1.79 MiB),
  before platform overhead. They are loaded as needed. The legacy animation libraries retain their existing formats.
- `output/twig-quality/comparison.html` compares actual shipped images with the preceding revision, with a choice of
  bare / acorn / cap. `before-after.png` shows the default acorn comparison. Both use the delivered resolution.

### Remaining work before claiming final reference quality

Twig's FBXs arrived without UVs or texture images. The current front-color projection is provisional, contains some
painted lighting and is intended for the current front pose. Expanding its front coverage avoids a visible material
boundary on the cheek; it does not turn it into an all-angle, deformation-tested UV atlas. Finish the stable unwrap
and clean all-angle base color before the upcoming animation feature. Some dents / irregular contours remain in the
source geometry and need selective cleanup in the owner's master. Other heroes have not received this new pass yet.

Rebuild / review without changing shipped assets:

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-gear.py
node tools/heroanim/bake-twig-gear.mjs --review
```

Remove `--review` to publish the five states, metadata and manifests; then run `node tools/heroanim/check.mjs` and the
focused game previews. High-resolution render masters stay in ignored `content/work/twig-quality-review/`.
