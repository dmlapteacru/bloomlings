# TwigHeroV2 — permanent production identity

The owner approved this common bald base in game. The authoritative delivery consists of:

- `source/TwigHeroV2.zip`: original base, seven Head parts, original eyewear, scarf, 25-bone rig and Idle / Joy / Walk /
  Wave actions, retained byte for byte.
- `source/additions-2026-10-17/`: three clothing FBXs and replacement Glasses FBX, also retained unchanged.
- `source-manifest.json`: source hashes, addition / replacement roles and embedded export manifest path.

The latest Glasses replaces the old frame-plus-lenses mesh. Its single rounded frame has open lens holes, so no
lens image can obscure the eyes. Selection keeps the stable `glasses.twig_round` ID. The owner's Blender project is
the only editable master; no second `.blend` is maintained here.

[ITEMS.md](ITEMS.md) lists all **12** fitted items and their stable game IDs: **7 Head, 1 Glasses, 3 Clothes, 1 Neck**.
Head choices include hair, hair-with-hat and earmuffs-with-hair. The acorn hat is the stock in-game appearance; the bald mesh is only a modeling base. The Clothes choices
are Winter Jacket, Autumn Coat and Garden Overalls. All twelve are temporarily free in Shop → Cosmetics → Twig and
in the Wardrobe accessories shelf, including before L40. Clothes, Head, Glasses and Neck save independently and
follow Home / fitting stands / victory. Removing one slot preserves the others; Default clears the whole outfit.
Profile avatars, frames and badges remain independently selected. Meta-only clothes never reach puzzle workers.

## Rebuild

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-v2.py
node tools/heroanim/bake-twig-v2.mjs
node tools/heroanim/bake-twig-blank.mjs
python tools/heroanim/pack-twig-identity.py
node tools/heroanim/check.mjs
```

`bake-twig-v2.mjs --review` instead writes sprites and combined outfit previews to ignored `content/work/twig-v2/review/`.
The importer extracts the original ZIP safely, applies the separate source overrides, imports rest coordinates, proves
unchanged world vertices / UVs and removes only the derived bake copy's rig. No trimming, refitting or source painting.
The rig and four actions remain in the FBXs / ZIP for the separately planned animation / action-selection feature.
The current game uses static renders with its existing subtle 2D breathing.

## Materials and compact layering

`../../twig-v2-look.json` authors the owner's requested warm brown / cream accessory palette during offline rendering.
The Panama fabric / underside are graded continuously by luminance; cream trim, separate daisy and painted detail
remain. The new glasses frame is soft brown. Base face / body textures and source files do not change.
Rendering retains 75% painted albedo plus 25% soft white diffuse, original alpha, and alpha-safe 3× supersampling.
The logical 448×504 cell and foot line remain constant; extra space above fits the tall holiday hat.

35 RGBA8 cropped PNGs total **1,674,582 bytes** and cover **128** saved combinations (112 distinct looks, since an empty Head slot and explicit Acorn Hat match):

- 8 shared base / Head states;
- 3 depth-masked garments;
- 4 scarf masks, one per garment or bare body;
- 8 eyewear masks, one per Head state;
- 12 isolated Shop icons.

The build report proves Head parts sit above clothes and scarf, and eyewear above clothes. This permits mask reuse
with the fixed front orthographic camera. Changed source extents fail preparation rather than silently clipping.
Four selected layers draw in order: base / head, clothes, scarf, glasses. No full-outfit atlas is loaded.
`twig-v2.json`, `TwigV2Data.cs`, HeroMotion's manifest and `build-report.json` pin sources, geometry, crops and output.
Only PNGs enter app resources. ZIPs, FBXs, GLB derivatives and reference images remain offline; no APK is built here.

## References and compatibility

[../../references/twig-v2/README.md](../../references/twig-v2/README.md) indexes the preserved modeling / seasonal
references, including the bald base. The old Twig runtime libraries are retired under `../../archive/twig/`.
Old Acorn / Panama IDs remain valid; the old Daisy Cap resolves to Panama. Existing saves retain progress, currency,
profile cosmetics and outfit choices. Clothes is appended to enum values / save kinds without renumbering old ones.
V2 avatars / medal and solo / group fallback images retain stable asset names. L2000's mosaic is deliberately unchanged.

Stock-head rule: removing headwear, Default and a fresh profile always show the Acorn Hat. The empty saved Head slot means stock appearance, not a bald hero. Source bald geometry / references remain available for modeling.
