# BloomHeroV2 — owner-aligned modular preview

Both owner-delivered original ZIPs remain unchanged in `source/`, pinned by `source-manifest.json`.
They include a common bald base, 15 fitted wearables, a rig and Idle / Joy / Walk / Wave actions.
The owner's Blender project remains the sole editable master. No second maintained `.blend` is created here.

## Rebuild

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-bloom-v2.py
node tools/heroanim/bake-bloom-v2.mjs
node tools/heroanim/check.mjs
```

Preparation validates archive hashes and extraction paths, imports rest coordinates, verifies unchanged world
vertices and UVs and preserves scarf shape keys. Only the derived offline bake copy loses its rig.
The animation FBX stays intact for the later animation / action-selection feature. Current meta hosts use the
same pose with their existing subtle 2D breathing / reaction timing; skeletal actions are not integrated here.
Delivered albedo is preserved with 75% painted color / 25% soft diffuse lighting, matching Twig and Sprig.

## Fitted items

| Slot | Source part | Stable game ID |
| --- | --- | --- |
| Head — stock | HairPetals | `hat.bloom_petals` |
| Head | HairBob | `hat.bloom_bob` |
| Head | HairPeach | `hat.bloom_peach` |
| Head | HairRose | `hat.bloom_rose` |
| Head | BucketHat | `hat.bloom_panama` |
| Head | Cap | `hat.bloom_cap` |
| Head | Beret | `hat.bloom_beret` |
| Head | EarmuffHair | `hat.bloom_earmuffs` |
| Head | XmasHat | `hat.bloom_xmas` |
| Face | Glasses | `glasses.bloom_round` |
| Body | Jacket | `clothes.bloom_jacket` |
| Body | Coat | `clothes.bloom_coat` |
| Body | Overalls | `clothes.bloom_overalls` |
| Accessories | ScarfKnit | `scarf.bloom_knit` |
| Accessories | ScarfStripe | `scarf.bloom_stripe` |

All 15 are free for this preview and listed in Wardrobe only. Stock Bloom wears HairPetals, including the category's
Default / removed state. The bald mesh is a modeling scaffold. Each Head is one choice; fitted hats include their
delivered petal hair. Face, Body and Accessories save independently. The two scarves replace each other in the
same Accessories slot and can both be worn over Body. Shelf previews stay on stock Bloom with only their own item;
fitting affects only the main stand and then the saved outfit on Home. Cosmetics stays absent from Shop.

## Runtime

The baker conditions garment layers on Head, scarf layers on Head + Body and glasses on Head + Body + scarf.
ScarfKnit's `Over_Jacket`, `Over_Coat` and `Over_Overalls` morphs activate with the matching garment. ScarfStripe
retains its delivered geometry; visible portions are depth-masked against each outfit.
255 cropped lossless RGBA8 sprites cover all 240 saved combinations / 216 distinct looks, including the modeling-only
bald head in offline sprite recipes. An outfit draws at most four small layers, with no full-outfit atlas.
The orthographic bake uses 94% of Sprig's scale around the same foot line so Bloom's taller stock petal halo fits
the logical hero cell. Every garment and head choice shares that camera; source geometry remains unchanged.
Source ZIPs, FBXs, GLB, reference images and rigs stay outside application Resources and APKs.

132 retired legacy Bloom motion PNGs (7,067,971 bytes) remain under `../../archive/bloom/`, outside runtime.
The legacy baker cannot publish Bloom. Existing profile portraits and level mosaics remain unchanged.
No APK or levels are generated here. Reference designs: `../../references/bloom-v2/`.
