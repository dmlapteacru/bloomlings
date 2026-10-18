# Bloomlings accepted hero sources and processing

The owner permanently accepted the four modular V2 heroes on 2026-10-17. All previous
hero model versions, experimental rigs, archived PNG animation libraries and their unused
render scripts have been removed from the current tree. Earlier iterations remain in Git
history. There is one maintained Blender master per owner workflow, not a second editor
scene in the repository. Raw deliveries are authoring inputs, never mobile game assets.

## Current authoritative deliveries

- `models/twig-v2/source/TwigHeroV2.zip` plus its later four FBX additions: accepted Twig
  base, seven Head choices (stock acorn hat), round glasses, scarf and three garments.
- `models/sprig-v2/source/`: the two owner-aligned Sprig V2 delivery ZIPs, with the Leafy
  stock Head, other Head alternatives, glasses, fitted scarf and three garments.
- `models/bloom-v2/source/`: the two owner-aligned Bloom V2 delivery ZIPs; rounded Petals
  stock Head, alternate hairstyles / headwear, glasses, two scarves, three garments.
- `models/drop-v2/source/` and `products/DropHeroV2_FBX.zip`: the segmented owner sources
  and verified fitted / rigged handoff. NativeDrop is a whole droplet head on the short body;
  other Heads share the rounded modeling face. `models/drop-clothing-study/` is retained
  because its unchanged base ZIP and fitted overalls FBX are CURRENT Drop build inputs,
  pinned in `models/drop-v2/source-manifest.json`; it is not a fallback to an old game hero.

- `models/eyewear-v1/source/`: six unchanged owner FBXs, two separate frames each for
  Sprig, Bloom and Twig. [Fit and rebuild record](models/eyewear-v1/README.md). Existing
  round IDs are retained; square, heart and octagon alternatives add three Face choices.

- `models/bloom-extras-v1/source/`: nine additional unchanged owner FBXs. Their derived
  [fitted Head / Body handoff](models/bloom-extras-v1/README.md) retains UVs and skin weights,
  with declared head fits and local garment edge smoothing. Existing Bloom geometry stays fixed.

- `models/wardrobe-additions-v1/source/`: two unchanged owner archives, 17 Sprig / Twig
  FBXs. [Fit and handoff record](models/wardrobe-additions-v1/README.md) assigns the Twig
  vest from the Sprig ZIP correctly and retains UVs, skin weights and accepted hero bases.

The owner generated / supplied these models and textures through Tripo / Meshy and their
own Blender project. Source manifests retain original filenames and SHA-256 values. No
third-party model or motion library was substituted. Fitting, weight transfer, scripted
motions and coordinate conversion are documented in each model folder. Source topology,
UVs and aligned model coordinates are verified by the preparation reports.

## Runtime sprite derivation

The [shared rig repair](rigging/README.md) derives a separate, checked authoring handoff
from these same deliveries. It preserves all hero and garment rest geometry, topology,
UVs, albedo textures and accessories. Normalized linear skinning,
half-angle support bones, outfit occlusion materials and six authored body actions are
validated before publishing [four family archives](products/rig-fix-v1/README.md).
Their manifest pins raw input hashes and a real FBX export / import check, including the
owner importer. This package is the next rig authoring baseline; existing runtime bakes
remain pinned to their original static deliveries until the separate animation feature.

`prepare-{family}-v2.py` imports current FBXs into a disposable GLB cache. Drop's game
prepare step imports its final handoff and applies one common −90° front rotation. Bakes
use Three.js with an orthographic camera and soft studio lighting, preserving a bright
75% albedo / 25% diffuse finish. Family eyewear colors are leaf green, petal pink, warm
cream and chestnut brown. Twig's panama remains warm brown with cream trim and daisy.

The four `bake-{family}-v2.mjs` tools produce cropped RGBA8 PNG layers: shared base + Head,
garment, scarf, glasses. Scarf morphs accommodate all garments. Depth-only occlusion masks
keep the original fitted geometry aligned. Crop coordinates refer to a 448×504 logical
cell with 96 px top padding, fixed body scale and a common foot line. Linear-light alpha
reduction preserves edges and gradients. `*-v2.json` records every source, output hash,
crop, recipe and generated `*V2Data.cs`.

`bake-home-heroes.mjs` uses gentle three-quarter yaws matching the earlier fountain layout:
Sprig +30°, Bloom 0°, Drop −25°, Twig −30°. Whole bodies and all fitted items share the
view; extreme profile poses are excluded. The studio rotates with the subject to retain
the approved finish. Wardrobe and victory use frontal art. Home's touch targets and shadows
follow its angled sprites. `home-heroes.json` pins all 623 outfit-layer counterparts;
identical pixels with identical crop coordinates are stored once. No 3D rendering occurs
at runtime. Existing restrained 2D breathing remains until the separate action feature.

## Profile, fallback and promo identities

`references/current-identity/` retains newly generated profile artwork based on the exact
accepted game renders, not the retired pointed-petal / spiky-leaf / single-body Drop models.
The image-generation source and processing are recorded alongside the atlas. Stable avatar
IDs, prices, ownership and selections remain intact. The same new identities appear on
achievement medals and No Ads sprites. Twig's already accepted identity art remains in
`references/twig-v2/identity/` and is published by `pack-twig-identity.py`.

`pack-current-heroes.py` extracts the authored atlas cells, encodes 384 px avatar JPEGs,
256 px medals and transparent promo PNGs, and records all inputs / outputs. It also creates
512×576 solo / cheer fallback illustrations from the actual production front sprites.
Blank twins are rendered from current geometry by `bake-current-blanks.mjs` and
`bake-twig-blank.mjs`; all four are current owner identities. The group fallback combines
the four production sprites with a shared foot line. `current-identity.json` and
`twig-identity.json` pin publication. Regeneration never invokes the level generator.

## Backgrounds and validation

The owner's background sources remain in `backgrounds/`; matching garden, ivory/jade
fountain and blush lotus layers are recorded by `layers.json`. `backdrops.mjs` applies the
approved finish and pins it in `backdrops.json`. Original authored rights remain the owner's.

`node tools/heroanim/check.mjs` verifies current source records, every runtime pixel file,
C# metadata, references and backgrounds. The game resource manifest contains only current
V2 sprites and the Home view set. Old model imports / resources are forbidden by the
permanent-adoption regression checks. Existing level mosaics are authored level content;
the owner keeps generation local, and the previous decision to keep L2000 is respected.
