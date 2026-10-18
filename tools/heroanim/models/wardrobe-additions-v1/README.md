# Sprig and Twig fitted wardrobe additions, 2026-10-18

The owner supplied two complete ZIP files with 17 FBXs. Unchanged archives are kept in
`source/`; `fit.json` records archive hashes, each FBX entry hash, family, slot and fitting
parameters. The Sprig archive also contains **TwigHeroV2_VestExplorer.fbx**; it is assigned
to Twig. Filenames containing part1 / part2 do not indicate split ZIP volumes.

Sprig has seven new Head choices and one Body choice. Twig has seven Head and two Body
choices. Sprig’s second planned light garment is not in these deliveries; all supplied
items are integrated. The owner’s Blender project remains the only maintained master.

| Hero | Source part | Slot | Stable game ID | Processing |
|---|---|---|---|---|
| Sprig | BowlerHat | Head | `hat.sprig_bowler` | alignment retained |
| Sprig | ExplorerHat | Head | `hat.sprig_explorer` | alignment retained |
| Sprig | HairClover | Head | `hat.sprig_clover` | alignment retained |
| Sprig | HairSprout | Head | `hat.sprig_sprout` | alignment retained |
| Sprig | Headscarf | Head | `hat.sprig_headscarf` | fitted |
| Sprig | Nightcap | Head | `hat.sprig_nightcap` | fitted |
| Sprig | VestExplorer | Body | `clothes.sprig_explorer_vest` | edge smoothing |
| Twig | VestExplorer | Body | `clothes.twig_explorer_vest` | edge smoothing |
| Sprig | Boater | Head | `hat.sprig_boater` | fitted |
| Twig | HairPinecone | Head | `hat.twig_pinecone` | alignment retained |
| Twig | Headscarf | Head | `hat.twig_headscarf` | alignment retained |
| Twig | MushroomCap | Head | `hat.twig_mushroom` | alignment retained |
| Twig | Nightcap | Head | `hat.twig_nightcap` | fitted |
| Twig | ShirtExplorer | Body | `clothes.twig_explorer_shirt` | edge smoothing |
| Twig | Boater | Head | `hat.twig_boater` | alignment retained |
| Twig | BowlerHat | Head | `hat.twig_bowler` | alignment retained |
| Twig | ExplorerHat | Head | `hat.twig_explorer` | fitted |

## Fitting and handoff

Sprig’s Boater rises to clear the eyes. Its Headscarf and Nightcap narrow in width / depth
and sit lower. Twig’s Explorer Hat lowers and narrows; its Nightcap narrows and lifts
slightly. The other nine Head items retain their supplied coordinates. All fourteen
Head meshes have 100% rigid Head weights. The three garments retain their fit and receive
local smoothing only along open mesh boundaries to reduce ragged cut edges. Vertex count,
UV coordinates, material textures and vertex weights remain unchanged. Accepted base
hero meshes are verified unchanged before the disposable static GLB is exported.

Every fitted FBX retains its supplied armature and is reimported to check world-space
rest geometry error below 1e-5. This is a fitting handoff; no skeletal animation repair
or new actions are included. [Download all 17 fitted FBXs](../../products/SprigTwig_Additions_Fitted.zip)
for the existing Blender master. They retain their original SprigHeroV2 / TwigHeroV2 names;
replace the corresponding raw imports. Base heroes are not duplicated in the handoff.
The archive also contains fit parameters and a separate proof for each family.

Actual front and side views are in `review/`, with [before](review/before-0.jpg) and
[after](review/after-0.jpg) overview sheets. All reviewers use the accepted short bodies.

## Game behavior

The new items are free testing choices in Wardrobe only. New hairstyles sort with hair,
Bowler / Boater / Explorer / Headscarf / Nightcap stay with their respective classes, and
Body gains light tops / vests ahead of the autumn and winter collections. Individual cards
show only their own item. Selection changes the main stand and saved appearance; it never
changes other item icons. Default Head restores the stock leaves / acorn hat.

The additional bake covers every affected Head × Body × scarf combination with both
existing glasses shapes. New outfit masks are separate from the accepted legacy bakes.
Scarves use their Over_Overalls fit over these light garments; depth masks include all
selected parts. Front and Home recipes retain the existing fixed scale and foot line.
Home rotates Sprig +30° and Twig −30° toward the lotus; the studio rotates with them.
Identical crop offsets / pixels share a PNG. Runtime art is cropped RGBA only; FBXs, GLBs,
source archives, rigs and handoff ZIPs remain outside Resources and APK packaging.

## Rebuild

With accepted Sprig / Twig and separate eyewear caches prepared, run from repository root:

```sh
blender -b --factory-startup -t 2 --python-exit-code 1 --python tools/heroanim/prepare-wardrobe-additions-v1.py
node tools/heroanim/review-wardrobe-additions-v1.mjs
node tools/heroanim/bake-wardrobe-additions-v1.mjs
node tools/heroanim/check.mjs
```

`prepare-wardrobe-additions-v1.py -- --inspect` emits unfitted inspection caches instead
of the handoff. Repeat normal preparation before baking; the baker rejects inspection
reports, stale fit parameters and a changed accepted base. Only one family’s source FBX
is extracted at a time, and temporary loose handoff FBXs are removed after ZIP publication.
The main `bake.mjs` runs this extension last, after the family, Home, eyewear and Bloom
additions bakes. Regenerate this extension after running the standalone Home publisher,
which replaces Home resources. `wardrobe-additions-v1.json` pins sources, fitted archive,
complete recipe tables, isolated icons, PNG hashes and generated C#.
No APK or levels are generated. Game fixtures 189–208 cover all new items and full outfits.

## Validation

56 focused client tests pass, covering all registered outfit combinations, save/reload,
family restrictions, category ordering and stock Head restoration. The asset check passes
for source hashes, FBX fitting proofs, complete mask tables and PNG dimensions / checksums.
All 20 game fixtures (189–208) pass at 1080×2340, including real card taps, Home persistence,
combined Face / scarf outfits, staged navigation and delayed loading checks.
[Game gallery](review/game-board.png) · [Home](review/game-home.png).

The publication adds 117 unique cropped PNGs (5,203,790 bytes) for 86 Sprig and 112 Twig
affected outfit recipes in each view. The 17 fitted FBXs were individually reimported;
maximum rest geometry error was 1.94e-7. The combined handoff ZIP’s entries and FBX SHA-256
values were checked after compression. Skeletal action deformation is outside this fitting
pass and remains the separately deferred animation work.
