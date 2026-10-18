# Bloom fitted additions, 2026-10-18

Nine owner-supplied FBXs: seven Head choices and two Body choices. These extend the
accepted Bloom V2, using the same face, body, scale and foot line. The owner’s Blender
project remains the only maintained master. The unchanged FBX originals are in `source/`;
`fit.json` records source hashes and the complete reproducible fitting parameters.

| Source part | Slot | Stable game ID | Adjustment |
|---|---|---|---|
| Boater | Head | `hat.bloom_boater` | unchanged fit |
| BucketHatGreen | Head | `hat.bloom_bucket_green` | unchanged fit |
| CrownHair | Head | `hat.bloom_crown` | narrower fit around the head and slight downward adjustment |
| DressPurple | Body | `clothes.bloom_dress_purple` | rear-edge smoothing |
| FlowerHat | Head | `hat.bloom_flower_hat` | narrower fit around the head |
| Headscarf | Head | `hat.bloom_headscarf` | narrower fit around the head and slight downward adjustment |
| Nightcap | Head | `hat.bloom_nightcap` | narrower fit around the head and slight downward adjustment |
| Panama | Head | `hat.bloom_summer_panama` | narrower fit around the head |
| TopFlower | Body | `clothes.bloom_top_flower` | rear-edge smoothing |

The seven Head parts include their delivered petal hair and each has rigid 100% Head
weights. Boater and Green Bucket Hat already fitted and retain identity transforms.
The other five Head items are slightly narrowed in width / depth; three also move down
by 5–10 mm in the owner’s model coordinates. DressPurple and TopFlower retain their
position and front silhouette; local smoothing removes jagged segmentation edges on
the back. Vertex count, UVs, material textures and skin weights remain unchanged.
Front and side renders are retained in `review/`.

`prepare-bloom-extras-v1.py` checks source hashes, applies the declared edits, exports
nine fitted FBXs with their supplied armatures and reimports every export. The preparation
report records world-space rest geometry error below 1e-5. All accepted Bloom mesh
coordinates are verified unchanged. This is a fitting handoff; the separate skeletal
action / rig repair feature remains paused. No animations are published by this addition.

[Fitted FBX archive for the owner’s Blender master](../../products/BloomHeroV2_Extras_Fitted.zip)
contains all nine items, their source names, fit parameters and build report. Import the
parts into the existing scene, replacing the corresponding raw imports; the base hero is
not included or replaced.

## Game integration

All nine are free testing choices in Wardrobe only. Head and Body are separate slots;
Face and Accessories retain both current eyewear shapes and the two existing scarves.
Default Head restores the stock petals. Category filters and item-only cards retain their
existing fitting / equipment flow. Classes keep panamas together, followed by the other
headwear classes; Top and Dress are Body choices.

The additional bake creates front and Home recipes only for outfits affected by a new
Head or Body. Existing accepted bakes remain intact. Every affected outfit has its own
base / clothing / scarf / round-glasses / heart-glasses mask recipe. Scarves use the
existing Over_Overalls fit over the two light garments. Depth masks include all selected
parts, and identical PNG pixels / crop offsets are stored once. Home’s Bloom yaw remains
0°, matching the accepted fountain composition. Card icons contain only the isolated item.
All runtime output is cropped RGBA PNG; FBXs, rigs, source images and handoff archives
remain outside Resources and the APK.

## Rebuild

With the accepted Bloom and separate eyewear GLB caches prepared, run from repo root:

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-bloom-extras-v1.py
node tools/heroanim/bake-bloom-extras-v1.mjs
node tools/heroanim/check.mjs
```

The main `bake.mjs` runs this extension after the accepted family, Home and eyewear bakes.
The normal Home publisher replaces Home resources, so rerun both extensions after invoking
it separately. The additional record `bloom-extras-v1.json` pins sources, fitted FBX ZIP,
output hashes, complete outfit maps, isolated icons and generated `BloomExtrasData.cs`.
Game fixtures 178–188 cover every new card, return to Home, and a combined outfit with
glasses / scarf. No APK or level content is generated.

## Validation

54 focused client tests pass, including all registered outfit combinations, persistence,
category separation, item ordering and the stock Head fallback. The asset check validates
all PNG hashes / crop dimensions and original / fitted source records. The 11 item and
combined-outfit game fixtures pass at 1080×2340, including staged navigation and delayed
loading checks. [Game gallery](review/game-board.png) and [combined outfit](review/game-outfit.png).

The extension adds 81 unique sprites, 2,940,379 bytes total, for 180 affected outfit recipes
in each view. Runtime source art is unchanged outside the separately registered additions.
