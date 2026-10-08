# Source record: the owner's animated heroes and layered Home (spec 005 FR-028)

| Field | Value |
|---|---|
| Delivered | by the project owner on 2026-10-02, in conversation: four rigged and animated FBX models and `bloomlings_home_assets.zip` (the Home picture in five layers, with a README giving the layer order); on 2026-10-03 the same four models again, exported at 60 fps, to replace the first ones ("the same heroes but 60 fps. Just replace them"); on 2026-10-03 Twig's and Sprig's Blender models; on 2026-10-04 `Heroes.glb`, the four heroes in one Blender file, which replaces them all ("bake them all and update"; see "The owner's Heroes.glb" below) |
| Made with | the models: Meshy AI (meshy.ai), text-to-3D with Meshy's auto-rigging and its animation library in one file per hero (first `Meshy_AI_…_biped_Meshy_Merged_Animations.fbx`, now `Meshy_AI_…_biped_Animation_all_frame_rate_60.fbx`); the Home layers: the owner's picture generator (ChatGPT image generation, as the other owner pictures, `tools/artgen/models/owner-pictures.md`) |
| Rights | the owner's: they delivered the files for the game to use, as with the other owner pictures (owner's statement on 2026-10-02 for the pictures). the models were made on the owner's personal Meshy licence (owner's statement on 2026-10-03), under which Meshy's terms give the output to its creator: no attribution is needed (the free plan's CC BY 4.0 does not apply) |
| Constitution | VII: the models are never loaded by the game. `bake.mjs` renders them offline into flat frame pictures, shown on meta screens only (Home and the splash, the win; the milestone card shares the win's hero) |

## The models

| File here | Delivered as | SHA-256 |
|---|---|---|
| `models/heroes.glb` | `Heroes.glb` (2026-10-04; the four heroes; see "The owner's Heroes.glb" below) | `89ebd3a0d5a04be1e9fb8b0092071c18f9c08a64e569d590364184db660ea0f3` |

Until 2026-10-04 (in the repository's history): `models/sprig.glb` (`Sprig_Complete.glb`, `9bdebdff…`), `models/bloom.fbx`
(`Meshy_AI_Petalina_biped_Animation_all_frame_rate_60.fbx`, `052c5936…`), `models/drop.fbx`
(`Meshy_AI_Dewdrop_Buddy_biped_Animation_all_frame_rate_60.fbx`, `cb75560c…`) and `models/twig.glb` (`Twig.glb`, `95c9bd89…`).

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
- A hero that celebrates on the win (Twig, Sprig; `winYaw` 0) is baked a second time facing the player, as it stands
  alone on the win and the milestone: its celebrations and its idle there (`winidle`), with their own palette, so
  Home's frames stay turned toward the fountain (the owner, 2026-10-05: "why do the heroes on the celebration screen
  look aside, their bodies turned?").
- The idle eases into its own first pose over its last 0.75 s, so it loops; the reaction blends in from that pose over
  0.25 s and back to it over its last 0.4 s, so it starts and ends where the idle loop starts.
- Each Meshy clip at the length of the owner's table (`heroes.json` `idleSeconds` 4, `reactSeconds` 2), whatever time
  the file stamps on its frames; Twig's clips at their own spans (from their first keyframe to their last). A hero with
  a `win` clip (Twig's cheer) gets it baked like the reaction, from the seam pose and back to it.
- 24 frames per second (every frame of the motion; 12 before the 60 fps delivery): 96 idle and 48 reaction frames per
  hero. Every frame is cropped to its visible bounds and stored as an 8-bit palette PNG (one 256-entry palette per hero
  shared by all its frames, entry 0 clear, Floyd–Steinberg dithered, `png8.mjs`), 23 MB for the 576 frames.
- Since the owner's Home tuning of 2026-10-05 (spec 005 FR-036, research D29) the Home set (every hero's idle and
  reaction) gets `heroes.json` `home` before its palette (`post.mjs`): a 3 × 3 sharpening kernel of 0.3 on the colors,
  then 110% contrast and 110% saturation, the filters the owner chose in the Home constructor; the win's set stays as
  rendered.

## The Home layers (`layers.mjs`)

| Delivered | Here (`client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`) |
|---|---|
| `01_home_bg_back.png` (852 × 1846) | `home.jpg`: re-encoded as JPEG (quality 90); it replaces the earlier single Home picture. Since 2026-10-04 the garden comes from the owner's calm backgrounds (`bloomlings_calm_backgrounds.zip`, `01_home_calm_garden.png`, recorded in `tools/artgen/models/owner-pictures.md`), given to `layers.mjs` as `01_home_bg_back.png` with this pack's other four layers unchanged (`layers.json` holds its hash). Since the owner's note of 2026-10-04 (spec 005 FR-031) `layers.mjs` scales every layer's saturation by the factor that brings the garden to 70% of the heroes' (×0.73, `layers.json` `saturation`), lightness and hue kept. Since the owner's Home tuning of 2026-10-05 (FR-036) the garden is blurred by a Gaussian of 4/1080 of its width (`gardenBlur`) after that scale; the factor was measured on the heroes before their Home finish (×0.738). Since 2026-10-08 `home.jpg` is the owner's new garden (a 941 × 1672 picture sent in the chat, already blurred: columns with ivy and a round stone terrace with steps), scaled to the picture's height and cropped to its middle 852 × 1846, encoded once as JPEG (quality 90), with no blur pass (`gardenBlur` 0) and its colors as delivered; `layers.json` holds its hash |
| `02_home_fountain_back.png` | `home-fountain-back.png`: cropped to its visible bounds (alpha under 6 of 255 counts as dust) |
| `02_home_fountain_back.png` | `home-lotus.png`: the lotus cut out of it (its pink petals and what they enclose, the edge softened), drawn again over Bloom, who stands behind it |
| `03_home_fountain_front.png` | `home-fountain-front.png`: cropped the same way |

Since 2026-10-08 `home-fountain-front.png` and `home-lotus.png` are the owner's restyled versions (`bloomlings_01_backgrounds_restyled.zip`, `home_layers/home-fountain-back.png` and `home_layers/home-lotus.png`, their canvases matching the layers' boxes): scaled into the same boxes, alpha up to 5 / 255 cleared, scaled by the scene's saturation factor of `layers.json`, which lists their new hashes; record `tools/artgen/models/owner-pictures.md`. The garden, the fountain's back and the shadow stay those of `bloomlings_home_assets.zip` until their restyled versions come. Since the owner's choice of 2026-10-08 (FR-031 as changed) no layer is muted: the front stones and the lotus keep their delivered colors, and the fountain's back and the shadow had the scene's ×0.738 undone (each channel moved away from the pixel's `(max + min) / 2` by 1 / 0.738, lightness and hue kept); `layers.json` lists the new hashes.
| `04_home_soft_shadow.png` | `home-shadow.png`: its front left shadow (of four), cut out with faded edges, drawn under every hero |
| `05_home_petals_overlay.png` | not used since 2026-10-06: the owner removed Home's falling petals (it was `home-petals.png`) |

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

