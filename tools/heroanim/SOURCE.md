# Source record: the owner's animated heroes and layered Home (spec 005 FR-028)

| Field | Value |
|---|---|
| Delivered | by the project owner on 2026-10-02, in conversation: four rigged and animated FBX models and `bloomlings_home_assets.zip` (the Home picture in five layers, with a README giving the layer order) |
| Made with | the models: Meshy AI (meshy.ai), text-to-3D with Meshy's auto-rigging and its animation library merged into one file per hero (`Meshy_AI_…_biped_Meshy_Merged_Animations.fbx`); the Home layers: the owner's picture generator (ChatGPT image generation, as the other owner pictures, `tools/artgen/models/owner-pictures.md`) |
| Rights | the owner's: they delivered the files for the game to use, as with the other owner pictures (owner's statement on 2026-10-02 for the pictures). Meshy's terms give the output to the creator on its paid plans, while output made on its free plan is published under CC BY 4.0 (attribution required). **To confirm with the owner which plan made these models**; until then the release checklist keeps this record open (`specs/001-core-game-mvp/checklists/originality.md`) |
| Constitution | VII: the models are never loaded by the game. `bake.mjs` renders them offline into flat frame pictures, shown on meta screens only (Home and the win; the milestone card shares the win's hero) |

## The models

| File here | Delivered as | SHA-256 |
|---|---|---|
| `models/sprig.fbx` | `Meshy_AI_Leafling_Character_Tu_biped_Meshy_Merged_Animations.fbx` (the owner: "the files are named a little differently") | `fa7f3a34b4037d39f1e78e5fde59b6d1a257aee61b6a7d4398288eb99432f779` |
| `models/bloom.fbx` | `Meshy_AI_Petalina_biped_Meshy_Merged_Animations.fbx` | `58b8a20e089dde30928f90557fc5db2e4096efd9be09fce1e3270de04c1cea39` |
| `models/drop.fbx` | `Meshy_AI_Dewdrop_Buddy_biped_Meshy_Merged_Animations.fbx` | `afe9851a48ef7183cfda42126ff644364b01dd84543b939c1d4ffc6215d739c9` |
| `models/twig.fbx` | `Meshy_AI_Acorn_Sprout_biped_Meshy_Merged_Animations.fbx` | `0dbad90b56aefd5c6224e6d9387d182e7f3da00b9452b3a0b78d527f7570f7ea` |

Each holds one textured skinned mesh (a 28-bone Mixamo-style rig) and between 4 and 12 clips. Many clips carry Meshy's
library ids instead of names; the owner's table (2026-10-02) names the two each hero uses, and `heroes.json` maps them:

| Hero | Constant (idle, 4 s) | Reaction A (2 s) | Clip ids (idle; reaction) |
|---|---|---|---|
| Sprig | breathing + sway | curious head tilt | `01a0fe96-…`; `01a0fe99-…` |
| Bloom | breathing + soft sway | happy bounce | `01a0fe96-…`; `01a0fea0-…` |
| Drop | breathing + soft body sway | soft buoyant bounce | `01a0fe96-…`; `01a0fea0-…` |
| Twig | breathing + sway | head tilt + tiny bounce | `01a0fe96-…`; `01a0fea0-…` |

`01a0fe96` is the 4-second idle every model carries; `01a0fe99` is the 2-second head tilt; `01a0fea0` the 2-second
bounce (it tilts the head on the way up, Twig's "head tilt + tiny bounce"). The other clips (walking, running, hops,
dances, `victory`) are not used.

## Processing (`bake.mjs`, `heroes.json`)

- three.js 0.160.0 `FBXLoader` in headless Chromium (playwright-core 1.56.1, SwiftShader); the loader is patched at bake
  time to read the embedded textures, which Meshy names after a `.fbm` folder (`serve.mjs`).
- The models' normal maps are dropped (they show blotches); the albedo is lit with a Lambert material, a warm hemisphere
  light, a key light from the upper left, a fill and a rim light (`heroes.json` `light`).
- One camera per hero for all its frames (field of view 20°, 10° from above, turned toward the fountain's middle by its
  `yaw`), rendered 2.5 times larger and drawn down into a 448 × 504 cell (the still heroes' 8:9 shape) with the seam
  pose's feet on 90% of its height.
- The idle eases into its own first pose over its last 0.75 s, so it loops; the reaction blends in from that pose over
  0.25 s and back to it over its last 0.4 s, so it starts and ends where the idle loop starts.
- 12 frames per second: 48 idle and 24 reaction frames per hero. Every frame is cropped to its visible bounds and stored
  as an 8-bit palette PNG (one 256-color palette per hero, dithered, `png8.mjs`), 11 MB for the 288 frames.

## The Home layers (`layers.mjs`)

| Delivered | Here (`client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`) |
|---|---|
| `01_home_bg_back.png` (852 × 1846) | `home.jpg`: re-encoded as JPEG (quality 90); it replaces the earlier single Home picture |
| `02_home_fountain_back.png` | `home-fountain-back.png`: cropped to its visible rows |
| `02_home_fountain_back.png` | `home-lotus.png`: the lotus cut out of it (its pink petals and what they enclose, the edge softened), drawn again over Bloom, who stands behind it |
| `03_home_fountain_front.png` | `home-fountain-front.png`: cropped |
| `04_home_soft_shadow.png` | `home-shadow.png`: its front left shadow (of four), cut out with faded edges, drawn under every hero |
| `05_home_petals_overlay.png` | `home-petals.png`: cropped; drifts down over the scene |

The layers' boxes in the 852 × 1846 picture are in `client/Assets/Bloomlings/UI/Design/HomeLayersData.cs`; the input
files' hashes are in `layers.json`.
