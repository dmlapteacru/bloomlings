# TwigHeroV2: permanent production identity

`source/TwigHeroV2.zip` preserves the owner's archive byte for byte. `source-manifest.json` pins its hash. Its manifest
names the base FBX, 25-bone rig, Idle / Joy / Walk / Wave source actions and nine accessory FBXs:

| Export | Game item | Slot |
| --- | --- | --- |
| PumpkinHat | Acorn Hat (`hat.twig_acorn`, retained ID) | Head |
| BucketHat | Panama Hat (`hat.twig_panama`, retained ID) | Head |
| Hair | Woodland Hair | Head |
| HatHair | Acorn Hat & Hair; already includes its hat | Head |
| Beret | Autumn Beret | Head |
| EarmuffHair | Winter Earmuffs with short hair | Head |
| XmasHat | Holiday Hat with short hair | Head |
| Glasses | Twig Glasses | Glasses |
| Scarf | Golden Scarf | Scarf |

The seven Head variants replace one another. Glasses and Scarf are independent, giving 32 combinations with the bald
base. All nine items are temporarily free: Shop → Cosmetics → Twig, and the Wardrobe accessories shelf. Selection,
removing one item and Default / full reset survive application restart. The same selection draws on Home, fitting
stands and victory. Meta-only accessories never reach core gameplay workers or override independent profile avatars.

## Rebuild

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-v2.py
node tools/heroanim/bake-twig-v2.mjs --review
node tools/heroanim/bake-twig-v2.mjs
node tools/heroanim/check.mjs
```

The importer safely extracts the archive into ignored `content/work/twig-v2/build/source/`. It imports each FBX in
the owner's rest coordinates, checks unchanged world vertex positions and UVs, and removes the derived copy's rig
for the static art bake. No mesh trimming, refit, source texture painting or second editable Blender master. The
original rig and actions stay in the archive; the current game retains the current subtle 2D breathing. Full action /
animation selection remains a separate feature.

`content/work/twig-v2/build/TwigHeroV2.glb` is an offline exchange / rendering derivative. PNG generation keeps the
approved 75% albedo + 25% soft white diffuse look, original colors and opacity, and 3× alpha-safe supersampling.
The logical 448×504 cell, base scale and foot position remain consistent; additional space above the cell fits the
tall holiday hat. Eight head-with-base states, eight depth-masked Scarf layers, eight Glasses layers and nine isolated
Shop icons make 33 full-color PNGs totaling 1,614,285 bytes. Only the selected layers load; no 32-outfit atlas.
The ~33 MB ZIP, FBXs, GLB and Blender data never enter APK assets. No APK is built in this workflow.

`twig-v2.json`, `TwigV2Data.cs`, the HeroMotion manifest and `build-report.json` pin conversion / source / crop /
output metadata. Previous Twig / Sprig art libraries remain archived; old unrelated fitted pieces stay saved but
withheld until fitted to their active model. Current captures: `output/twig-v2-in-game/`.


## Permanent migration

Approved in game on 2026-10-17. This is the only active Twig source. Modeling references, including the bald base,
are indexed in [../../references/twig-v2/README.md](../../references/twig-v2/README.md). The old motion / fitted
runtime resources and generated helpers are retired in `../../archive/twig/`; no old Twig PNG remains in the
HeroMotion resources. The three old profile avatar IDs now show V2, as do the hero medal and static / group fallback
art. Their IDs, prices, achievements and save data remain compatible. L2000's mosaic is intentionally unchanged.

Identity publishing instructions are in `../../README.md`; `check.mjs` checks source / reference / output hashes.
