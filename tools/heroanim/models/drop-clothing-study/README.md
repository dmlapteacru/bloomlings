# Drop overalls — offline fitting study

The owner supplied two segmented, unrigged Tripo exports to test fitting generated clothing onto a common base.
The unchanged original ZIPs and their delivery names / hashes are in `source-manifest.json`.

- **Base:** `cute+blue+character+3d+model (2).zip`, seven meshes including the rounded bald head and tail.
- **Donor:** `blue+cartoon+character+3d+model (3).zip`; only `tripo_part_21` (overalls + shirt) is reused.
  The donor's smaller head and taller torso / limbs are discarded.
- `products/Drop_Overalls_Fitted.fbx`: one separate garment mesh, in the original base world coordinates,
  with its original albedo embedded. Import this file into the owner's Blender project.
- `products/Preview_FullCharacter.glb`: disposable full-base fitting preview, not another editable master.
- `products/Before_After.png`: the SAME unchanged base, with no clothes / original donor clothing / fitted clothing.
- `products/fit-report.json`: unchanged base / UV assertions, fitting parameters, FBX reimport and image embedding checks.
- `manifest.json`: source, script and delivered output hashes; checked by `node tools/heroanim/check.mjs`.

## Rebuild and inspect

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/fit-drop-overalls.py
node tools/heroanim/review-drop-clothing.mjs a.glb unfitted.glb fitted.glb garment.glb
```

These commands write to ignored `content/work/drop-clothing-fit/`. FBX output is in its `delivery/` subfolder.
The FBX writer includes creation timestamps, so a rebuild need not have the same byte hash. Reimport assertions
check the actual world-space geometry, mesh count, UV coordinates and embedded image bytes instead.

## Fit and validation

The fitting leaves all seven base meshes and their UV coordinates exactly unchanged. Only the garment is deformed:
80% original height, 96% width and a small forward offset, then local clearance against the base torso / arms / legs.
The displacement field is smoothed without smoothing the authored garment itself; a final clearance pass preserves
the original shirt, pockets, buttons and garment UV layout. Head and tail joint interfaces are excluded from this
clearance proxy. The original tail remains visible behind the overalls.

The result retains 19,311 garment vertices and 38,239 polygons. Reimported FBX has exactly one mesh and the same
UV coordinates; maximum world-coordinate difference is below 0.000002 source units. The embedded albedo is verified
byte-for-byte against the donor texture. Preview shading uses the same soft albedo-led finish as the game.

This is a **static fitting study**, not rigging or production retopology. The delivered garment's existing cut edges
remain (441 boundary edges, 442 non-manifold edges); the script does not claim a watertight simulation garment or
an animation-tested skin. Both source models are unrigged. Weight transfer and bending tests belong to the owner's
rigged common base when available. No second maintained `.blend` is created.

Nothing from this study is added to game Resources, Wardrobe, APKs or level content. The current in-game Drop is
unchanged. The separate rounded bald base is still an authoring scaffold, not a selected stock gameplay appearance.
