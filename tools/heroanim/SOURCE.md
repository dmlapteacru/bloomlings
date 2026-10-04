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
| `models/sprig.glb` | `Sprig_Complete.glb` (2026-10-03; see "Sprig's Blender model" below; it replaced the Meshy `sprig.fbx`, `61a47b4a…`) | `9bdebdff3d18bc53341549394afdab24e8b81843c4ff87fada9c11758e6ecde5` |
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
| Sprig (until 2026-10-03) | breathing + sway | curious head tilt | `01a0fe96-…`; `01a0fe99-…` |
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
| Look | lighter and warmer than its texture, the owner's choice "H" of six variants (2026-10-03: "Twig looks dark, as if in shadow"): an even light (`heroes.json` `light`: hemisphere 4.2, key 0.9, fill 0.6 and a 0.2 glow of its own albedo) and a grade of its texture (`color`: gamma 0.75, gain 1.08, saturation 1.1, warm 0.04); its mean brightness 0.73 (it was 0.46, the other heroes about 0.62). On 2026-10-04 the owner found it "too bright" beside the new Sprig; of three toned-down variants "B" is used: hemisphere 3.9, key 0.9, fill 0.6, glow 0.12; gamma 0.84, gain 1.0, saturation 0.74, no warm shift (brightness 0.62, saturation 0.59; Bloom 0.63 and 0.50, Drop 0.61 and 0.47) |
| Frames | 72 idle, 36 reaction and 72 win frames, 7.97 MB; with the Meshy heroes, 612 frames and 25.5 MB in all |

## Sprig's Blender model (2026-10-03)

| Field | Value |
|---|---|
| Delivered | by the project owner on 2026-10-03, in conversation, to replace Sprig: "replace the hero, and add it to the round's celebration, taking turns with Twig; use celebrate/clap there, alternating". Two exports of `Sprig_character_Model.glb` came first (the same file twice): its rigged mesh's own color texture was blank (cream islands only), the colors lying on an unrigged copy (`Sprig_design`) beside it; then `Sprig_Complete.glb`, used here |
| Made with | Blender (glTF exporter 5.2.39): one 6 139-vertex skinned mesh with UVs and tangents, its own 34-joint rig (`Head`, `Neck`, `Chest`, `Spine`, `Pelvis`, arms with three fingers, legs) and three embedded 2048 × 2048 PNG textures (base color, normal, roughness: "Prepare_single_GLB_Sprig"); the mesh lies 4 m to the side of the scene's origin |
| Rights | the owner's: delivered for the game, as the other owner pictures |
| Clips | `Sprig_Breathing` (4 s, the idle), `Sprig_Wave` (3 s, the reaction), `Sprig_Celebrate` (2 s) and `Sprig_Clap` (3 s), the win's two celebrations, taking turns; keyed 24 a second, at their own lengths. Not used: `Sprig_LookAround` (5 s), `Sprig_SmallJump` (1.5 s), `Sprig_ThumbsUp` (2.7 s) |
| Placement | `heroes.json` `ground: "model"`: the hero's ground point is the middle of its posed bounds' base (the mesh is 4 m from the origin), not the origin |
| Bones | the rig has no bone at the head's top, so the brow the hats follow is a point 0.3 m up the `Head` bone (`topOffset` [0, 0.3, 0], at the top of the face under the leaves); the chin is `Head` |
| Look | toned down on 2026-10-04 (the owner: "Sprig too", after Twig's "too bright"): a grade of its texture, `color` gamma 1.04, gain 0.94, saturation 0.74, under the shared light (brightness 0.65 and saturation 0.64; it was 0.69 and 0.79) |
| Frames | 96 idle, 72 reaction, 48 `win` and 72 `win2` frames, 11.8 MB; with the other heroes, 756 frames and 31.3 MB in all |