## The owner's Heroes.glb (2026-10-04)

| Field | Value |
|---|---|
| Delivered | by the project owner on 2026-10-04, in conversation, after single exports (`twig2.glb` without clips, `sprig3.glb` with a blank color texture, `drop2.glb`, `bloom3.glb`, an empty `bloom4.glb`): `Heroes.glb`, all four heroes in one file. The owner: "Bake them all and update", then "Bake Twig too. We'll sort out the animations later" |
| Made with | Blender (glTF exporter 5.2.39): four skinned meshes, each with its own rig, material and 2048 × 2048 base color (`GLB_Bloom_Model` 5 551 vertices, 34 joints; `GLB_Drop_Model` 3 196, 13 joints; `GLB_Sprig_character_Model` 6 138, 34 joints; Twig's `GLB_ChatGPT_4_2026_23_55_22` 3 385, 20 joints, with six corrective shape keys); no normal maps; the heroes stand side by side, away from the origin |
| Rights | the owner's: delivered for the game, as the other owner pictures |
| One file | each hero is drawn alone (`heroes.json` `mesh`; the other three hidden), its head points looked up in its own skeleton by the bones' names in the file (three.js makes the second "Head" "Head_1" and keeps the name in `userData`); every clip moves only its own rig |
| Clips | Sprig: `GLB_Sprig_Breathing` (4 s, the idle), `GLB_Sprig_Wave` (3 s, the reaction), `GLB_Sprig_Celebrate` (2 s) and `GLB_Sprig_Clap` (3 s), the win's two, taking turns (as before). Bloom (a new design: a pink face in a crown of petals with a white flower on top): `GLB_Bloom_Breathing` (4 s) and `GLB_Bloom_SmallJump` (1.5 s; its `Wave` lifts the hand only about 30°). Drop: `GLB_Drop_Breathing` (4 s) and `GLB_Drop_Jump` (1.5 s); the breathing moves only 0.1%, so the bake adds a breath of its body (`breathe`: the `Head` bone, which carries the drop, 2.5% across and 1.5% up, once over the loop). Twig: no clips; the bake makes stand-ins (`synth`, page.html `synthClips`): the arms lowered 55° from the model's T-pose with its `arms_down` corrective on, a 3 s idle (a 2.2% breath of the chest, a 2° sway, a slight nod), a 1.5 s hop with the arms opening and the win's 3 s of two jumps with the arms out (higher, they hide behind the cap) — until the owner's own come. Not used: the other clips (LookAround, ThumbsUp, Clap for Bloom, Celebrate for Bloom and Drop) |
| Bones | the hats' head points: the `Head` bone (the chin) and a brow 0.3 m (Sprig), 0.33 m (Bloom), 0.4 m (Drop) and 0.5 m (Twig, the cap's top) up it (`topOffset`) |
| Look | graded to sit together and keep the heroes' mean saturation (spec 005 FR-031: the backgrounds keep 70% of it): Sprig saturation 1.25; Bloom gamma 0.7, gain 1.06, saturation 1.3 and a 0.12 glow; Drop gamma 0.92, gain 1.02, saturation 3.2 (its texture is a pale grey blue); Twig gamma 0.74, gain 1.06, saturation 1.1 and a 0.15 glow. Mean brightness and HSL saturation of every 10th frame: Sprig 0.66 and 0.67, Bloom 0.58 and 0.71, Drop 0.58 and 0.58, Twig 0.55 and 0.68; the heroes' mean 0.659 (0.656 before), so the backgrounds stay as they were (`saturation.json`) |
| Home | the new Bloom's petals are wide: on Home it stands a little smaller (0.40 of the picture's width, was 0.45) and Drop further right and taller (at 0.735, 0.36 tall), so Drop's face shows (`HomeLayers.Placement`) |
| Frames | Sprig 96 idle, 72 reaction, 48 `win`, 72 `win2`; Bloom and Drop 96 idle and 36 reaction; Twig 72 idle, 36 reaction and 72 win: 732 frames, 32.7 MB in all |
| Known | thin light seams along the texture islands of Drop and Twig (the bake left no margin round them): the owner's next export, with a 16 px margin; the Daily Challenge's round button (from level 50) touches Drop's tip on Home |

