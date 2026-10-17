# Source record: the owner's animated heroes and layered Home (spec 005 FR-028)

## BloomHeroV2 modular preview (2026-10-17)

The owner supplied `BloomHeroV2_Coat_part_1.zip` and `BloomHeroV2_HairPeach_part_2.zip`: common bald Bloom,
rig, four actions, nine Head choices, fitted Glasses, three Body garments and two Neck scarves.
Original deliveries / hashes: `models/bloom-v2/source-manifest.json`. The unchanged source parts retain their
world geometry, UVs and ScarfKnit garment fits in `prepare-bloom-v2.py`. `bake-bloom-v2.mjs` uses the delivered
albedo and soft diffuse finish to create 255 cropped RGBA8 layers for 240 saved outfits / 216 distinct looks.
`bloom-v2.json` pins sources, `BloomV2Data.cs` and runtime pixels. Stock Bloom always wears HairPetals.
All 15 pieces are free in Wardrobe for fitting. Source ZIPs / rig / GLB stay offline; 132 old Bloom motion PNGs
are archived outside runtime under `archive/bloom/`. Existing motion timing is retained with the new static pose;
source skeletal actions and action selection remain a future feature. Profile portraits and mosaics are unchanged.

## SprigHeroV2 modular preview (2026-10-17)

The owner supplied two original ZIPs containing the aligned bald Sprig base, rig, four actions, eight Head items,
Glasses, Scarf and three Body garments. Originals and hashes: `models/sprig-v2/source-manifest.json`.
`prepare-sprig-v2.py` preserves world geometry / UVs and scarf fit shape keys; `bake-sprig-v2.mjs` renders cropped
RGBA8 layers with the delivered albedo and the same gentle lighting as Twig V2. `sprig-v2.json` pins every source,
the generated `SprigV2Data.cs` and all 157 PNGs (3,288,169 bytes). The stock in-game head is HairLeafy.
Source files / GLB derivatives remain offline; no runtime 3D or APK build is introduced. The 384 previous motion
PNGs are retained outside runtime under `archive/sprig/`. Wardrobe now separates Head, Face, Body and Accessories;
all 13 fitted Sprig parts are temporarily free there. Shop no longer includes Cosmetics.

## Separate owner model rigging study (2026-10-16)

The owner supplied `plant+character+3d+model.zip` in conversation and asked to try rigging/animating it.
Its `tripo_convert_2337d86d-c6b8-4010-9ac5-d18e2dc3ef72.fbx` has SHA-256
`e8263bc9a77af4245a65c62056822b8a84427362ae9ddf9f11fa32282556eff6`, one 36,956-vertex,
73,932-triangle mesh and textures, no bones/weights/actions. `plant-rig-study.py` authors a fitted
25-bone skeleton, normalized weights and four original skeletal clips on that exact geometry/UVs.
`models/sprig-plant-study.glb` is the resulting source-only model; its review, hashes, checks and
limitations are recorded in `output/plant-rig-study/`. No external animation library or motion capture
was used. Original textures are retained. This experiment does not replace `models/heroes.glb`,
`heroes.json`, any production frames, or the existing Hero lab. No 3D source model enters the APK.

## Production source history

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

Since the owner's request of 2026-10-08 (spec 005 FR-049: every background "a little, just a touch" blurred and darkened)
`layers.mjs` and the hand fitting below write into `tools/heroanim/backgrounds/`, which keeps every background and Home
layer as delivered, and `backdrops.mjs` writes what the game shows into the Backgrounds folder: the garden (and every
other owner background) blurred by a Gaussian of 3 / 1080 of its width and 14% darker, the fountain's back, the lotus and
the front bushes 14% darker only, the shadow as it is (`backdrops.json` holds both hashes of each).

