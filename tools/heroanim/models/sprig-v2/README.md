# SprigHeroV2 — owner-aligned modular preview

The two original deliveries in `source/` are retained unchanged and pinned in `source-manifest.json`.
Together they contain the common bald base, 13 fitted items, a rig and Idle / Joy / Walk / Wave actions.
The owner's Blender project remains the sole editable master. No second maintained `.blend` is created here.

## Rebuild

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-sprig-v2.py
node tools/heroanim/bake-sprig-v2.mjs
node tools/heroanim/check.mjs
```

Preparation safely extracts both ZIPs, imports rest coordinates, verifies unchanged world vertices and UVs and
preserves scarf shape keys. Only the derived bake copy loses its rig. The animation FBX remains unmodified for
the later animation / action-selection feature. Current meta hosts use the static pose and subtle 2D breathing.
Delivered albedo is preserved with 75% painted color / 25% soft diffuse lighting, matching Twig's clean material finish.

## Fitted items

| Slot | Source part | Stable game ID |
| --- | --- | --- |
| Head — stock | HairLeafy | `hat.sprig_leafy` |
| Head | HairRosette | `hat.sprig_rosette` |
| Head | HairSwirl | `hat.sprig_swirl` |
| Head | BucketHat | `hat.sprig_panama` |
| Head | Cap | `hat.sprig_daisy` |
| Head | Beret | `hat.sprig_beret` |
| Head | EarmuffHair | `hat.sprig_earmuffs` |
| Head | XmasHat | `hat.sprig_xmas` |
| Face | Glasses | `glasses.sprig_round` |
| Body | Jacket | `clothes.sprig_jacket` |
| Body | Coat | `clothes.sprig_coat` |
| Body | Overalls | `clothes.sprig_overalls` |
| Accessories | Scarf | `scarf.sprig_knit` |

All 13 are free for this preview and listed in Wardrobe by slot. Stock Sprig always wears HairLeafy; removing
headwear restores it. The bald mesh is a modeling scaffold only. Existing Daisy Cap / Garden Glasses save IDs now
resolve to their newly fitted parts. The previous unfitted backpack remains in old saves but is withheld from the shelf.
Face, Body and Accessories persist independently when Head changes or its category's Default is selected.

## Runtime and masking

The source geometry intersects around earmuffs / neck / collars. The bake therefore conditions garment masks on Head,
scarf masks on Head + Body, and eyewear masks on Head + Body + scarf. The scarf's `Over_Jacket`, `Over_Coat` and
`Over_Overalls` morphs activate with the corresponding garment. This avoids treating the scarf as a flat overlay.

157 cropped RGBA8 PNGs (3,288,169 bytes) cover all 144 saved combinations / 128 distinct looks, including the
modeling-only bald state. A selected outfit draws four small layers at most; no full-outfit atlas is loaded.
Sources, reference PNGs, rig, ZIPs and the derived GLB stay outside application Resources and APKs.
The owner permanently accepted Sprig V2. Previous meshes, trial sprites and archived motion PNGs are removed; earlier versions remain in Git history.
No APK or levels are generated here. Profile avatar IDs retain their ownership and price; their art now depicts the accepted model. Level mosaics are not regenerated.

Additional fitted Head / Body items: [owner archive additions](../wardrobe-additions-v1/README.md).
The separate extension preserves the accepted original bake and its recipe indices.
