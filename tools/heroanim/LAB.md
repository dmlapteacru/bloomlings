# Original motion lab and modular accessories

Owner-requested experiment, 2026-10-16. Open **Settings → Hero lab** in the full .NET playtest APK. Four buttons select
Sprig's new Look, Hello, Bow and Bounce; Replay starts over, 0.5×/1× changes speed, and Hat demonstrates the existing
2D head attachment with the bucket hat. Nothing is purchased, equipped or written to the player's profile. Back returns
to Settings. The production Home, victory clips, meshes and skin weights stay unchanged. This is a test bench, not a
new product Shop or a replacement for a gameplay screen.

## What was authored

`models/heroes.glb` contains four skinned meshes: Sprig and Bloom have 34 bones, Drop 13 and Twig 20. The current Twig
already uses procedural stand-in skeletal clips (`page.html`, `synthClips`).

`author-lab.py` imports that owner's file in Blender, isolates Sprig and captures the first pose of its breathing clip.
It creates five new actions: a still seam pose, a curious head turn, a right-hand greeting, a polite bow and two small
celebratory hops. Rotations use the actual bones' rest axes; motions return to the reference pose, with no cumulative
rotation. It exports `models/sprig-lab.glb` (one original mesh, 34 bones, five actions). It does not generate a new mesh,
change vertex weights or claim to repair the other heroes' rigs. The new GLB can be imported into Blender to inspect
and edit the actual skeletal keyframes.

```bash
blender --background --factory-startup --python tools/heroanim/author-lab.py
node tools/heroanim/bake-lab.mjs
node tools/heroanim/check-lab.mjs
```

The bake needs the Chromium version available to `playwright-core`; `HERO_LAB_CHROMIUM=/path/to/chromium` selects an
already installed compatible browser. It reuses the existing production Three.js renderer, camera fitting and PNG8
palette writer. All motions share one camera/ground point, 256×288 cells, 20 fps and one palette. Only the chosen clip
is warmed in bounded four-frame steps in the game's bounded hero-frame cache. `playtest/art/HeroLab/manifest.json` records the generated
frames and their source, crop and head-anchor data; `HeroLabData.cs` is generated with them. Production `HeroMotion`
data and its 900 pictures are not rebaked. The original GLB stays outside runtime assets. Test pictures are outside Unity Resources and are only included in the full playtest APK/preview. Pass `-p:IncludeHeroMotionLab=false` to omit them and the Settings entry.

Source and rights: these motions were scripted for Bloomlings on the owner's Meshy-derived Sprig mesh and rig;
`SOURCE.md` records the owner's source/rights. No third-party animation library or copied motion capture was added.

## When a model has no usable rig

OBJ has geometry, materials and textures but no skin/skeleton or skeletal animation. FBX/GLB can carry them, but the
export must actually include bones, vertex weights and actions. A bad skin remains bad under new keyframes. New motion
cannot repair collapsed shoulders or a face weighted to the wrong arm.

For a fresh AI mesh: keep a neutral pose with separated arms/legs and no fused props; preserve the textured base; inspect
geometry/intersections; create a simple skeleton; bind weights; test head turn, arm lift, elbow bend and squat; fix local
weights before authoring a whole animation library. A short stylized hero usually needs a compact custom rig, not a
human auto-rigger's finger-heavy skeleton. Blob-like characters can instead use restrained whole-body deformation,
shape keys or a few body bones. Fine facial expressions need appropriate facial topology/morph targets or drawn faces.

## Low-friction accessory workflow

Owner revision, 2026-10-16: a cap equips an exclusive prepared head state instead of
stacking with the natural hairstyle. The owner's new Sprig FBX already carries a
`CapOn` morph for this. The isolated [outfit study](../../output/sprig-outfit-study/README.md)
checks that switch plus independent glasses/backpack on one shared rig. It preserves
the original meshes/weights. The subsequent [in-game equipment pilot](../../product/plans/hero-asset-workflow.md)
uses this Sprig on meta screens; the old APK animation lab remains a separate experiment. The owner's Blender project
is the sole authoring master; the assistant supplies textures and actions for import into it.

1. Freeze one approved, textured, rigged base per family. Version it. Work in Blender on copies; don't regenerate the
   whole hero for each Shop item. That would change the face, proportions, UVs and weights every time.
2. Ask Meshy/Tripo for **one separate prop** (cap, round glasses, backpack), without a character or mannequin. Supply
   front/side reference images when available. The prop should have its own mesh/material and export independently.
3. If an AI tool only produces a dressed full hero, use its object/part export or segmentation if available on that
   workflow/plan. Extract the accessory and discard the regenerated body. Segmentation can leave holes or fuse straps,
   hair and head; it does not guarantee a clean standalone wearable. Avoid outfits whose removal requires carving away
   half the hero. Feature labels and availability differ between Meshy/Tripo versions and subscriptions.
4. Fit the separate prop to the approved base once in Blender: translation, rotation and scale, then check silhouette,
   leaf/petal/ear clearance and surface intersections. Fit metadata is per family; our four head shapes differ greatly.
5. Add named attachment nodes in the authoring rig: `socket_hat` and `socket_glasses` under Head, `socket_back` under
   Chest/Body. Parent rigid caps/glasses/backpacks to the corresponding bone/socket with a saved local transform. They
   usually need **no separate skeleton**. Deforming straps, sleeves or dangling parts may need weights, extra bones or
   baked secondary motion. Export with the correct transform/pivot, textures and consistent units.
6. Test the prop on the reference pose plus head turns, a bow and arm raises. A backpack needs front-strap and behind-body
   occlusion; glasses must remain on the face as it turns. View from front, side and the game's actual camera.

The current runtime is a 2D frame compositor. Simple hats already use projected head points and screen roll, which is
what the lab's Hat switch tests. That is not full 3D perspective or occlusion. For glasses/backpacks, prepare modular
rendered layers and, where necessary, depth/occlusion masks or a few angle variants offline. Backpack rear layer draws
behind the hero; visible straps draw in front. Avoid baking every possible hat×glasses×backpack combination into full
hero frames: storage grows as their product. Independent layers grow roughly as their sum. Keep attachment metadata,
layer order and a version of the base rig with each accessory.

A useful first delivery is: one approved base GLB (or rigged FBX with textures), a neutral pose, and three separate props
as GLB/FBX/OBJ+MTL+textures. Supply one or two fitting screenshots. Then automate placement and rendering from the
saved sockets; later items reuse that setup. Rigid objects are much easier than full clothes and deforming straps.
