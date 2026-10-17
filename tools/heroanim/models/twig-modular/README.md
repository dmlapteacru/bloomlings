# Current Twig: one bald base, Acorn Hat and Panama Hat

Owner delivery, 2026-10-17. Original archives are preserved byte-for-byte under `source/`; original filenames / hashes
are recorded in `source-manifest.json`:

- `pumpkin+character+3d+model.zip` → `TwigHero_AcornHead_Tripo.zip`: source head with the default acorn hat.
- `cute+character+head+3d+model.zip` → `TwigHero_PanamaHead_Tripo.zip`: source head with the green panama.
- `cute+cartoon+3d+model.zip` → `TwigHero_BaldBase_Tripo.zip`: the complete bald body / head used in every state.

Each FBX is a single connected textured mesh, without a skeleton or actions. The separate heads contain different
faces. They are not swapped into the runtime: only derived headwear surfaces are taken from those two generations.
The same complete bald base is retained, so the face, ears, torso, hands and feet do not change with the chosen hat.

## Assembly and rendering

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-twig-modular.py
node tools/heroanim/bake-twig-modular.mjs --review
node tools/heroanim/bake-twig-modular.mjs
node tools/heroanim/check.mjs
```

`twig-modular-fit.json` records slanted trimming planes and accessory-only scales / translations. The importer trims
each accessory copy above its attachment seam, rejects remaining face paint / geometry using color, surface normal
and spatial masks, and keeps the connected hat region. Both hats retain the X scale 0.90 (3.23% narrower than
initial delivery). Panama retains its lowered position. Acorn returns to its original Z translation 0.40; only its
lower band receives an additional smooth X taper, up to 9% below world Z=0.80, fading to zero at Z=1.00. This brings
the rim closer to the head without squeezing the crown / leaf or lowering the forehead edge. Depth and vertical
scale stay unchanged. The base is never trimmed or reshaped. Its imported world
coordinates are checked within 1e-7 and its UV coordinates remain identical. Original sources and color images remain
intact. No second editable `.blend` master is created; derived GLBs are only exchange / rendering outputs.

Ignored `content/work/twig-modular/build/` contains `TwigHero_Modular.glb`, the shared `TwigHero_Base.glb` and fitted
`TwigHero_AcornHat.glb` / `TwigHero_PanamaHat.glb`. They share one coordinate system and origin. If imported into the
owner's master, use the base plus one headwear-only GLB, retaining transforms. These are static unrigged derivatives;
a later animation / attachment workflow still belongs in that master.

The bake uses the approved 75% painted albedo + 25% soft white diffuse treatment, without the extra PBR / specular
shading that darkened the previous review. No source texture repaint or base-head width correction is applied.
Five transparent full-color PNGs are generated: bare / acorn / panama and two hat-only Shop icons. They share one
448×504 logical camera cell and foot line, with 3× alpha-safe supersampling. Together they weigh 365,113 bytes.
`TwigModularData.cs` and `twig-modular.json` record crop rectangles and hashes. Fixed visible-face and body comparison
regions have exactly the same pixels in all three states; publication fails if those regions differ.

## Game behavior

Shop → Cosmetics → Twig has free Acorn Hat and Panama Hat items. Acorn keeps its previous ID `hat.twig_acorn`;
Panama uses `hat.twig_panama`. One Hat slot switches them, Remove / Default returns to the bald base. They can be
selected in the already unlocked Shop before the regular Wardrobe unlock at L40. Saves / application restart,
Home, Wardrobe and victory share that selection. Both fitted items remain excluded from core gameplay figures.

The old Daisy Cap ID stays in save / catalog data but is withheld until refitted. Previous Sprig fitted pieces also
remain withheld. The current simplified Sprig / Bloom / Drop, generic 2D cosmetics and independent profile avatars /
frames / badges keep their behavior. Only small PNGs ship in APK assets; source ZIPs / FBXs / GLBs do not.

The approved generated modeling reference images are retained in `../../references/twig-modular/`. Current game
captures: `output/twig-modular-in-game/`. No APK is built here; this trial uses static poses and existing 2D breathing.

Superseded for active Twig rendering by the owner-aligned `../twig-v2/` delivery. This earlier source / fit / art
library remains archived and reproducible. Active equipment keeps the same Acorn / Panama IDs.