| Delivered | Here (`tools/heroanim/backgrounds/`, shown through `backdrops.mjs` in `client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/`) |
|---|---|
| `01_home_bg_back.png` (852 × 1846) | `home.jpg`: re-encoded as JPEG (quality 90); it replaces the earlier single Home picture. Since 2026-10-04 the garden comes from the owner's calm backgrounds (`bloomlings_calm_backgrounds.zip`, `01_home_calm_garden.png`, recorded in `tools/artgen/models/owner-pictures.md`), given to `layers.mjs` as `01_home_bg_back.png` with this pack's other four layers unchanged (`layers.json` holds its hash). Since the owner's note of 2026-10-04 (spec 005 FR-031) `layers.mjs` scales every layer's saturation by the factor that brings the garden to 70% of the heroes' (×0.73, `layers.json` `saturation`), lightness and hue kept. Since the owner's Home tuning of 2026-10-05 (FR-036) the garden is blurred by a Gaussian of 4/1080 of its width (`gardenBlur`) after that scale; the factor was measured on the heroes before their Home finish (×0.738). Since 2026-10-08 `home.jpg` is the owner's new garden (a 941 × 1672 picture sent in the chat, already blurred: columns with ivy and a round stone terrace with steps), scaled to the picture's height and cropped to its middle 852 × 1846, encoded once as JPEG (quality 90), with no blur pass (`gardenBlur` 0) and its colors as delivered; `layers.json` holds its hash |
| `02_home_fountain_back.png` | `home-fountain-back.png`: cropped to its visible bounds (alpha under 6 of 255 counts as dust) |
| `02_home_fountain_back.png` | `home-lotus.png`: the lotus cut out of it (its pink petals and what they enclose, the edge softened), drawn again over Bloom, who stands behind it |
| `03_home_fountain_front.png` | `home-fountain-front.png`: cropped the same way |

