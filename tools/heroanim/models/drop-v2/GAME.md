# Drop V2 — accepted production hero

The game now uses the same fitted Drop as the FBX handoff. All four current heroes can be compared on Home and dressed in Wardrobe. Normal APK builds use committed PNGs and need no Blender / Tripo step.

- 9 Head choices, 1 Face, 3 Body, 2 Accessories; every Drop item is free during this review.
- Default is the complete native droplet head. Other heads use the shared rounded face; the bald modeling scaffold is never exposed.
- The existing round glasses are reused. Opaque frame colors: Twig warm brown, Sprig leaf green, Bloom petal pink, Drop cream. There is no painted lens image.
- Shared four-layer rendering preserves outfits across Home and Wardrobe; shelves preview only their own item on the stock hero.
- 240 saved slot selections collapse to 216 distinct complete looks because Default and explicit NativeDrop show the same head.
- 231 cropped RGBA8 PNGs, 4,599,103 bytes; previous Drop runtime pictures were 7,077,657 bytes. The previous 133 pictures, legacy meshes and trial model sources are removed. The owner permanently accepted the complete V2 quartet.

These are 2D game pictures with the established breathing / reaction presentation, like the other V2 heroes. The separate five skeletal actions remain in the FBX handoff; an action picker / outfit animation library is a later feature.

## Rebuild art

From repository root, with Blender 4.3.2 and `npm ci --prefix tools/heroanim` available:

```bash
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-drop-v2-game.py
node tools/heroanim/bake-drop-v2.mjs
node tools/heroanim/bake-sprig-v2.mjs --glasses-only
node tools/heroanim/bake-bloom-v2.mjs --glasses-only
python tools/heroanim/pack-twig-identity.py
node tools/heroanim/check.mjs
```

The eyewear-only option is for material-only edits with unchanged camera and fitted geometry. It retains unrelated sprite pixels and the complete metadata order. Previously identical depth masks share a recolored render. A geometry / camera edit requires a full family bake.

`prepare-drop-v2-game.py` verifies the final handoff ZIP / each FBX hash, removes rigs from the disposable bake copy and applies one shared front rotation. UVs and fitted source geometry are verified. Head / clothing / scarf / eyewear layers use actual depth masks. The baker caches only masks with matching intersecting occluders, retaining all logical layer keys for saves.

## Verification

Client tests exercise all saved combinations, family restrictions, per-slot reset, frame lookup and actual file availability. Preview fixtures 155–157 show all four heroes together; 158–161 show Drop's four Wardrobe categories; 162 uses real category/item taps to check unchanged shelf pixels and a changing main stand. Fixture 142 keeps Cosmetics absent from Shop. Previews / tests compile the same client design and Wardrobe code used by the game. No APK or level catalog is generated here.
