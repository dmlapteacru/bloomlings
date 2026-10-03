# Source record: the owner's animated heroes and layered Home (spec 005 FR-028)

| Field | Value |
|---|---|
| Delivered | by the project owner on 2026-10-02, in conversation: four rigged and animated FBX models and `bloomlings_home_assets.zip` (the Home picture in five layers, with a README giving the layer order); on 2026-10-03 the same four models again, exported at 60 fps, to replace the first ones ("the same heroes but 60 fps. Just replace them") |
| Made with | the models: Meshy AI (meshy.ai), text-to-3D with Meshy's auto-rigging and its animation library in one file per hero (first `Meshy_AI_…_biped_Meshy_Merged_Animations.fbx`, now `Meshy_AI_…_biped_Animation_all_frame_rate_60.fbx`); the Home layers: the owner's picture generator (ChatGPT image generation, as the other owner pictures, `tools/artgen/models/owner-pictures.md`) |
| Rights | the owner's: they delivered the files for the game to use, as with the other owner pictures (owner's statement on 2026-10-02 for the pictures). the models were made on the owner's personal Meshy licence (owner's statement on 2026-10-03), under which Meshy's terms give the output to its creator: no attribution is needed (the free plan's CC BY 4.0 does not apply) |
| Constitution | VII: the models are never loaded by the game. `bake.mjs` renders them offline into flat frame pictures, shown on meta screens only (Home and the splash, the win; the milestone card shares the win's hero) |

## The models

| File here | Delivered as | SHA-256 |
|---|---|---|
| `models/sprig.fbx` | `Meshy_AI_Leafling_Character_Tu_biped_Animation_all_frame_rate_60.fbx` (the owner: "the files are named a little differently") | `61a47b4ae2137931685019095dba3c6816458bc18a5d222a2fd9a09d231eb4a5` |
| `models/bloom.fbx` | `Meshy_AI_Petalina_biped_Animation_all_frame_rate_60.fbx` | `052c59366cdfb92dd60c2f3e35598fa98fe62a7bf3a77795b353471a0980479c` |
| `models/drop.fbx` | `Meshy_AI_Dewdrop_Buddy_biped_Animation_all_frame_rate_60.fbx` | `cb75560cfcfd2b75afee43d5e1b9d656043eee91ff0dddae502f29cc309c3a02` |
| `models/twig.glb` | `Twig.glb` (2026-10-03; see "Twig's Blender model" below) | `95c9bd89f9400b851c027c05ff89d02051ce33cd7bbd38130ce0941a586231ed` |

The first delivery (`…_Meshy_Merged_Animations.fbx`, 2026-10-02) had the same models and clips with the frames 1/24 s
apart; the 60 fps export holds the same frames 1/60 s apart, so its clips last 0.4 times as long (the idle 1.6 s, the
reactions 0.8 s) and it names them `target_character|target_character|<id>`. The bake finds a clip by the id at the end
of its name and plays it at the owner's table's length (research D22).

Each Meshy model holds one textured skinned mesh (a 28-bone Mixamo-style rig) and between 5 and 12 clips. Many clips carry Meshy's
library ids instead of names; the owner's table (2026-10-02) names the two each hero uses, and `heroes.json` maps them:

| Hero | Constant (idle, 4 s) | Reaction A (2 s) | Clip ids (idle; reaction) |
|---|---|---|---|
| Sprig | breathing + sway | curious head tilt | `01a0fe96-…`; `01a0fe99-…` |
| Bloom | breathing + soft sway | happy bounce | `01a0fe96-…`; `01a0fea0-…` |
| Drop | breathing + soft body sway | soft buoyant bounce | `01a0fe96-…`; `01a0fea0-…` |
| Twig (until 2026-10-03) | breathing + sway | head tilt + tiny bounce | `01a0fe96-…`; `01a0fea0-…` |

`01a0fe96` is the 4-second idle every model carries (96 frames); `01a0fe99` is the 2-second head tilt; `01a0fea0` the
2-second bounce (48 frames each) (it tilts the head on the way up, Twig's "head tilt + tiny bounce"). The other clips (walking, running, hops,
dances, `victory`) are not used.

## Processing (`bake.mjs`, `heroes.json`)

- three.js 0.160.0 `FBXLoader` (the Meshy models) and `GLTFLoader` (Twig's `.glb`) in headless Chromium
  (playwright-core 1.56.1, SwiftShader); the FBX loader is patched at bake time to read the embedded textures, which
  Meshy names after a `.fbm` folder (`serve.mjs`). Only the skinned meshes are drawn: an exported scene's other meshes,
  lights and cameras are hidden.
- The models' normal maps are dropped (they show blotches); the albedo is lit with a Lambert material, a warm hemisphere
  light, a key light from the upper left, a fill and a rim light (`heroes.json` `light`).
- One camera per hero for all its frames (field of view 20°, 10° from above, turned toward the fountain's middle by its
  `yaw`), rendered 2.5 times larger and drawn down into a 448 × 504 cell (the still heroes' 8:9 shape) with the seam
  pose's feet on 90% of its height, the seam pose at most 84% of the cell high (`fill`) and every frame at least 1.5%
  inside the cell (`margin`).
- The idle eases into its own first pose over its last 0.75 s, so it loops; the reaction blends in from that pose over
  0.25 s and back to it over its last 0.4 s, so it starts and ends where the idle loop starts.
- Each Meshy clip at the length of the owner's table (`heroes.json` `idleSeconds` 4, `reactSeconds` 2), whatever time
  the file stamps on its frames; Twig's clips at their own spans (from their first keyframe to their last). A hero with
  a `win` clip (Twig's cheer) gets it baked like the reaction, from the seam pose and back to it.
- 24 frames per second (every frame of the motion; 12 before the 60 fps delivery): 96 idle and 48 reaction frames per
  hero. Every frame is cropped to its visible bounds and stored as an 8-bit palette PNG (one 256-entry palette per hero
  shared by all its frames, entry 0 clear, Floyd–Steinberg dithered, `png8.mjs`), 23 MB for the 576 frames.

## The Home layers (`layers.mjs`)

| Delivered | Here (`client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`) |
|---|---|
| `01_home_bg_back.png` (852 × 1846) | `home.jpg`: re-encoded as JPEG (quality 90); it replaces the earlier single Home picture |
| `02_home_fountain_back.png` | `home-fountain-back.png`: cropped to its visible bounds (alpha under 6 of 255 counts as dust) |
| `02_home_fountain_back.png` | `home-lotus.png`: the lotus cut out of it (its pink petals and what they enclose, the edge softened), drawn again over Bloom, who stands behind it |
| `03_home_fountain_front.png` | `home-fountain-front.png`: cropped the same way |
| `04_home_soft_shadow.png` | `home-shadow.png`: its front left shadow (of four), cut out with faded edges, drawn under every hero |
| `05_home_petals_overlay.png` | `home-petals.png`: cropped the same way; drifts down over the scene |

The layers' boxes in the 852 × 1846 picture are in `client/Assets/Bloomlings/UI/Design/HomeLayersData.cs`; the input
files' hashes are in `layers.json`.

## Twig's Blender model (2026-10-03)

| Field | Value |
|---|---|
| Delivered | by the project owner on 2026-10-03, in conversation, to replace Twig: "replace the twig fbx with this one, and let it be Twig in the celebration after the round". Two FBX exports came first: the first with no texture at all (a plain grey material), the second (`Twig_v2.fbx`) naming four textures it did not embed; then `Twig.glb`, used here |
| Made with | Blender 4.2 (glTF exporter 5.2.39): a 122 721-vertex skinned mesh with UVs, its own 24-joint rig (`Twig_rig_*`), one shape key (`Twig_cheer_update_LidsSurface`, the eyelids) and three embedded 1024 × 1024 PNG textures (color, normal, metallic-roughness; "Mixar Twig"); the file also holds Blender's default cube, which the bake hides |
| Rights | the owner's: delivered for the game, as the other owner pictures |
| Clips | `Twig_Breathing` (3 s, the idle), `Twig_SmallBounce` (1.5 s, the reaction), `Twig_WinCheer` (3 s, the win's celebration: a jump with the arms spread), keyed 24 a second; the bake keeps their own lengths (`heroes.json` `idleSeconds`, `reactSeconds` null) |
| Bones | the head points the hats follow: `Twig_rig_Head` (the head's base) and `Twig_rig_Leaf` (the leaf's stem on top of the cap) |
| Frames | 72 idle, 36 reaction and 72 win frames, 7.96 MB; with the Meshy heroes, 612 frames and 25.5 MB in all |