Since 2026-10-08 `home-fountain-front.png` and `home-lotus.png` are the owner's restyled versions (`bloomlings_01_backgrounds_restyled.zip`, `home_layers/home-fountain-back.png` and `home_layers/home-lotus.png`, their canvases matching the layers' boxes): scaled into the same boxes, alpha up to 5 / 255 cleared, scaled by the scene's saturation factor of `layers.json`, which lists their new hashes; record `tools/artgen/models/owner-pictures.md`. The garden, the fountain's back and the shadow stay those of `bloomlings_home_assets.zip` until their restyled versions come. Since the owner's choice of 2026-10-08 (FR-031 as changed) no layer is muted: the front stones and the lotus keep their delivered colors, and the fountain's back and the shadow had the scene's ×0.738 undone (each channel moved away from the pixel's `(max + min) / 2` by 1 / 0.738, lightness and hue kept); `layers.json` lists the new hashes. Later that day the owner kept the original fountain: its back already has its lotus in the basin, so no other lotus goes on top. `home-lotus.png` is again the cut-out of that same lotus (the earlier file, its ×0.738 undone like the back's), drawn again only over Bloom, who stands behind it; the restyled lotus is not used. A restyled back (a pool without the lotus), new front stones and a new shadow sent the same day were tried and set aside; the restyled front stones above stay. Later on 2026-10-08 the owner replaced the whole fountain ("the fountain on Home is wrong in the end") with three pictures sent in the chat, made with the owner's picture generator like the other pictures of the day: the basin with a pedestal in its pool and no lotus, the lotus alone, and two front bushes, each 1448 × 1086 on one transparent canvas (their hashes are `layers.json`'s inputs). They were fitted by hand (not by `layers.mjs`, whose inputs are the five full-size layers): alpha under 6 / 255 cleared, the canvas scaled to the picture's 852 px width (×0.588, Lanczos over premultiplied alpha) with its top at y 674, so the basin's pool ends where the old back did; the basin as `home-fountain-back.png`; the lotus trimmed and scaled ×0.72 more, centered where the owner drew it, its leaves ending at the pedestal's front edge (canvas y 705), as `home-lotus.png`; each front bush scaled ×0.82 toward its outer bottom corner, so the basin's sides and both waterfalls show, as `home-fountain-front.png` (the owner's choice "C" of three trial renders); colors as delivered; each trimmed to its alpha, the boxes in `HomeLayersData.cs`.
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


## Bloom's approved texture cleanup (2026-10-16)

The owner approved the comparison in `output/bloom-texture/` and requested replacing the game assets to test on device.
The original `models/heroes.glb`, Bloom's 5,551-vertex mesh, 34-joint skeleton, skin weights and clips are unchanged.
`textures/bloom-study/front-clean.png` is a project-generated ImageGen paint cleanup of an orthographic albedo render
of that exact mesh; the prompt and original render are kept beside it. `bloom-projection.py` projects the paint onto
the visible original surface and bakes it into the original UVs as `textures/bloom-study/basecolor.png` (2048×2048).
The original paint remains on the back and hidden surfaces. The source is for offline rendering only, never an APK asset.

Bloom's `heroes.json` entry now reads that texture and `textures/bloom-study/look.json`: neutral lights, no added glow,
cleaner color/finish, 0.35 dithering strength. Its shared palette is sampled in the full 448×504 cell, as in the approved
comparison, before cropping the indices. The bake records and checks the texture and appearance recipe hashes.
Only Bloom's 96 idle + 36 reaction frames are replaced, at their existing size, 24 fps, names, crop boxes and anchors.
The other heroes, Sprig's separate animation lab, Home layers and backgrounds are retained. Large turns and rear views
still inherit the original material; this pass targets the existing meta-screen animation set.

## Owner's modular Sprig outfit study (2026-10-16)

The owner supplied `SprigHero.fbx`, `SprigHero@Anims.fbx`, `SprigHero_Cap.fbx`,
`SprigHero_Glasses.fbx`, `SprigHero_Backpack.fbx` in conversation to test one hero
with equip/unequip. Source hashes and inspection are recorded in
`output/sprig-outfit-study/source-inputs.json` and `build-report.json`.
The assets and their supplied rig, weights, embedded textures, materials and CapOn
head state are the owner's inputs for Bloomlings. Mesh topology/coordinates, UVs
and skin weights remain unchanged in this assembly.

`models/sprig-outfit-study.glb` is an offline review model: one shared 25-bone rig,
four meshes (290,606 triangles total) and the owner's CapOn morph. Caps equip that
exclusive prepared head state rather than stacking with the natural hairstyle.
Glasses and backpack are independently visible. No runtime hero assets are replaced.

The supplied animation FBX has four named clips with 259 curves each, but every
curve has constant key values (max range 0). They are preserved as Source_* in the
editable Blender assembly. Study_Idle, Study_Look, Study_Step and Study_Joy are new
original procedural attachment-test motions authored for this study, not recovered
or third-party animations. No external motion library is used.

The review HTML embeds Three.js 0.160.0 (MIT license included), original materials
and the GLB. Review PNG/MP4 files are real GLB renders. Model geometry was compared
after reimport at twelve poses; reports and instructions are in
`output/sprig-outfit-study/README.md`. Editable `.blend`, self-contained HTML and ZIP
are generated under ignored `content/work/sprig-outfit-study/`.

## Modular Sprig in-game equipment pilot (2026-10-17)

At the owner's request, the new Sprig now replaces the displayed meta-screen Sprig. The offline study above remains
historical provenance; the owner's Blender project is the sole authoring master. See
`product/plans/hero-asset-workflow.md` for the updated geometry/texture/animation responsibilities.

`bake-sprig-gear.mjs` renders `models/sprig-outfit-study.glb` using Three.js and headless Chromium. It retains the
owner's embedded textures/materials, rig and CapOn morph. It does not create new texture paint or new skeletal actions.
Six cropped PNG8 layers (natural/capped base and their depth-masked glasses/backpack overlays) and three standalone
shop icons total 117,205 bytes. Depth-only body/cap renders occlude the accessories correctly. An independent rear
view exposes the backpack's outward side for its shop icon.

`sprig-gear.json` records the model hash, crop boxes, image hashes/sizes and generated `SprigGearData.cs` hash.
`check.mjs` verifies all of them and their presence in the bundled HeroMotion folder. No FBX/GLB enters the APK.
Free Sprig-only, meta-only equipment persists through the existing wardrobe save, shown on Home, Wardrobe and wins;
the cap switches the exclusive prepared head. The current runtime pose has a restrained 2D breathing transform.
The full skeletal animation/action feature is deferred, as requested. Existing legacy frames remain for compatibility;
the other heroes and gameplay character art are retained.

## Owner's modular Twig and saved references (2026-10-17)

The owner supplied `BaseHero.fbx`, `BaseHero@Anims.fbx`, `BaseHero_AcornHat.fbx` and `BaseHero_FlowerCap.fbx` to replace
Twig on meta screens and test two free headwear items in the game. Original exchange bytes are retained under
`models/twig/source/`, renamed `TwigHero.fbx`, `TwigHero@Anims.fbx`, `TwigHero_AcornHat.fbx`, `TwigHero_FlowerCap.fbx`.
The owner creates/maintains these meshes and equipment in their single Blender master using Tripo. No second master
is authored here. Original names, SHA-256 hashes, sizes, geometry/weight signatures and inspection are recorded in
`models/twig/build-report.json`. Base mesh: 36,109 vertices; acorn hat: 25,413; flower cap: 24,787.

All meshes lack UV maps and texture images. A legacy UV atlas therefore cannot be applied unchanged.
`prepare-twig-gear.py` adds temporary vertex-colour materials in Twig's existing warm color direction using
`twig-materials.json`: golden skin/bib, brown wood limbs/ears, dark eyes, olive/rust cap and cream/yellow daisy.
This is a provisional material pass, not a reused UV texture or final hand-painted albedo. Source coordinates,
topology, weights and source FBX bytes are unchanged. The actual flower surface is a separate connected component.

Both accessories match the body's 25-bone rest rig. The animation-only FBX has the same bone names/hierarchy but
different rest axes when imported; its four real moving Idle/Wave/Walk/Joy actions are preserved in the source FBX
and require retargeting before use on this mesh rig. The current equipment pilot uses the base rest pose plus the
shared restrained 2D breath, following the owner's separate-animation-feature decision.

`bake-twig-gear.mjs` renders bare/acorn/cap states and two isolated shop icons as cropped PNG8 images, at the existing
448×504 hero cell / 320×320 icon design sizes. Inputs, recipes, generated data and outputs are hashed in `twig-gear.json`
and verified by `check.mjs`. Derived GLB/intermediates stay in ignored `content/work/twig-gear/`; only sprites enter APK
assets. There is no runtime 3D. Headwear is free, Twig-only and meta-only, uses one mutually exclusive Hat slot,
persists through restart and appears on Home, Wardrobe and Twig's win/milestone views. Removing it shows the bare base.

The owner also requested preserving generated modeling references. `references/twig/` contains the final short-body,
bare-head baseline with two low woody ears, the acorn-head reference, and the four final olive/rust cap views.
They were made with OpenAI image generation in this conversation from the owner's character/reference; they are
authoring references, not additional APK assets or exact mesh orthographic projections. Rejected drafts are omitted.

### Twig appearance correction

After the pilot the owner reported angry/jagged brows, a dark face and a flattened/tight acorn hat. The current
`textures/twig/front-color.png` is a new clean color projection reference made with OpenAI image generation from the
actual pilot model render and the saved owner baseline: soft curved brows, light cream-gold skin, brown irises and
tidy facial coloring. `prepare-twig-gear.py` adds a derived planar front UV layer and applies that image to visible
front polygons. Hidden/rear surfaces retain the palette. Painted polygons use white vertex colors to avoid glTF
multiplying the image by the old coarse paint. This is a current-front-pose cleanup, not final all-angle UV paint.

The body coordinates, topology and weights remain unchanged. `twig-materials.json` also records the derived acorn
fit: width/depth ×1.035, lift 0.06 and extra crown height up to 0.075. The base head, cap geometry and original FBX
files are retained; no second Blender master is created. Softer neutral warm light removes the muddy face shading.
`models/twig/build-report.json` distinguishes original and derived signatures and records the fit/paint. Texture,
recipe and output hashes remain checked by `check.mjs`. The five runtime PNG8 images now total 124,390 bytes.
## Twig surface quality — 2026-10-17

The owner requested smooth, vivid, sharp hero surfaces like the approved references. The Twig pilot keeps the source
FBXs and front paint image intact. A reproducible render-normal filter reduces small sculpt noise without moving the
mesh or changing its silhouette / weights; the previously recorded acorn fit remains. Separate roughness and neutral
studio lighting add soft volume. The bake supersamples 3× and reduces linear-light premultiplied samples into the same
delivery dimensions. Five full-color RGBA8 PNGs (479,663 bytes) replace palette / dither output (124,390 bytes).

This pass is a real model render, not a substituted image-generation illustration. Source front paint remains
provisional: final all-angle UV paint, removal of painted lighting and selective geometry cleanup are still needed
before the later action-animation feature. Other heroes' animation libraries keep their existing format.
Recipes, limits and budget: `product/plans/hero-surface-quality.md`; actual comparison: `output/twig-quality/`.

## Original Twig restored — 2026-10-17

The owner prefers the old model and requests its return, then a bald base and two head references. Runtime routing
again uses Twig's original `Heroes.glb` motion set, with its existing texture, frame pixels and clips retained. The
replacement-only free headwear entries are removed from the catalog; saved IDs remain intact but resolve to no worn
item. The replacement FBX sources and rendering study remain archived; they do not override the restored hero.

`references/twig-legacy/` preserves an actual original-mesh frontal render and three OpenAI image-generation modeling
references: bald body, acorn head, olive/rust daisy-cap head. The heads derive from the same new bald reference. They
are not new runtime meshes or approved UV maps. Future geometry stays in the owner's single Blender master. Actual
restored game captures and checks are recorded in `output/twig-restored/`.

## Segmented Tripo Twig fitting study — 2026-10-17

The owner supplied three segmented FBX / texture ZIPs and asked to try assembling them. Original archives, names and
hashes are preserved under `models/twig-tripo/`. The bare base has 8 segments; the acorn generation has 3, including an
unwanted full body / face; the cap generation has 15, including a different head and a clothed body. None has a rig.

`twig-tripo-study.py` keeps the base geometry / UVs and extracts only acorn segments 1/2 and cap segments 0/6/8/10.
The fitting recipe scales / places those objects on one common head. Both whole-head exports reuse the same base
head, ears and mouth; `twig-tripo-check.mjs` verifies their actual GLB geometry, UV, paint and world transforms against
the base. Generated metallic / normal shading is removed on the derived copy and eyes receive a gloss material;
the original color maps are retained. This is a static study with provisional Tripo paint, not final surface work.

Five embedded-texture GLBs passed a real Blender round-trip import, including the shared neck pivot. The import ZIP,
fixed-camera renders and source / delivery hashes are in `output/twig-tripo-study/` and `models/twig-tripo/`. The
owner imports these exchange files into the existing master; no separate `.blend` is saved. No production sprites,
gameplay, cosmetics or APK assets are changed; the previously restored original Twig remains active in the game.

## Segmented Tripo Twig enabled in game — 2026-10-17

The owner clarified that the assembled study should be playable. `bake-twig-tripo.mjs` now renders the actual
`TwigHero_Review.glb` with its unchanged base, fitted acorn / cap groups and original color maps. It publishes three
full-body states and two headwear-only icons, 438,562 bytes total, RGBA8 with alpha-safe linear-light downsampling.
Source ZIP / assembly / fit / bake hashes are recorded in `twig-gear.json`. The five existing `twigmod-*` names are
reused; legacy animation pixels remain archived, but shared runtime routing uses the new states on meta screens.
Both free Twig-only catalog entries return, with saved selections and Remove / Default. Profile avatar pictures
remain independent. The static source has no rig; breathing uses the existing 2D transform. Final UV paint and
skeletal actions remain separate work. No source models are runtime assets, and no APK is built for this change.

Current game captures: `output/twig-tripo-in-game/`; model imports / segmentation proofs remain unchanged under
`output/twig-tripo-study/` and `models/twig-tripo/`. The earlier `bake-twig-gear.mjs` is archived tooling for the old
untextured FBX pilot, not the current publisher.

## Twig head width correction — 2026-10-17

The owner found the new head too wide / flattened. `bake-twig-tripo.mjs` now applies X scale 0.88 to the shared
head pivot on its derived render copy, including ears, mouth and either fitted hat. Body geometry, render camera,
height and feet are unchanged. Source archives / fitted GLBs / Blender study delivery remain intact; no new master
is created. All five full-color PNGs are rebaked (426,263 bytes total), including matching Shop icons. Their source /
output hashes and crop rectangles are refreshed in `twig-gear.json`, `TwigGearData.cs` and the runtime art manifest.
Actual game comparison: `output/twig-head-proportions/`; current game captures: `output/twig-tripo-in-game/`.

## Simplified four-hero Tripo trial — 2026-10-17

The owner supplied new simplified Bloom, Drop, Twig and Sprig FBX / texture ZIPs and requested them in the game.
Original archives / names / hashes are preserved under `models/simple-tripo/`; approved OpenAI-generated references
from this conversation are saved under `references/simple-heroes/`. Each model is one textured unrigged mesh.

`prepare-simple-heroes.py` retains geometry and UV / base color, uniformly normalizes the complete model, and removes
noisy generated normal / metallic shading only on a derived copy. No previous Twig head-width correction is applied.
`bake-simple-heroes.mjs` renders the actual meshes to four 448×504 logical-cell, full-color transparent PNGs
(411,390 bytes together). Alpha-safe supersampling preserves clean edges without palette quantization. Camera / fill /
foot placement are shared; no source silhouette is stretched. Inputs and art hashes are in `simple-heroes.json`.

The shared meta-screen renderer now uses all four bases. Existing fitted Sprig / Twig 3D parts are withheld while
unfitted to the new models, but their service / save selection remains intact. Generic 2D cosmetics, avatar pictures,
frames and badges are retained. The trial has only the source static poses and existing 2D breathing; no new skeletal
actions, runtime 3D models, or second Blender master. Actual game captures: `output/simple-heroes-in-game/`.

Viewer brightness correction in the same four-hero trial: source paint already includes illumination. The final
renderer replaces the extra PBR / specular pass with 75% original albedo + 25% soft diffuse light. This avoids the
dark chin / cheeks and dirty eye whites seen in the first in-game review. Source textures are not repainted; there
is no dark UI overlay to remove. Actual rendered comparison is in `output/simple-heroes-in-game/lighting-comparison.png`.

## Shared bald Twig with acorn / panama — 2026-10-17

The owner supplied three new Tripo ZIPs based on the bald Twig and the two headwear reference images. Original
archives / filenames / hashes are retained under `models/twig-modular/`; generated reference images are retained in
`references/twig-modular/`. All three FBXs are single connected textured meshes without a rig.

`prepare-twig-modular.py` retains the complete bald base, including head / ears / UV / paint. It derives accessory
surfaces from the two head-only generations using the seam plane, paint / normal / spatial masks and the connected
hat region, discarding their alternate faces. Only headwear is fitted with `twig-modular-fit.json`; Acorn restores its original height and smoothly tapers the
lower band in X to close the lateral opening, leaving the upper crown / leaf and Panama fit intact. Source images and
base world geometry / UV remain unchanged. No separate `.blend` master is created. The common base appears in each
runtime state; fixed face / body regions compare pixel-identical across the renders.

`bake-twig-modular.mjs` uses the approved light painted-color treatment from the simplified heroes (75% albedo / 25%
soft diffuse), 3× alpha-safe supersampling and RGBA8 output. Five small `twigfit-*` states / icons total 365,113 bytes.
`twig-modular.json`, `TwigModularData.cs` and the HeroMotion manifest record hashes / crops. Shop / Wardrobe offer the
two free fitted Twig hats; Remove shows the bare base, and saved selection follows Home / victory. The original
Acorn ID is reused, Panama has a new ID; the unrefitted old Daisy Cap stays saved but withheld. Other simplified
heroes and profile pictures are retained. No new skeletal actions, runtime 3D or APK build. Actual game captures:
`output/twig-modular-in-game/`.


## TwigHeroV2 — owner-aligned modular delivery, 2026-10-17

The owner supplied `TwigHeroV2.zip`, retained byte-for-byte at `models/twig-v2/source/TwigHeroV2.zip` with its hash in
`source-manifest.json`. It contains the common bald Twig mesh and 25-bone rig, four authored actions (Idle, Joy,
Walk, Wave) and nine separately aligned wearable FBXs: PumpkinHat, BucketHat, Hair, HatHair (hat plus hair), Beret,
EarmuffHair, XmasHat, Glasses and Scarf. The delivered manifest records Head / Face / Neck slots. The seven Head
parts are exclusive; Face and Neck remain independent. The two previous Acorn / Panama item IDs are retained.

`prepare-twig-v2.py` imports each aligned part without trimming, scaling, refitting or changing UV / world geometry;
only its derived bake copy loses the rig and extra PBR maps. Original FBXs / embedded maps / rig / actions remain in
the archive. No second editable Blender master is created. `bake-twig-v2.mjs` uses the approved 75% painted albedo /
25% soft white diffuse treatment, original accessory colors / alpha, 3× alpha-safe supersampling and RGBA8 PNGs.
The 448×504 logical camera cell and foot line retain the base's scale; padding above the cell fits the holiday hat.
Eight base-with-head states, eight depth-masked scarves, eight masked glasses and nine isolated icons total 33 PNGs,
1,614,285 bytes. Layers allow all 32 combinations without duplicating complete outfits. Only PNGs enter Android
assets; the ~33 MB source archive and derived GLB are offline inputs. The game keeps its current subtle 2D breath;
action selection / animation authoring remain a separate feature.

All nine parts are temporarily free in Shop → Cosmetics → Twig, also available in the Wardrobe's accessories shelf.
Head, Glasses and Scarf slots save / reload independently; removing one preserves the others. Default removes all.
The common selection appears on Home, fitting stands, victory and other meta hero hosts. Meta-only parts remain out
of gameplay workers and independently chosen profile avatars. `twig-v2.json`, `TwigV2Data.cs`, `build-report.json`
and HeroMotion's manifest pin the source, conversion, dimensions and output hashes. Screens: `output/twig-v2-in-game/`.


## Permanent Twig V2 identity migration — 2026-10-17

The owner approved TwigHeroV2 in game and requested full replacement of the previous Twig. Current modeling references
are preserved under `tools/heroanim/references/twig-v2/` with hashes. Full-resolution portrait / medal sources in its
`identity/` folder were generated with OpenAI image generation from the approved new Twig reference. New runtime
avatars retain their three previous IDs, prices and themes; `pack-twig-identity.py` downsamples them to 384px JPEGs.
`pack_badges.py --only hero_twig` overrides the historical medal atlas with the new source, producing its 256px RGBA
badge without changing achievement rules. Solo / cheer / group fallback pictures use current baked production sprites;
Twig's blank-face fallback is derived from its actual mesh in an offline renderer without changing source texture.
The old Twig frame library / fitted states are retired outside Resources in `tools/heroanim/archive/twig/` (263 PNGs,
11,631,125 bytes). Only TwigHeroV2 is active; no previous Twig publisher may write to production. Existing saves map
the retired flower cap to Panama without spending or resetting progress. The owner explicitly keeps L2000's mosaic
unchanged. No source models / references enter APK resources. No APK is built here.

## Twig clothing addition and warm accessory materials — 2026-10-17

Owner supplied four separate FBXs after the expanded ZIP exceeded the attachment transfer limit. They are preserved
byte-for-byte in `models/twig-v2/source/additions-2026-10-17/`, with hashes / roles in the source manifest. Glasses
replaces the prior frame-plus-lenses mesh without changing `glasses.twig_round`. Its single material becomes soft
warm brown. Jacket (winter green / cream), Coat (autumn orange / brown) and Overalls (cream / green shirt) add three
free Clothes choices; `twig.clothes` is independent of Head, Glasses and Neck. Generic puzzle cosmetics / levels do
not change. The owner's Blender project remains the single authoring master.

`twig-v2-look.json` and `bake-twig-v2.mjs` author accessory materials during offline rendering: the Panama's non-cream
fabric is graded continuously by luminance to warm brown; the separate daisy / cream trim and painted detail remain.
No image synthesis, texture repaint, geometry / UV / alignment edit or runtime shader is introduced. The new glasses
has open lens holes and no lens planes. The original source archives and FBXs remain unchanged.

The source / geometry report proves headwear lies above clothes and scarf, and glasses above clothes. This allows
35 RGBA sprites, 1,674,582 bytes, to cover 128 combinations with four on-demand layers, including garment-aware scarf
occlusion. Default resets all worn items; individual removal leaves other slots intact. Save kinds and enum values
are extended at the end. Exact inventory: `models/twig-v2/ITEMS.md`. No APK build; L2000 mosaic remains unchanged.

## Stock Twig head and Sprig modeling baseline — 2026-10-17

The owner clarified that the Acorn Hat is Twig's permanent default in game. An empty / removed / reset Head slot now
resolves to the Acorn Hat and its matching glasses mask; it never selects the bald image. Save entries remain empty
for stock appearance, preserving the existing customization / reset semantics. Home fitting now measures the stock
hat rather than the bald crop while preserving the approved body scale. Bald source geometry / references remain
for offline modeling. Seven head looks give 112 distinct outfits; 128 serialized combinations still include both
implicit stock and explicit Acorn Hat.

The requested bald Sprig modeling reference is preserved in `references/sprig-v2/` alongside his approved default
leafy reference, with hashes. OpenAI image generation removed only the leaf hairstyle while retaining the green
palette, face and compact body. This is an offline Tripo / Meshy reference, not a replacement for the current in-game
Sprig model. The owner will supply the new base / modular parts from their Blender master.
